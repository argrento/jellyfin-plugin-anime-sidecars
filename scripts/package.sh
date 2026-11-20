#!/usr/bin/env bash
# Build the plugin and pack it as a Jellyfin plugin zip.
# Usage: scripts/package.sh <version, e.g. 1.1.0.0>
# Output: out/anime-sidecars_<version>.zip (prints its path)
set -euo pipefail

VERSION="${1:?usage: scripts/package.sh <version>}"
TARGET_ABI="10.11.8.0"
export PATH="$HOME/.dotnet:$PATH" DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/out"

rm -rf "$OUT"
dotnet publish "$ROOT/src/Jellyfin.Plugin.AnimeSidecars" -c Release -p:Version="$VERSION" -o "$OUT/publish" >/dev/null

mkdir -p "$OUT/plugin"
cp "$OUT/publish/Jellyfin.Plugin.AnimeSidecars.dll" "$OUT/plugin/"
cat > "$OUT/plugin/meta.json" <<JSON
{
  "category": "Metadata",
  "changelog": "",
  "description": "Links subtitles/audio from release subfolders as external tracks, hides service folders and fills missing episode numbers.",
  "guid": "5f2c1d7e-8a3b-4c9e-9b61-2e7d4a0c3f18",
  "name": "Anime Sidecars",
  "overview": "External tracks from anime release subfolders",
  "owner": "argrento",
  "targetAbi": "$TARGET_ABI",
  "timestamp": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "version": "$VERSION",
  "status": "Active",
  "autoUpdate": true,
  "assemblies": []
}
JSON

ZIP="$OUT/anime-sidecars_${VERSION}.zip"
(cd "$OUT/plugin" && zip -q -j "$ZIP" ./*)
echo "$ZIP"
