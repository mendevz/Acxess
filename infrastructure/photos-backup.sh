#!/bin/bash
# -----------------------------------------------------------------------------
# photos-backup.sh: copy the member photos to a second R2 bucket.
#
# What it does
#   1. Copies new photos with "rclone copy" (never deletes in the backup bucket).
#   2. Checks that every photo is in the backup bucket.
#
# On the server  /opt/acxess-backup/photos-backup.sh (installed by Ansible)
# Runs           every day at 03:30, as acxess-backup (acxess-photos-backup.timer)
# Run by hand    sudo systemctl start acxess-photos-backup.service
# Logs           journalctl -u acxess-photos-backup.service
# Config         .env next to the script, written by Ansible:
#                PHOTOS_SRC, PHOTOS_DST, PHOTOS_HEALTHCHECK_URL (optional)
# Access         the token for the photos bucket can only read.
#                The token for the backup bucket can read and write.
# Alerts         pings PHOTOS_HEALTHCHECK_URL when it works, .../fail when not
# Exit code      0 = photos copied and checked, other = failed
# More           infrastructure/README.md
# -----------------------------------------------------------------------------

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

set -a
source "$SCRIPT_DIR/.env"
set +a

notify() {
    status=$?
    if [ -n "${PHOTOS_HEALTHCHECK_URL:-}" ]; then
        if [ "$status" -eq 0 ]; then
            curl -fsS -m 10 --retry 3 "$PHOTOS_HEALTHCHECK_URL" > /dev/null || true
        else
            curl -fsS -m 10 --retry 3 "$PHOTOS_HEALTHCHECK_URL/fail" > /dev/null || true
        fi
    fi
}
trap notify EXIT

echo "Copying photos"
rclone copy "$PHOTOS_SRC" "$PHOTOS_DST" -v

echo "Verifying photos"
rclone check "$PHOTOS_SRC" "$PHOTOS_DST" --one-way

echo "Photos backup completed"