#!/bin/bash
# -----------------------------------------------------------------------------
# restore.sh: restore the Acxess database from R2 and start the app.
#
# Usage          sudo acxess-restore /dev/shm/<age-key> [backup-file-name]
#                Without a file name, it uses the newest backup in R2.
#
# What it does
#   1. Checks: root, tools, Ansible already ran, free disk space.
#   2. Chooses the backup.
#   3. Downloads and decrypts it into the SQL Server container, then checks it
#      (RESTORE VERIFYONLY). The decrypted file never touches the host disk.
#      The app is still running in this step.
#   4. Stops the web app, restores the database (WITH CHECKSUM) and runs
#      DBCC CHECKDB. The app is down from here.
#   5. Starts the app. The migrator runs first.
#   6. Waits until the app answers and prints the time of each step.
#
# The age private key must be in /dev/shm (memory, never on disk).
# The script deletes it as soon as the backup is decrypted.
#
# On the server  /usr/local/sbin/acxess-restore (installed by Ansible, root only)
# Needs          ansible-playbook site.yml already applied on this server
# Exit code      0 = database restored and app answering, other = failed
# Runbook        docs/runbooks/restore-database.md
# -----------------------------------------------------------------------------

set -euo pipefail
APP_DIR="/opt/acxess"
BACKUP_ENV="/opt/acxess-backup/.env"
export RCLONE_CONFIG="/home/acxess-backup/.config/rclone/rclone.conf"
CONTAINER="sql_acxess_container"
DB_NAME="AcxessDB"
RESTORE_FILE="/var/opt/mssql/backup/restore.bak"
MIN_FREE_MB=2048

IDENTITY="${1:-}"
WANTED="${2:-}"

log()  { echo "[$(date +%H:%M:%S)] $*"; }
fail() { echo "ERROR: $*" >&2; exit 1; }

# Run T-SQL inside the container. The password never leaves the container.
# -b: exit with an error code when the SQL fails (without it, sqlcmd returns 0).
sql() {
    docker exec "$CONTAINER" bash -c \
        '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -W -b -Q "$1"' _ "$1"
}

cleanup() {
    rm -f "$IDENTITY"
    docker exec "$CONTAINER" rm -f "$RESTORE_FILE" 2>/dev/null || true
}

# ---------- 1. Checks ----------
[ "$(id -u)" -eq 0 ] || fail "run it with sudo"
[ -n "$IDENTITY" ]   || fail "usage: sudo acxess-restore /dev/shm/<age-identity> [backup-file-name]"

case "$IDENTITY" in
    /dev/shm/*) ;;
    *) fail "the age identity must be in /dev/shm (RAM), not on disk" ;;
esac

[ -f "$IDENTITY" ] || fail "$IDENTITY not found"
trap cleanup EXIT


for cmd in docker age rclone; do
    command -v "$cmd" > /dev/null || fail "$cmd is not installed. Run ansible-playbook site.yml first"
done

[ -f "$APP_DIR/.env" ] || fail "$APP_DIR/.env not found. Run ansible-playbook site.yml first"
[ -f "$BACKUP_ENV" ]   || fail "$BACKUP_ENV not found. Run ansible-playbook site.yml first"

RCLONE_REMOTE="$(grep '^RCLONE_REMOTE=' "$BACKUP_ENV" | cut -d= -f2- | tr -d "'")"

FREE_MB="$(df --output=avail -m /var/lib/docker | tail -n 1 | tr -d ' ')"
[ "$FREE_MB" -ge "$MIN_FREE_MB" ] || fail "only ${FREE_MB} MB free in /var/lib/docker, need ${MIN_FREE_MB} MB"

cd "$APP_DIR"
START=$SECONDS

# ---------- 2. Choose the backup ----------
if [ -n "$WANTED" ]; then
    FILE="$WANTED"
else
    # Names are acxess_YYYY-MM-DD_HH-MM-SS.bak.age, so alphabetical order = date order.
    FILE="$(rclone lsf --files-only --include '*.bak.age' "$RCLONE_REMOTE" | sort | tail -n 1)"
fi
[ -n "$FILE" ] || fail "no backups found in $RCLONE_REMOTE"
log "Backup: $FILE"

# ---------- 3. Prepare (the app is still running) ----------
log "Starting SQL Server"
docker compose up -d --wait acxess-db

log "Downloading and decrypting into the container"
T=$SECONDS
docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
rclone cat "$RCLONE_REMOTE/$FILE" \
    | age -d -i "$IDENTITY" \
    | docker exec -i "$CONTAINER" sh -c "cat > $RESTORE_FILE"
rm -f "$IDENTITY"
log "Identity deleted"

sql "RESTORE VERIFYONLY FROM DISK = N'$RESTORE_FILE' WITH CHECKSUM"
T_PREPARE=$((SECONDS - T))

# ---------- 4. Restore (from here the app is down) ----------
log "Stopping the web app"
docker compose --profile app stop acxess-web

log "Restoring $DB_NAME"
T=$SECONDS
sql "RESTORE DATABASE [$DB_NAME] FROM DISK = N'$RESTORE_FILE' WITH REPLACE, CHECKSUM, STATS = 25"
docker exec "$CONTAINER" rm -f "$RESTORE_FILE"

log "Checking the database"
sql "DBCC CHECKDB ([$DB_NAME]) WITH NO_INFOMSGS, ALL_ERRORMSGS"
sql "SET NOCOUNT ON; SELECT state_desc, recovery_model_desc FROM sys.databases WHERE name = '$DB_NAME'"
sql "SET NOCOUNT ON; SELECT (SELECT COUNT(*) FROM [$DB_NAME].[Identity].Tenants) AS tenants, (SELECT COUNT(*) FROM [$DB_NAME].Membership.Members) AS members"
T_RESTORE=$((SECONDS - T))

# ---------- 5. Start the app ----------
log "Starting the app (the migrator runs first)"
T=$SECONDS
docker compose --profile app up -d

# ---------- 6. Check the app ----------
log "Waiting for the app to answer"
for _ in $(seq 1 30); do
    if docker exec caddy_proxy wget -q -O /dev/null http://acxess-web:8080/ 2>/dev/null; then
        T_APP=$((SECONDS - T))
        log "Restore completed in $((SECONDS - START)) s (prepare ${T_PREPARE} s, restore and checks ${T_RESTORE} s, app ${T_APP} s)"
        exit 0
    fi
    sleep 2
done
fail "the app does not answer after 60 s. Check: docker compose logs acxess-web"