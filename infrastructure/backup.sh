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
FECHA="$(date +%Y-%m-%d_%H-%M-%S)"
ARCHIVO_CONTENEDOR="/var/opt/mssql/backup/acxess_${FECHA}.bak"

mkdir -p "$BACKUP_DIR"

echo "Starting backup of $DB_NAME"
docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost \
    -U sa \
    -P "$DB_PASSWORD" \
    -C \
    -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$ARCHIVO_CONTENEDOR' WITH CHECKSUM, STATS = 10"


ARCHIVO_CIFRADO="$BACKUP_DIR/acxess_${FECHA}.bak.age"
echo "Encrypting backup"
docker exec "$CONTAINER" cat "$ARCHIVO_CONTENEDOR" | age -r "$AGE_RECIPIENT" -o "$ARCHIVO_CIFRADO"
docker exec "$CONTAINER" rm -f "$ARCHIVO_CONTENEDOR"

# Create folder backups
# mkdir -p /opt/acxess/backups

# FECHA=$(date +%Y-%m-%d_%H-%M-%S)
# ARCHIVO="/var/opt/mssql/backup/acxess_$FECHA.bak"
# ARCHIVO_HOST="/opt/acxess/backups/acxess_$FECHA.bak"

# # Start backup process
# echo "Starting backup of the AcxessDB database"

# docker exec sql_acxess_container /opt/mssql-tools18/bin/sqlcmd \
#     -S localhost \
#     -U sa \
#     -P "$DB_PASSWORD" \
#     -C \
#     -Q "BACKUP DATABASE [AcxessDB] TO DISK = N'$ARCHIVO' WITH NOFORMAT, NOINIT, NAME = 'Acxess-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"


# # Copy backup file to host volume
# echo "Copying backup file to host volume"
# docker cp sql_acxess_container:$ARCHIVO $ARCHIVO_HOST

# # Delete temporary backup file inside the container to avoid disk saturation
# echo "Eliminando archivo temporal dentro del contenedor..."
# docker exec sql_acxess_container rm -f "$ARCHIVO"

# # Remove backup file from container
# echo "Removing backup file from container"
# find /opt/acxess/backups/ -type f -name "*.bak" -mtime +7 -exec rm {} \;

# # Sync backup to Cloudflare R2 using rclone
# echo "Synchronizing backups with Cloudflare R2..."
# rclone sync /opt/acxess/backups/ r2-acxess:acxess-backups/sql-server/ -v

# echo "Backup completed successfully and secured in the cloud: $ARCHIVO_HOST"
