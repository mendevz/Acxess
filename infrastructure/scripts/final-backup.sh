#!/bin/bash
# -----------------------------------------------------------------------------
# final-backup.sh: last backup of a server before it is rebuilt or replaced.
#
# Usage          infrastructure/scripts/final-backup.sh <ssh-destination> <output-dir>
#                Example: final-backup.sh acxess-prod ~/acxess-migration
#
# What it does
#   1. Stops the web app: no more writes, so nothing is lost after this backup.
#   2. Makes a full SQL Server backup in the container (WITH CHECKSUM)
#      and checks it (RESTORE VERIFYONLY).
#   3. Streams it over SSH and encrypts it with age ON THIS MACHINE.
#      The unencrypted backup never touches a disk outside the container.
#   4. Decrypts it here and compares its SHA-256 with the file on the server.
#
# Runs on        your machine, not on the server
# Needs          ssh, age, the age private key, and an SSH user that can run
#                docker without sudo on the server
# If it fails    it starts the web app again. If it works, the app stays stopped.
#                To cancel later:  ssh <ssh-destination> 'docker start acxess_web'
# Exit code      0 = encrypted backup saved and checked, other = failed
# -----------------------------------------------------------------------------

set -euo pipefail

DEST="${1:-}"
OUT_DIR="${2:-}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VARS_FILE="$SCRIPT_DIR/../ansible/group_vars/all/vars.yml"
AGE_IDENTITY="${AGE_IDENTITY:-$HOME/.ssh/acxess_backup_age.key}"
CONTAINER="sql_acxess_container"
WEB="acxess_web"
DB_NAME="AcxessDB"
STAMP="$(date +%Y-%m-%d_%H-%M-%S)"
CONTAINER_FILE="/var/opt/mssql/backup/final_${STAMP}.bak"

log()  { echo "[$(date +%H:%M:%S)] $*"; }
fail() { echo "ERROR: $*" >&2; exit 1; }

# Run T-SQL in the container on the server. The password never leaves the container.
remote_sql() {
    local inner='/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b -Q "$1"'
    ssh "$DEST" "docker exec $CONTAINER bash -c $(printf '%q' "$inner") _ $(printf '%q' "$1")"
}

# ---------- 1. Checks (nothing changes yet) ----------
[ -n "$DEST" ] && [ -n "$OUT_DIR" ] || fail "usage: $0 <ssh-destination> <output-dir>"

for cmd in ssh age sha256sum; do
    command -v "$cmd" > /dev/null || fail "$cmd is not installed"
done

[ -f "$AGE_IDENTITY" ] || fail "age key not found: $AGE_IDENTITY (set AGE_IDENTITY)"

RECIPIENTS="$(grep -oE 'age1[0-9a-z]+' "$VARS_FILE")"
[ "$(wc -l <<< "$RECIPIENTS")" -ge 2 ] || fail "expected 2 or more age recipients in $VARS_FILE"

ssh "$DEST" "docker ps --format '{{.Names}}'" | grep -x "$CONTAINER" > /dev/null \
    || fail "$CONTAINER is not running on $DEST, or this user cannot use docker"

mkdir -p "$OUT_DIR"
OUT_FILE="$OUT_DIR/acxess_${STAMP}.bak.age"

STOPPED="no"
cleanup() {
    status=$?
    rm -f "$OUT_FILE.part"
    ssh "$DEST" "docker exec $CONTAINER rm -f $CONTAINER_FILE" || true
    if [ "$status" -ne 0 ] && [ "$STOPPED" = "yes" ]; then
        echo "The final backup FAILED: starting the web app again" >&2
        ssh "$DEST" "docker start $WEB" || true
    fi
}
trap cleanup EXIT

# ---------- 2. Freeze: no more writes ----------
log "Stopping the web app on $DEST"
ssh "$DEST" "docker stop $WEB" > /dev/null
STOPPED="yes"

# ---------- 3. Backup and check on the server ----------
log "Backup of $DB_NAME inside the container"
ssh "$DEST" "docker exec $CONTAINER mkdir -p /var/opt/mssql/backup"
remote_sql "BACKUP DATABASE [$DB_NAME] TO DISK = N'$CONTAINER_FILE' WITH CHECKSUM, STATS = 25"
remote_sql "RESTORE VERIFYONLY FROM DISK = N'$CONTAINER_FILE' WITH CHECKSUM"
SERVER_SHA="$(ssh "$DEST" "docker exec $CONTAINER sha256sum $CONTAINER_FILE" | cut -d' ' -f1)"

# ---------- 4. Stream it here and encrypt it before it touches the disk ----------
log "Downloading and encrypting with age (on this machine)"
ssh "$DEST" "docker exec $CONTAINER cat $CONTAINER_FILE" \
    | age -R <(printf '%s\n' "$RECIPIENTS") -o "$OUT_FILE.part"
mv "$OUT_FILE.part" "$OUT_FILE"

# ---------- 5. Check: decrypt here and compare ----------
log "Checking: decrypt and compare SHA-256"
LOCAL_SHA="$(age -d -i "$AGE_IDENTITY" "$OUT_FILE" | sha256sum | cut -d' ' -f1)"
[ "$SERVER_SHA" = "$LOCAL_SHA" ] || fail "SHA-256 is different (server $SERVER_SHA, here $LOCAL_SHA)"

log "Final backup OK: $OUT_FILE"
log "SHA-256 of the database backup: $SERVER_SHA"
log "The web app is STOPPED on $DEST. Next: snapshot and rebuild."