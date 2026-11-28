#!/bin/bash
# -----------------------------------------------------------------------------
# backup.sh: back up the Acxess database to Cloudflare R2.
#
# What it does
#   1. Makes a full SQL Server backup inside the container, WITH CHECKSUM.
#   2. Encrypts it with age. Only the public key is on the server.
#   3. Uploads it with "rclone copy" (never deletes in R2), then checks the upload.
#   4. Deletes local copies older than 7 days (only after the check).
#
# On the server  /opt/acxess-backup/backup.sh (installed by Ansible)
# Runs           every day at 03:00, as acxess-backup (acxess-backup.timer)
# Run by hand    sudo systemctl start acxess-backup.service
# Logs           journalctl -u acxess-backup.service
# Config         .env next to the script, written by Ansible:
#                DB_PASSWORD, AGE_RECIPIENTS_FILE, RCLONE_REMOTE, BACKUP_DIR,
#                HEALTHCHECK_URL (optional)
# Alerts         pings HEALTHCHECK_URL when it works, HEALTHCHECK_URL/fail when not
# Exit code      0 = backup uploaded and checked, other = failed
# More           infrastructure/README.md
# -----------------------------------------------------------------------------

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
ENV_FILE="$SCRIPT_DIR/.env"

if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: $ENV_FILE not found" >&2
    exit 1
fi

set -a
source "$ENV_FILE"
set +a

for var in DB_PASSWORD AGE_RECIPIENTS_FILE RCLONE_REMOTE BACKUP_DIR; do
    if [ -z "${!var:-}" ]; then
        echo "ERROR: $var is not defined in $ENV_FILE" >&2
        exit 1
    fi
done
if [ ! -s "$AGE_RECIPIENTS_FILE" ]; then
    echo "ERROR: $AGE_RECIPIENTS_FILE not found or empty" >&2
    exit 1
fi

CONTAINER="sql_acxess_container"
DB_NAME="AcxessDB"
DATE_NOW="$(date +%Y-%m-%d_%H-%M-%S)"
CONTAINER_FILE="/var/opt/mssql/backup/acxess_${DATE_NOW}.bak"

mkdir -p "$BACKUP_DIR"

ENCRYPT_FILE="$BACKUP_DIR/acxess_${DATE_NOW}.bak.age"

cleanup() {
    status=$?
    docker exec "$CONTAINER" rm -f "$CONTAINER_FILE" || true
    rm -f "$ENCRYPT_FILE.part"

    if [ -n "${HEALTHCHECK_URL:-}" ]; then
        if [ "$status" -eq 0 ]; then
            curl -fsS -m 10 --retry 3 "$HEALTHCHECK_URL" > /dev/null || true
        else
            curl -fsS -m 10 --retry 3 "$HEALTHCHECK_URL/fail" > /dev/null || true
        fi
    fi
}
trap cleanup EXIT

echo "Starting backup of $DB_NAME"
docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$DB_PASSWORD" \
    -C \
    -b \
    -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$CONTAINER_FILE' WITH CHECKSUM, STATS = 10"

echo "Encrypting backup"
docker exec "$CONTAINER" cat "$CONTAINER_FILE" | age -R "$AGE_RECIPIENTS_FILE" -o "$ENCRYPT_FILE.part"
mv "$ENCRYPT_FILE.part" "$ENCRYPT_FILE"

docker exec "$CONTAINER" rm -f "$CONTAINER_FILE"

echo "Uploading to $RCLONE_REMOTE"
rclone copy "$BACKUP_DIR" "$RCLONE_REMOTE/" --include "*.bak.age" -v

echo "Verifying upload"
rclone check "$BACKUP_DIR" "$RCLONE_REMOTE" --one-way

echo "Removing local backups older than 7 days"
find "$BACKUP_DIR" -type f -name "*.bak.age" -mtime +7 -delete

echo "Backup completed: $ENCRYPT_FILE"
