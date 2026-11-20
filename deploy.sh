#!/usr/bin/env bash
# Developer helper: build the plugin and install it on a Jellyfin host over SSH
# (native Debian/Ubuntu package layout; asks for the sudo password on the host).
# Usage: JELLYFIN_HOST=user@host ./deploy.sh [version, default 1.0.0.0]
set -euo pipefail

HOST="${JELLYFIN_HOST:?set JELLYFIN_HOST=user@host}"
VERSION="${1:-1.0.0.0}"
PLUGIN_DIR="/var/lib/jellyfin/plugins/AnimeSidecars_${VERSION}"

cd "$(dirname "$0")"
ZIP="$(scripts/package.sh "$VERSION")"

ssh "$HOST" 'rm -rf /tmp/AnimeSidecars && mkdir -p /tmp/AnimeSidecars'
scp -q out/plugin/* "$HOST:/tmp/AnimeSidecars/"
ssh -t "$HOST" "sudo mkdir -p '$PLUGIN_DIR' && sudo cp /tmp/AnimeSidecars/* '$PLUGIN_DIR/' && sudo chown -R jellyfin:jellyfin '$PLUGIN_DIR' && sudo systemctl restart jellyfin && echo 'Installed $(basename "$ZIP") to $PLUGIN_DIR, Jellyfin restarted'"
