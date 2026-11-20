#!/usr/bin/env python3
"""Add a release to the Jellyfin plugin repository manifest.

Usage: update-manifest.py <manifest.json> <version X.Y.Z.0> <zip path> <download url> [changelog]
"""
import datetime
import hashlib
import json
import sys

GUID = "5f2c1d7e-8a3b-4c9e-9b61-2e7d4a0c3f18"
TARGET_ABI = "10.11.8.0"


def main() -> None:
    manifest_path, version, zip_path, url = sys.argv[1:5]
    changelog = sys.argv[5] if len(sys.argv) > 5 else ""

    with open(zip_path, "rb") as f:
        checksum = hashlib.md5(f.read()).hexdigest()

    try:
        with open(manifest_path, encoding="utf-8") as f:
            manifest = json.load(f)
    except FileNotFoundError:
        manifest = []

    plugin = next((p for p in manifest if p.get("guid") == GUID), None)
    if plugin is None:
        plugin = {
            "guid": GUID,
            "name": "Anime Sidecars",
            "description": "Links subtitles and audio from anime release subfolders "
            "(e.g. 'RUS Subs [Group]') as named external tracks, hides service folders "
            "so they stop showing up as seasons, and fills missing episode numbers.",
            "overview": "External tracks from anime release subfolders",
            "owner": "argrento",
            "category": "Metadata",
            "versions": [],
        }
        manifest.append(plugin)

    plugin["versions"] = [v for v in plugin["versions"] if v.get("version") != version]
    plugin["versions"].insert(0, {
        "version": version,
        "changelog": changelog,
        "targetAbi": TARGET_ABI,
        "sourceUrl": url,
        "checksum": checksum,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    })

    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
        f.write("\n")


if __name__ == "__main__":
    main()
