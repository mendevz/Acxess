#!/bin/bash
# Create folder backups
mkdir -p /opt/acxess/backups

FECHA=$(date +%Y-%m-%d_%H-%M-%S)
ARCHIVO="/var/opt/mssql/backup/acxess_$FECHA.bak"
ARCHIVO_HOST="/opt/acxess/backups/acxess_$FECHA.bak"

# Start backup process
echo "Starting backup of the AcxessDB database"
docker exec sql_acxess_container /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Acxess_Prod_2026_Secure!!' -C -Q "BACKUP DATABASE [AcxessDB] TO DISK = N'$ARCHIVO' WITH NOFORMAT, NOINIT, NAME = 'Acxess-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"

# Copy backup file to host volume
echo "Copying backup file to host volume"
docker cp sql_acxess_container:$ARCHIVO $ARCHIVO_HOST

# Remove backup file from container
echo "Removing backup file from container"
find /opt/acxess/backups/ -type f -name "*.bak" -mtime +7 -exec rm {} \;

# Sync backup to Cloudflare R2 using rclone
echo "Synchronizing backups with Cloudflare R2..."
rclone sync /opt/acxess/backups/ r2-acxess:acxess-backups/sql-server/ -v

echo "Backup completed successfully and secured in the cloud: $ARCHIVO_HOST"
