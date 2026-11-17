#!/bin/bash
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