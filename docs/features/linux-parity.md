# Feature: linux-parity

> Close the gap between Windows WPF Quick Media Ingest and the Linux Avalonia head (`QuickMediaIngest.Desktop`). Fluent chrome, same ingest product. Not a MaterialDesign clone and not Wine.

## Approach (locked)

Do **not** compile `MainViewModel` into Desktop (`System.Windows`, `Dispatcher`, `MessageBox`). Extract WPF-free commands into Core / AppModel; Desktop binds those types. Windows adapters stay in `Platform/Windows/`. Linux adapters already in Core (`XdgAppPaths`, inotify, libsecret, `gio trash`, `xdg-open`, LUKS, uid 0).

A row is not done if only WPF gained a control. AXAML ≤800; Core files ≤200; composition root ≤10 lines per feature.

## What Linux has today

| Surface | Status |
|---------|--------|
| Window, Fluent dark, compiled bindings | Present |
| First-run Welcome / Got it (in-memory) | Present — persisted `onboarding.json` (LP-host) |
| Empty copy “No sources yet” | Present |
| Import / Dry run / Refresh **buttons** | Present — Import/Dry run call `IngestEngine`; Refresh opens drive picker |
| Local/`/media` scan + drive picker | Present (LP-scan) |
| Destination chip + naming template | Present — left-rail + command-bar Save location (44×88, same as Scan/Import); **LD-naming** checkboxes |
| Import / Dry run / delete-after / free-space / collisions | Present (LP-import) |
| Shoot list / select / skip / keyword / transport | Present (LP-shoots) |
| FTP add/test/browse + secrets + throttle | Present (LP-ftp) |
| Prefer-ADB + dual-FTP de-dupe | Present (LP-adb) — Import pulls with `AdbFileProvider` per serial; drive picker has DCIM + Pictures checkboxes |
| Magick preview + cache cap | Present (LP-thumbs) — Magick then libvips then HEIF/AVIF then ffmpeg; hashed JPEGs reused until import |
| Import progress + afterglow + Open folder | Present (LP-scene) — Copy n/N and Verify m/N overlap; files leave the grid as each dest hashes; elapsed/ETA; Open folder |
| Crash overlay + discarded fingerprints | Present (LP-crash) |
| Command bar overflow + notifications + 14px/`#007ACC` | Present (LP-chrome) |
| History / exclusions / feedback / prefs | Present (LP-history…prefs) |
| Queue / retry failed / resume pending | Present (LP-queue) |
| Pick/reject, stars, color labels | Present (LP-cull) |
| SHA-256 / 3-2-1 / XMP / hash catalog | Present (LP-post) |
| Watch-folder, split/merge, timezone, rename | Present (LP-watch) |
| HEIC/video/compare/ICC/cache purge | Present (LP-media) — AVIF/JXL/HIF plus extra camera RAW and video containers ingest on the same preview pipeline |
| Camera Wi-Fi FTP presets | Present (LP-wifi) |
| PTP/USB tether (gphoto2/gvfs) | Present (LP-ptp) |
| High-contrast, reduced motion, RTL, F1 | Present (LP-a11y) |
| Safe unmount + leftover reminder | Present (LP-eject) |
| Magick + libvips `--smoke-native` | Present |
| `.deb` install, uid-0 refuse | Present — visible `.desktop` files claim `x-content/image-dcf` (no `NoDisplay`) so Cinnamon/Nemo’s import AppChooser lists this app |

## Gap list

### Port (same behavior, Avalonia view)

| ID | Windows function | Linux gap |
|----|------------------|-----------|
| G-scan | Local + removable scan, Refresh, drive picker | LP-scan: mounts + overlay + inotify on first selected root |
| G-empty | Waiting-for-card + Refresh + Add FTP | Add FTP opens editor |
| G-dest | Browse destination, naming template, dest chip | LP-dest dest chip; **LD-naming** chips/presets/preview |
| G-import | Copy ingest, dry-run plan, delete-after, free-space, collisions | LP-import: Core `IngestEngine` + `gio trash` |
| G-shoots | Shoot groups, select all, skip folder, keyword filter, transport badge | LP-shoots: `ItemGroup` list after scan |
| G-ftp | Add/test/browse FTP, password store, throttle | LP-ftp: overlay + libsecret/0600 + KB/s cap |
| G-adb | Prefer-ADB when `adb` on PATH | LP-adb: `AdbDeviceProbe` + alias de-dupe |
| G-thumbs | Preview grid | LP-thumbs: Magick → libvips → HEIF/AVIF → ffmpeg + Avalonia Image |
| G-scene | Full-bleed progress, afterglow, Open folder | LP-scene: overlay + `xdg-open` |
| G-queue | Queue / retry failed / resume pending | LP-queue: `pending-import.json` + in-memory jobs + retry failed paths |
| G-crash | Pending crash review + discarded fingerprints | LP-crash: Close marks discarded fingerprint |
| G-history | Search / filter / export CSV | LP-history: overlay filter + CSV + `import-history.json` |
| G-excl | Scan exclusions editor | LP-excl: folder prefixes + `scan-exclusions.json` |
| G-feedback | Composer, GitHub search, Esc/Ctrl+Enter | LP-feedback: preview + fail-soft search + shortcuts |
| G-prefs | Theme, language, naming, GPS strip, settings search, JSON import/export | LP-prefs: `prefs.json` + overlay search |
| G-about | Version, donate, filename-version updates | LP-about: Venmo + GitHub `.deb` filename check |
| G-prompt | `IUserPrompt` overlays | LP-prompt: dry-run Notify; delete-after Confirm stays destructive |
| G-cull | Pick/reject, stars, color labels, persist across rescan | LP-cull: Pick/Reject + stars/color; `CullSelectionPersistence` on Refresh |
| G-post | SHA-256 manifest, 3-2-1 second dest, XMP/copyright, already-imported catalog | LP-post: prefs + `checksums.sha256` + `import-hashes.json` |
| G-watch | Watch-folder, shoot split/merge, timezone, batch rename, dest tokens | LP-watch: Split/Merge/Rename + watch folder + dest tokens |
| G-media | HEIC via vips, video first-frame/proxy, optional ffmpeg, compare view, ICC, cache purge | LP-media: AVIF/JXL/HEIC/vips + ffmpeg frame + compare + Linux ICC + purge |
| G-wifi | Camera Wi-Fi FTP folder presets | LP-wifi: Sony/Canon/Nikon/Fuji/Panasonic remote-folder presets |
| G-chrome | Command bar overflow, notifications flyout, 14px / `#007ACC`, 44px Import | LP-chrome: compact overflow + flyout + 14px/`#007ACC`/44×88 |
| G-a11y | High-contrast, reduced motion, RTL, F1 shortcuts, live regions | LP-a11y: GTK_THEME / env, RTL FlowDirection, F1 overlay, live region |
| G-eject | Safe unmount + leftover-files reminder | LP-eject: `gio mount -u` / `udisksctl unmount` + leftover Notify |
| G-ptp | PTP/USB tether (FOSS libusb) | LP-ptp: gvfs gphoto2 mounts + `gphoto2 --auto-detect` |
| G-onboard | Persist `IsFirstRun` | LP-host: `onboarding.json` |

### Adapt (Linux equivalent, not a clone)

| Windows | Linux |
|---------|--------|
| MaterialDesign sidebar / chips | Avalonia Fluent command bar + optional rail |
| Shell/WIC thumbs | Magick + NetVips (already native-smoked) |
| Credential Manager | `secret-tool` + 0600 file (shipped) |
| BitLocker hint | LUKS via `findmnt` (shipped) |
| `%AppData%` | `XdgAppPaths` (exists; Desktop still constructs a bare AppModel) |
| Recycle Bin | `gio trash` |
| `SystemParameters.HighContrast` | `GTK_THEME` / `org.gnome.desktop.a11y.interface` high-contrast |
| Windows system theme | `color-scheme` / Fluent `RequestedThemeVariant` |
| WiX MSI | `.deb` (shipped) |
| SQLite VACUUM | JSON stores; skip SQLite on Desktop |

### Won’t port (stay Windows)

| Function | Why |
|----------|-----|
| WPD/MTP phone import | Windows Portable Device stack |
| iPhone USB (WPD) | Same |
| WMI device watcher / BitLocker APIs | Linux uses inotify + LUKS |
| Exact MaterialDesign pixel chrome | Fluent is the Linux look |
| WiX / portable EXE smoke | Windows distribution |
| `System.Data.SQLite` maintenance DB | WPF-only |

Native AOT stays out of v1 (trim already on).

## Remaining after LP (2026-09-15 Mint glance)

LP 43–71 shipped ingest *commands*. LD 1–5 shipped the photographer-facing **view** (naming chips, shoot cards, thumbs grid, filters, import prefs). Remaining items below are later polish (not Won’t-port).

### File naming (reported)

Windows Settings → File naming scheme is a **builder**, not a text field:

- Presets: Recommended (Date + Shoot + Original), Date + Time + Shoot + Original, Shoot + Date + Original, Custom
- Toggles: Date, Time, Sequence, Shoot name, Original name
- Date format (`yyyy-MM-dd` / `yyyyMMdd`), time format, separator `_` / `-`
- Shoot-name sample + force lowercase + live preview examples
- Core token palette still in `MainViewModel.Naming`: `[Date]` `[Time]` `[TimeMs]` `[YYYY]` `[MM]` `[DD]` `[HH]` `[mm]` `[ss]` `[fff]` `[ShootName]` `[Original]` `[Sequence]` `[Ext]` plus `_` / `-` (click-to-insert chips; WPF XAML palette was cut, Settings toggles remain)
- Folder tokens in `GroupFolderNaming`: `[Date]` `[YYYY]` `[MM]` `[DD]` `[ShootTitle]` `[Job]` `[Client]` `[Camera]`
- Per-source naming templates on Windows (`NamingTemplateBySource`)

Linux: Save location sheet uses always-visible **checkboxes** for file-name parts (Date/Time/Sequence/Shoot/Original) and folder parts (Shoot title/Date/Year/Month/Day/Job/Client/Camera). Tokens append in tick order. Presets, format/separator, and live preview remain. Hidden Preferences still has the old token palette. Per-source naming map still later.

### Shoot list & previews

| Windows | Linux now |
|---------|-----------|
| Expandable shoot card: editable title, keywords, folder path, start/end, file count, ignore-folder | Present (LD-shoot-card) |
| Thumbnail wrap-grid per shoot, zoom 50–300 | Present (LD-thumbs-grid, Magick cache) |
| Large selected preview, resizable right column | Present — splitter pane; full-resolution JPEG (embed or source), fitted to the column |
| Remembered window size and position | Present — `prefs.json` Window* + PreviewPaneWidth |
| Group-by hours slider, file-type filter, keyword **filter chips** | Present (LD-filters) |
| Expand all groups, rebuild previews | Expand-all present; rebuild still Magick purge |
| RAW+JPEG stack grouping | Present (LD-prefs-import) |

### Preferences wall (still thin on Linux)

Windows Import Settings still has: embed keywords, open destination when done, disk-space summary modal, confirm-cancel, single-thread import, file cooldown, thumbnail performance, FTP thumb cap, Prefer-ADB hint copy, save/load preset. Linux now also has dest presets, duplicate policy, verification, RAW+JPEG stack, and confirm-before-import (LD-prefs-import).

### Adapt / later (not a clone)

- Sidebar sources rail (Fluent, not MaterialDesign) — present; UX polish (UX-1–41) archived in `COMPLETED_TASKS.md`.
- Post-delete Recycle Bin recovery banner (`gio trash` analog)
- Per-source naming map

## Plan (BUILD_PLAN **43–71**, named LP)

Sequential. One feature row per agent task. Extract to Core first when logic still lives in `MainViewModel`.

**Wave A — ingest-bench v1** (promised LX: local/FTP/ADB, Import/Dry run, dest+naming, first-run, crash)

1. **LP-host** — Composition: `XdgAppPaths`, persist first-run, Avalonia `IUserPrompt` stub host, load crash store into AppModel
2. **LP-scan** — Local + `/media`/`/run/media` removable, Refresh, drive-select overlay
3. **LP-dest** — Folder picker, naming template, dest chip
4. **LP-import** — Real Import + Dry run + delete-after + free-space + collisions + `gio trash`
5. **LP-shoots** — Shoot list, select, skip folder, keyword filter, transport badge
6. **LP-ftp** — Add/test/browse + libsecret + throttle
7. **LP-adb** — Prefer-ADB if `adb` on PATH + dual-FTP de-dupe
8. **LP-thumbs** — Magick/Vips/HEIF/AVIF/ffmpeg previews + cache cap
9. **LP-scene** — Progress scene + afterglow + `xdg-open`
10. **LP-crash** — Overlay wired to pending-crash + discarded fingerprints
11. **AUTO LP-v1-smoke** — `pack-deb.sh` + `--smoke-native` after Wave A

**Wave B — chrome + Golden Path overlays**

12. **LP-chrome** — Command bar, overflow, notifications, 14px/`#007ACC`, 44×88 Import
13. **LP-history** — History search/CSV
14. **LP-excl** — Scan exclusions editor
15. **LP-feedback** — Feedback + GitHub search + shortcuts
16. **LP-prefs** — Preferences (theme, language, naming, GPS strip, search, JSON import/export)
17. **LP-about** — About, donate, filename-version updates
18. **LP-prompt** — Non-destructive confirms via `IUserPrompt` (keep delete-after destructive)

**Wave C — workflow + media (I-11…I-48 on Linux)**

19. **LP-queue** — Queue / retry / resume pending
20. **LP-cull** — Pick/reject, stars, labels, persist cull
21. **LP-post** — Checksums, 3-2-1, XMP/copyright, hash catalog
22. **LP-watch** — Watch-folder, split/merge, timezone, batch rename, dest tokens
23. **LP-media** — HEIC/AVIF/JXL/vips, video first-frame/proxy, optional ffmpeg, compare, ICC
24. **LP-wifi** — Camera Wi-Fi FTP presets
25. **LP-ptp** — PTP/USB tether via libusb (not WPD)
26. **LP-a11y** — High-contrast, reduced motion, RTL, F1, live regions
27. **LP-eject** — `gio mount -u` / udisks unmount + leftover reminder
28. **AUTO LP-parity-pack** — trimmed `.deb` after Wave C
29. **HUMAN** — Mint visual glance of ingest-bench (optional; `check-theme-qa` analog)

### Parallelization

Wave A is Sequential until Import/Dry run/scan public API is locked on AppModel. After **LP-import** ✅, Wave B overlay rows may `/scope` in Parallel if path prefixes do not overlap (`Desktop` views vs Core services). Wave C stays Sequential after Wave B prefs/history types exist.

### Critique

| Issue | Resolution |
|-------|------------|
| Null/empty at boundary | Folder/FTP/ADB paths validated in Core (existing parsers); Desktop disables Import when dest or selection empty; tests on AppModel |
| Network timeout | FTP/GitHub already have timeouts in Core; Desktop must not block UI thread — `IUiDispatcher` extract in LP-host; test fake slow FTP |
| Race (scan vs import) | Reuse import/scan cancellation tokens from Core; DebouncedPathWatcher 500ms/3s already specified; test overlapping Refresh |
| Unhandled exceptions | LP-crash loads `FilePendingCrashStore`; Desktop `AppDomain`/`UnobservedTask` hooks in LP-host; trim keeps compiled bindings (already fixed false crash overlay) |
| MainViewModel WPF types leak | LP-host forbids `System.Windows` in Desktop/Core; gate probe `test_linux_parity.py` greps Desktop for `System.Windows` |
| AXAML file limit | Split overlays into `QuickMediaIngest.Desktop/Views/`; `check-file-limits.sh` already scans `*.axaml` |
| WPD/iPhone expected by photographers | Documented Won’t port; Prefer-ADB + FTP + local cover Mint phones; no AGENT row that cannot compile on Linux |
| Scope explosion | One BUILD_PLAN row per slice; `/feature` does not batch Wave B+C |

## Acceptance criteria

- 🔲 User can scan a local folder or SD mount, pick dest, Dry run, then Import into dated shoot folders
- 🔲 FTP and ADB-if-PATH work without WPF
- 🔲 Overlays (history, exclusions, feedback, prefs, about) are real, not title stubs
- 🔲 Offline: local ingest works; FTP/GitHub fail-soft
- 🔲 Keyboard: Import, overlays Esc, Feedback shortcuts; compiled bindings survive trim
- 🔲 i18n: existing `QuickMediaIngest.Localization` keys; no new English-only AXAML for user strings

## Smoke scenario

1. Given a Mint install of the trimmed `.deb` and a folder of JPEGs
2. When the user Refresh, selects files, sets dest, Dry run, then Import
3. Then files land in a dated shoot folder; afterglow offers Open folder; no false crash overlay; stderr clean

## Container map

| Layer | Path |
|-------|------|
| Logic | `QuickMediaIngest/Core/`, `QuickMediaIngest/Core/AppModel/` |
| View | `QuickMediaIngest.Desktop/` (`*.axaml` ≤800) |
| Tests | `QuickMediaIngest.Core.Tests/`, `scripts/lib/test_linux_parity.py` |
| Wiring | `QuickMediaIngest.Desktop/App.axaml.cs` ≤10 lines per feature |

## Tests

- Automated: yes — Core.Tests per slice + `scripts/lib/test_linux_parity.py` (Desktop must not reference `System.Windows`)
- Coverage: AppModel commands plus one pack-deb smoke after Wave A and Wave C

## Fallback validation

- Why WPF STA tests are not the Linux bar: `net8.0-windows` cannot run on Mint
- Command: `dotnet test QuickMediaIngest.Core.Tests` and `bash scripts/pack-deb.sh`

## Definition of Done

Each LP row: Core extract if needed, Avalonia view, Core.Tests, i18n keys, `watch-agent-gates --once --autofix --scope auto`. After last AGENT/AUTO in a wave: `smoke-sprint --require`.
