#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="$SCRIPT_DIR/.env"

if [ ! -f "$ENV_FILE" ]; then
    echo "ERROR: $ENV_FILE not found" >&2
    exit 1
fi

set -a
source "$ENV_FILE"
set +a

for var in DB_PASSWORD AGE_RECIPIENT RCLONE_REMOTE BACKUP_DIR; do
    if [ -z "${!var:-}" ]; then
        echo "ERROR: $var is not defined in $ENV_FILE" >&2
        exit 1
    fi
done

CONTAINER="sql_acxess_container"
DB_NAME="AcxessDB"
DATE_NOW="$(date +%Y-%m-%d_%H-%M-%S)"
CONTAINER_FILE="/var/opt/mssql/backup/acxess_${DATE_NOW}.bak"

mkdir -p "$BACKUP_DIR"

echo "Starting backup of $DB_NAME"
docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$DB_PASSWORD" \
    -C \
    -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$CONTAINER_FILE' WITH CHECKSUM, STATS = 10"

cleanup() {
    docker exec "$CONTAINER" rm -f "$CONTAINER_FILE" || true
    rm -f "$ENCRYPT_FILE.part"
}
trap cleanup EXIT

ENCRYPT_FILE="$BACKUP_DIR/acxess_${DATE_NOW}.bak.age"
echo "Encrypting backup"
docker exec "$CONTAINER" cat "$CONTAINER_FILE" | age -r "$AGE_RECIPIENT" -o "$ENCRYPT_FILE.part"
mv "$ENCRYPT_FILE.part" "$ENCRYPT_FILE"

docker exec "$CONTAINER" rm -f "$CONTAINER_FILE"

echo "Uploading to $RCLONE_REMOTE"
rclone copy "$BACKUP_DIR" "$RCLONE_REMOTE/" --include "*.bak.age" -v

echo "Verifying upload"
rclone check "$BACKUP_DIR" "$RCLONE_REMOTE" --one-way

echo "Removing local backups older than 7 days"
find "$BACKUP_DIR" -type f -name "*.bak.age" -mtime +7 -delete

echo "Backup completed: $ENCRYPT_FILE"
