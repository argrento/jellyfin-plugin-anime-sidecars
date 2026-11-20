# Anime Sidecars for Jellyfin

A Jellyfin 10.11 plugin for anime releases that keep subtitles, dubs and extras in subfolders —
typical for BD/WEB releases from Russian trackers:

```
Jujutsu Kaisen/
  Jujutsu Kaisen 01 [BDRemux].mkv
  RUS Subs/RUS Subs [Crunchyroll]/Jujutsu Kaisen 01 [BDRemux].ass
  RUS Subs/RUS Subs [Anku & mutagenb]/Jujutsu Kaisen 01 [BDRemux].ass
  RUS Sound/RUS Sound [JAM CLUB]/Jujutsu Kaisen 01 [BDRemux].mka
  Bonus/NC/...
  Juju Stroll [SP]/...
```

Out of the box Jellyfin only finds external tracks next to the video, turns every subfolder into a
"season", and cannot parse episode numbers from names like `Show 01 [BDRemux].mkv`.

## What it does

- **External tracks from subfolders.** For every episode/movie it finds sidecar files in subfolders
  (file name = video name, or video name + `.something`) and links them into the item's
  *internal metadata folder*, named so Jellyfin shows them as proper tracks:
  `Jujutsu Kaisen 01 [BDRemux].Crunchyroll.ru.default.ass`
  - track title from `[...]` in the folder name (`RUS Subs [Crunchyroll]` → `Crunchyroll`);
  - language from tokens like `RUS` / `ENG` / `JPN` (configurable);
  - `forced` for "signs" folders (`надписи`, `signs`);
  - `default` for the first subtitle group from your priority list (audio is never made default).
- **Hides service folders** (`Bonus`, `*Sub*`, `*Sound*`, `*Dub*`, `NC`, `Creditless*`, `Menu`,
  `fonts`, `Scans`) so they stop appearing as seasons. Only folders that sit next to video files
  are hidden, never folders directly in a library root.
- **Fills missing episode numbers** for names Jellyfin rejects (`Show 01 [Tag]`, `Show (01 сер.)`),
  assumes season 1 when there is no season folder (as Jellyfin itself does) and refreshes the
  episode from metadata providers. Episodes in a folder without a season number (e.g. `Specials [SP]`)
  are left unnumbered, because providers would mistake them for season 1.
- **Never writes to your media folders** — releases stay intact for seeding, read-only mounts work.
  Only files it created are ever touched (tracked in `.anime-sidecars.json` per item).

Sync runs after every library scan and from *Dashboard → Scheduled Tasks → Anime Sidecars: sync*.
The settings page has a per-series preview and a "sync this series" button.

## Install

### From the plugin repository (recommended)

1. *Dashboard → Plugins → Repositories → +*, URL:
   ```
   https://raw.githubusercontent.com/argrento/jellyfin-plugin-anime-sidecars/main/manifest.json
   ```
2. *Catalog → Anime Sidecars → Install*, then restart Jellyfin.

### Manually

Download the zip from [Releases](https://github.com/argrento/jellyfin-plugin-anime-sidecars/releases),
extract it into `<jellyfin data>/plugins/AnimeSidecars_<version>/` and restart Jellyfin.

## Compatibility

- Requires Jellyfin **10.11.8** or newer.
- **Developed and tested on Linux only**: Jellyfin 10.11.8 from the native Debian package and from
  the official Docker image.
- Windows and macOS are **untested**. The code is not Linux-specific and should fall back to copying
  files where symlinks are unavailable, but nobody has verified it there. Reports are welcome.

## Settings

*Dashboard → Plugins → Anime Sidecars*

| Setting | Default |
|---|---|
| Libraries | all (pick your anime library!) |
| Link external audio too | off |
| Placement | symlink (falls back to copy) |
| Fill missing episode numbers | on |
| Hidden folder patterns | `Bonus`, `*Sub*`, `*Sound*`, `*Dub*`, `NC`, `Creditless*`, `Menu`, `fonts`, `Scans` |
| Language tokens | `RUS,RU,RUSSIAN=ru`, `ENG,EN,ENGLISH=en`, `JPN,JAP,JP,JAPANESE=ja`, `UKR,UA=uk` |
| "Signs" folder words | `надписи`, `signs`, `forced` |
| Subtitle priority | empty (e.g. `Crunchyroll`, `Anku & mutagenb`) |

## Build

Requires the .NET 9 SDK.

```bash
dotnet test
scripts/package.sh 1.1.0.0      # -> out/anime-sidecars_1.1.0.0.zip
JELLYFIN_HOST=user@host ./deploy.sh   # dev helper: install over SSH on a Debian-package server
```

## Releasing

Push a tag `vX.Y.Z`. GitHub Actions runs the tests, builds the zip, publishes a release and adds the
version to `manifest.json`, so servers using the repository see the update.

## License

[GPL-3.0](LICENSE)
