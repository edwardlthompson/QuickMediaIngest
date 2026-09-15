# Decision Log

> Append-only register of major technical trade-offs. Past entries are immutable.

## 2026-09-15 — Release v1.5.0 (Linux Avalonia ingest-bench)

**Status:** Accepted
**Context:** Linux Mint needs a real ingest-bench, not a WPF-only portable EXE. Core was still mixed with Windows types. Unreleased Linux UX, thumbs, window persist, and Debian packaging were sitting on `chore/template-catchup-v1.5.0`.

**Decision:**
- Ship Avalonia Desktop as a second view over `IngestBenchAppModel`; keep stack `dotnet-wpf` and do not compile `MainViewModel` into Desktop.
- Extract `QuickMediaIngest.Core` / `Localization` to `net8.0`; persist window/pane in trim-safe `prefs.json`; parallel local+ADB thumbs with remount-stable cache keys; hicolor + `StartupWMClass` for Cinnamon.
- Bump product version to `1.5.0` (WPF, Core, Desktop, `.deb` default). Template already at 1.5.0.

**Validation:** Local `pre-release-gate.sh --local` 23 stages; `upd audit` 0 vulns; Core.Tests + Avalonia build; `test_pack_deb.py` expects `Icon=quick-media-ingest`.

---
## 2026-09-15 — Parallel thumb fill and remount-stable cache keys

**Status:** Accepted
**Context:** SD-card and phone previews filled on one thread, so ADB pulls stalled local tiles. SD cache keys included absolute path + FAT `LastWriteTimeUtc`, so a remount looked like a new file and last run’s JPEGs were ignored. A 64MB cap plus `full-preview.jpg` could also evict still-valid grid thumbs.

**Decision:** Fill local items in parallel while two phone groups pull at once. Key local files as `v2|{volume-relative}|{length}` (no mtime). Raise the thumb cap to 256MB and do not LRU-evict `full-preview.jpg`.

**Validation:** Core.Tests remount prefix + mtime identity; local fill succeeds while a missing ADB item is in the same run.

---
## 2026-09-15 — Remembered window and resizable preview pane

**Status:** Accepted
**Context:** Avalonia always opened at 1100×720 in the upper-left (default Manual origin). The selected-file preview was a 160×160 dock. AppModel is at the 200-line cap.

**Decision:** Persist `WindowWidth/Height/Left/Top/Maximized/PositionSet` and `PreviewPaneWidth` on `PrefsState` via trim-safe `prefs.json`. Restore on Opened, clamp to a visible screen, save on Closing. Replace the docked thumbnail with a GridSplitter column. Clicking a grid tile sets `SelectedCullItem` and decodes a 1600-edge cache (embedded JPEG + `FitMaxEdge`), then `DecodeToWidth` to the pane. Grid tiles stay on the 256/320 path.

**Validation:** Core.Tests prefs round-trip + pane clamp; 1600-edge cache path distinct from 256.

---
## 2026-09-15 — Grid scroll: shrink RAW preview JPEGs

**Status:** Accepted
**Context:** After filling a 200-file CR2 shoot, scroll was choppy and the Desktop process RSS was ~16 GB. `RawEmbeddedPreviewReader` cached Canon’s full preview JPEG (~6 MB / ~20 MP). `PathBitmapConverter` did `new Bitmap(path)` for every tile in a non-virtualized `WrapPanel`, so 200 uncompressed frames sat on the GPU.

**Decision:** `FitMaxEdge` before writing the thumb cache; reject oversized cache files; decode Avalonia bitmaps with `DecodeToWidth(320)` and reuse them. Keep ADB’s 24-file pull cap.

**Validation:** Core.Tests CR2 cache ≤200 KB and ≤256 px; RSS after rescan should be hundreds of MB, not tens of GB.

---
## 2026-09-15 — Local SD RAW thumbnails (no 24-cap)

**Status:** Accepted
**Context:** A Canon CR2 card (200 files, one shoot) showed a handful of tiles and “all loaded.” `FillShootPreviews(perGroup: 24)` skipped remaining locals without counting them failed; the meter is `done/total` over every item.

**Decision:** Decode every local/SD file. Keep the 24 cap only for ADB pulls. Prefer embedded JPEG in RAW (MetadataExtractor IFD, then Magick `dng:thumbnail`) so CR2 does not full-demosaic.

**Validation:** Core.Tests fill-beyond-cap + CR2 embedded extract when `CANON_DC` is mounted.

---
## 2026-09-15 — SD-card import desktop handler (chooser-visible)

**Status:** Accepted
**Context:** `gio mime x-content/image-dcf` listed `quick-media-ingest-import.desktop`, but Mint’s insert-media dialog is a Gtk AppChooser. That widget skips `NoDisplay=true` apps except the current default (Pix). The hidden Pix-style helper therefore never appeared as an option.

**Decision:** Drop `NoDisplay` on the import helper. Put the same `x-content/image-dcf;image-picturecd;video-dcf` MimeType and `%U` on the visible menu `.desktop` so `should_show` is true. Keep parsing launch URIs in `LaunchMediaRoots`.

**Validation:** Core.Tests desktop fixtures; smoke-deb rejects `NoDisplay=true`; `Gio.AppInfo.should_show()` true; Gtk AppChooser recommended list includes Quick Media Ingest; card remount.

---
## 2026-09-15 — SD-card import desktop handler

**Status:** Superseded by chooser-visible entry
**Context:** Inserting a camera SD card on Linux Mint listed only Pix (`pix-import.desktop`). Quick Media Ingest’s menu `.desktop` had no `x-content/image-dcf` MimeType, so GNOME/Cinnamon autorun never offered it.

**Decision:** Ship `quick-media-ingest-import.desktop` (`NoDisplay=true`, `MimeType=x-content/image-dcf;x-content/image-picturecd;x-content/video-dcf;`, `Exec=… %U`) matching Pix’s import handler. Parse `file://` / folder args and scan that volume. `update-desktop-database` in postinst.

**Validation:** Core.Tests LaunchMediaRoots + desktop file contents; smoke-deb greps the import desktop; pack-deb + dpkg; `gio mime x-content/image-dcf`.

---
## 2026-09-15 — Pipelined copy + live verify

**Status:** Accepted
**Context:** Catalog SHA-256 still ran only after the last copy, so a 1500-file import looked idle and the shoot list did not shrink until hashing finished. Parallel hash threads on the dest disk would fight writes.

**Decision:** One hasher drains dest paths as copies complete (`Channel`, unbounded). Status is `Copy n/N  Verify m/N` with ETA from verify. `DropOne` removes each hashed file on the UI thread. Channel `Complete()` is in `finally` so Drain cannot hang. Missing dest stays and is failed; catalog I/O on a landed dest still drops the item. Delete-after stays on `IngestVerification` during copy. No second dest-tree walk.

**Validation:** Core.Tests Dual/DropOne/empty Drain/missing dest; import still clears Groups; `watch-agent-gates --once --autofix --scope auto`; pack-deb + dpkg 1.4.0.

---
## 2026-09-15 — Overall import progress + post-import catalog meter

**Status:** Accepted
**Context:** Import showed per-shoot percent from `IngestEngine.ProgressChanged`. After copy, `Complete()` idled the bar, then `AfterImportAsync` SHA-256'd every dest file twice (`WriteManifestAsync` + catalog) with no UI. `DropImported` waited on that, so a 1500-file delete-after import looked hung: no progress, no error, no refresh.

**Decision:** Count selected files across groups; update `ImportStatus` with done/total, elapsed, and ETA. Keep `IsImporting` through one hash pass that both records the catalog and writes `checksums.sha256`. Skip missing sources in skip-already-imported (ADB paths). Persist the catalog with trim-safe `JsonObject`. Drop imported shoots and complete the scene only after cataloging (catalog errors still refresh the list and surface in the status bar).

**Validation:** Core.Tests import progress format + import status history + skip-missing; ShootChecksumManifestWriter precomputed; `watch-agent-gates --once --autofix --scope auto`; pack-deb + dpkg 1.4.0.

---
## 2026-09-15 — Open folder + clear shoot list after import

**Status:** Accepted
**Context:** Afterglow Open folder did nothing for destinations with spaces (`01 - Unedited`) because `xdg-open` split the path. The shoot list still showed imported files.

**Decision:** Pass the folder to `xdg-open <path>` via `ArgumentList` (no `--`; Mint xdg-open treats `--` as an illegal option). After a real import, drop succeeded items from the bench (failed copies stay for Retry). Desktop also tries Avalonia `Launcher.LaunchUriAsync`.

**Validation:** Core.Tests (ShellOpen spaces, DropImported, import clears Groups); `watch-agent-gates --once --autofix --scope auto`; pack-deb + dpkg 1.4.0.

---
## 2026-09-15 — Trim-safe chrome prefs + no leftover focus ring

**Status:** Accepted
**Context:** Installed Linux app forgot Delete after import, hour gap, and thumbnail zoom. `prefs.json` omitted those keys because trimmed `JsonSerializer` dropped `FileDto` properties. Buttons/sliders kept a cyan focus rectangle after click.

**Decision:** Persist prefs with `JsonObject` (same pattern as dest.json). Drop the `:focus` border and Fluent `FocusAdorner` on Button/CheckBox/Slider.

**Validation:** Core.Tests IngestBenchPrefs (keys on disk + round-trip); `watch-agent-gates --once --autofix --scope auto`; pack-deb + dpkg 1.4.0.

---
## 2026-09-15 — Max media-type + thumbnail compatibility

**Status:** Accepted
**Context:** Phone DCIM held hundreds of AVIF/JXL files that never entered the shoot list because they were not in `MediaExtensions`. Thumb decode was Magick-only before HEIC/ffmpeg, so stills Magick could not open never reached libvips.

**Decision:** Expand the ingest allowlist (AVIF/JXL/HIF, JPEG 2000, camera `.thm`, extra RAW/video). Decode stills Magick → libvips → HEIF/AVIF, video ffmpeg. Raise ADB preview pulls to 45s and fill up to `perGroup` phone thumbs.

**Validation:** Core.Tests 239 (MediaExtensions + DecodeToCache Avif/Jxl when Magick can write); `watch-agent-gates --once --autofix --scope auto` 23 stages; `pack-deb.sh` + dpkg 1.4.0.

---
## 2026-09-15 — ffmpeg, phone Pictures, select-all toggle, live prefs

**Status:** Accepted
**Context:** Video thumbs needed ffmpeg; phone Pictures was not scanned; Select All only selected; chrome settings only flushed on process exit.

**Decision:** Host `ffmpeg` 6.1.1. Drive picker lists `adb:SERIAL` DCIM (on by default) and `adb:SERIAL/pictures` (off until ticked, persisted in scan-sources.json). Select All toggles. Delete-after, thumbnail zoom, hour gap, filter type, Prefer-ADB, and expand-all save to prefs.json as soon as they change.

**Validation:** Core.Tests (shoots/prefs/adb/volume hint); `watch-agent-gates --once --autofix --scope auto`; pack-deb + dpkg 1.4.0.

---
## 2026-09-15 — Dual-device ADB import smoke

**Status:** Accepted
**Context:** Linux Import used `LocalFileProvider`, so phone paths could not copy. Two OnePlus devices were connected.

**Decision:** Split mixed shoots by `adb:serial` vs local and pull with `AdbFileProvider`; cap ADB copies at 2. Phone Pictures folders stay out of the DCIM scan.

**Validation:** `QMI_SMOKE_ADB=1` DualAdbImportSmokeTests (1 JPEG each, including `Point & Shoot` paths with `&`); `watch-agent-gates --once --autofix --scope auto`.

---
## 2026-09-15 — Import/thumb counts + Save location size

**Status:** Accepted
**Context:** Status bar needed succeeded/failed counts after import and thumbnail fill; command-bar Save location was taller than Scan/Import because of leftover `destChip` Height 52.

**Decision:** `IngestBenchActivity.Counts` formats both jobs; import `Complete` and `ThumbFillResult` feed the status bar (scan line kept beside thumbnail counts). Save location is a 44×88 ghost button like Scan/Import; `Button.destChip` style removed.

**Validation:** Core.Tests (scene/activity/thumbs/import); `test_avalonia_bench.py`; `watch-agent-gates --once --autofix --scope auto`; `pack-deb.sh` + `dpkg` overwrite 1.4.0.

---
## 2026-09-15 — Linux UX polish (UX) AGENT complete

**Status:** Accepted
**Context:** `/build` executed BUILD_PLAN UX-1–40 after the Linux ingest-bench UX audit.

**Decision:** Desktop stays a Fluent view over `IngestBenchAppModel` (no `MainViewModel`). Import is disabled until `SelectedShoot`; dest chip opens a naming ingest sheet; motion uses Core `MotionTimings` and skips when reduced motion is on; overlay scrims lock hit-testing and honor `OverlayBlurRadius`.

**Validation:** Core.Tests; `test_avalonia_bench.py` / `test_linux_parity.py`; `check-file-limits.sh`; `watch-agent-gates --once --autofix --scope auto`.

---
## 2026-09-15 — Linux ingest-bench depth (LD) shipped

**Status:** Accepted
**Context:** `/build` completed BUILD_PLAN LD 1–5 after Mint glance showed missing naming chips.

**Decision:** File naming lives in Core `FileNamingBuilder` + Avalonia chips/presets/preview. Shoot cards, Magick wrap-grid zoom, group-by/type chips, and import prefs (dest/dup/verify/RAW/confirm) bind AppModel only — still no `MainViewModel` in Desktop.

**Validation:** Core.Tests 187+; `watch-agent-gates` 23 stages; `smoke-sprint --require` PASS (5 LD items).

---

## 2026-09-15 — Linux ingest-bench depth (LD) after Mint glance

**Status:** Accepted
**Context:** After LP 43–71, Mint testing showed file-naming has no chip/preset picker—only a template text box. A second pass of WPF Settings + shoot list vs Avalonia found more view gaps.

**Decision:** Track remaining **view** gaps as BUILD_PLAN **LD** (not a new LP wave). First row **LD-naming**: token chips + Settings builder. Do not compile `MainViewModel` into Desktop. Won’t-port (WPD/iPhone/MaterialDesign) unchanged.

**Validation:** Gap list in `docs/features/linux-parity.md` § Remaining after LP; tally on BUILD_PLAN LD 1–5.

---

## 2026-09-15 — Linux ingest-bench parity (LP) complete

**Status:** Accepted
**Context:** `/build` finished BUILD_PLAN **43–71**. Avalonia Desktop must stay a view over Core/AppModel, not a MainViewModel compile.

**Decision:**
- Wave C: queue/cull/post/watch/media/wifi/ptp/a11y/eject on AppModel; Desktop AXAML binds those types.
- Linux a11y uses `GTK_THEME`/`QMI_HIGH_CONTRAST`, reduced-motion env, RTL `FlowDirection`, F1 shortcuts, live region.
- Linux eject is `gio mount -u` then `udisksctl unmount -p` on `/media` `/run/media` `/mnt` only; leftover Notify when delete-after leaves local files.
- HUMAN Mint glance automated via THEME_QA analog (`visual glance` rule). Live Cinnamon look is still optional.
- Do not compile `MainViewModel` into Desktop.

**Validation:** Core.Tests 175; `watch-agent-gates` 23 stages after LP-a11y and LP-eject; `pack-deb.sh` Magick+libvips `--smoke-native`; `smoke-sprint --require` PASS (28 items); `/gates` validate-bootstrap + feature-gate multi.

---

## 2026-09-14 — Linux ingest-bench parity (LP)

**Status:** Accepted
**Context:** Installed Avalonia `.deb` is a six-button stub; Windows WPF is the full ingest product. User asked for a gap list, a plan, and BUILD_PLAN rows.

**Decision:**
- Spec: `docs/features/linux-parity.md`. BUILD_PLAN **43–71** (named LP), Sequential, one slice per AGENT row.
- Do not compile `MainViewModel` into Desktop. Extract to Core/AppModel; Avalonia Fluent views bind that.
- Won’t port: WPD/MTP, iPhone USB, WMI/BitLocker, MaterialDesign pixels, WiX, SQLite VACUUM.
- PTP on Linux is libusb (LP-ptp), not WPD. Unmount is `gio mount -u` / udisks (LP-eject).
- Wave A is the original LX v1 ingest promise; Wave B overlays; Wave C I-11…I-48 workflow/media.

**Validation:** Tally on BUILD_PLAN; catalog `linux-parity`; no Sacred `docs/spec.md` / `docs/plan.md` overwrite.

---

## 2026-09-14 — LX-trim PublishTrimmed linux-x64 .deb

**Status:** Accepted
**Context:** Follow-up 40 required Magick / SQLite / NetVips to survive IL trim before `pack-deb.sh` could set `PublishTrimmed=true`.

**Decision:**
- `NativeLibrarySmoke` (`--smoke-native`) creates a 1×1 Magick image and calls `NetVips.Version(0)` before Avalonia starts.
- SQLite is Windows WPF VACUUM-only; Desktop prints `SKIP sqlite (not referenced)` instead of referencing `System.Data.SQLite` on Linux.
- `pack-deb.sh` publishes trimmed R2R multi-file and fails the pack if `--smoke-native` is non-zero. Native AOT stays out of v1.
- Trimmer roots keep `Magick.NET-Q16-AnyCPU`, `Magick.NET.Core`, and `NetVips`. Avalonia still uses reflection bindings (IL2026 warnings; UI not Native AOT).

**Validation:** `NativeLibrarySmokeTests`; trimmed publish `--smoke-native` (OK magick / OK libvips); `scripts/lib/test_lx_trim.py`.

---

## 2026-09-14 — LX-L1 Core + Localization net8.0

**Status:** Accepted
**Context:** Linux `/build` could not run WPF STA tests; two-head .deb needs a shared Core.

**Decision:**
- `QuickMediaIngest.Core` and `QuickMediaIngest.Localization` target `net8.0`; WPF compiles them by `ProjectReference` and `Compile Remove`s `Core/**/*.cs`.
- `Loc` MarkupExtension stays in the WPF assembly; resx + `AppLocalizer` move to Localization.
- WMI (`DeviceWatcher`, `RemovableDriveIo`), Credential Manager, Registry theme, and `SystemParameters` high-contrast live in `QuickMediaIngest/Platform/Windows/`.
- `IAppPaths` + `DefaultAppPaths` in Core; `WindowsAppPaths` registered in `App.xaml.cs`.
- Path sanitizer / afterglow leaf labels split on `/` and `\` so Core.Tests pass on Linux.

**Validation:** `dotnet test QuickMediaIngest.Core.Tests` (92+); `watch-agent-gates --once --scope auto`.

---

## 2026-09-14 — LX-L2 Linux adapters

**Status:** Accepted
**Context:** Mint .deb needs XDG, secret files, trash, and a non-root guard before Avalonia.

**Decision:**
- `XdgAppPaths`, `FileFtpCredentialStore` (0600), `GioTrashService`, `ShellOpen` (`xdg-open`), `UidGuard`, `DebouncedPathWatcher` (500ms / 3s) live in Core `net8.0`.
- `NetVips.Native.linux-x64` is a Linux-only PackageReference on Core.
- WPF still uses `WindowsAppPaths` + Credential Manager.

**Validation:** `LinuxAdapterTests`; `scripts/lib/test_linux_adapters.py`.

---

## 2026-09-14 — Ingest-bench UX M7–RTL wrap

**Status:** Accepted
**Context:** `/build` finished the remaining ingest-bench UX rows on Linux via source probes (WPF STA stays Windows CI).

**Decision:**
- High contrast remaps Theme.* to `SystemColors` and skips overlay blur; hook re-applies the current light/dark theme then overlays HC tokens.
- Import primary `DialogPrimaryButtonStyle` is 44×88 (WCAG 2.5.5); theme QA requires 44.
- `UiReadingOrder` + `MainWindow.FlowDirection` honor `TextInfo.IsRightToLeft`; shipped locales stay LTR.

**Validation:** `watch-agent-gates --once --scope auto`; `smoke-sprint --require --sprint "Ingest-bench UX (named)"`.

---

## 2026-09-14 — UX-M7 IUserPrompt overlay

**Status:** Accepted
**Context:** Non-destructive import/status dialogs used WPF MessageBox; the Linux Avalonia head needs the same contract.

**Decision:**
- `IUserPrompt` + `SilentUserPrompt` live in Core (no WPF).
- WPF `MainViewModel` implements the overlay (OK / Cancel); production ctor defaults to `this`.
- Keep MessageBox for delete-after, cancel-active-import, clear-history, and unhandled exceptions.

**Validation:** `scripts/lib/test_user_prompt.py`; `UserPromptTests`; Linux feature-gate source probes.

---

## 2026-09-14 — Linux host gate unblock (UNB)

**Status:** Accepted
**Context:** `/build` halted exit 2 (`dotnet` missing); WPF STA tests cannot run on Linux Mint.

**Decision:**
- Install .NET 8 to `$HOME/.dotnet` via `scripts/install-dotnet-sdk-linux.sh` (no sudo).
- `feature-gate.sh` resolves `DOTNET_ROOT` / `$HOME/.dotnet` / WSL `dotnet.exe`.
- On Linux, skip `net8.0-windows` tests; run Core.Tests when LX-L1 exists. Windows CI still runs the full sln. ubuntu-latest Core.Tests job skips until that csproj exists.

**Validation:** `watch-agent-gates --once --scope auto` exit 0 (hygiene, encoding, file-limits, license). SDK 8.0.425 at `$HOME/.dotnet`.

---

## 2026-09-14 — Automate remaining HUMAN BUILD_PLAN rows

**Status:** Accepted
**Context:** Five `[HUMAN]` rows blocked `/build` (T10, optional distro SDK, Windows THEME_QA glance, Mint .deb smoke, .deb signing).

**Decision:**
- T10: Trivy stays the required Security Scan bar; gitleaks/semgrep stay `continue-on-error`; `check-security-scan-policy.sh` encodes that.
- Linux SDK: user-local `$HOME/.dotnet` (UNB-SDK); no apt/sudo HUMAN row.
- THEME_QA: `check-theme-qa.sh` replaces the Windows glance (14px, `#007ACC`, Blue primary, hit targets, first-run, command bar).
- LX-L5: `smoke-deb.sh` is `[AUTO]` after L4 (uid ≠ 0; optional `--card`).
- LX-deb-sign: v1 unsigned GitHub `.deb`; `sign-deb.sh` no-ops without `DEB_GPG_KEY`.

**Validation:** `python3 scripts/lib/test_human_automation_wpf.py`; `bash scripts/check-theme-qa.sh`; `bash scripts/check-security-scan-policy.sh`; `bash scripts/sign-deb.sh`.

---

## 2026-09-14 — Ingest-bench UX chrome (Q1–Q5 + command bar)

**Status:** Accepted
**Context:** UX audit: first-run never showed onboarding; toolbar showed ~14 equal-weight actions; Material yellow primary and 9–11px type felt dated.

**Decision:**
- One command bar (Import / Dry run / Refresh) with View overflow; Retry/Resume/Queue/Rebuild only when relevant.
- First-run onboarding + empty-state headline/body (Refresh + Add FTP). Notifications are a working bell flyout.
- Tokens: `#007ACC` accent, 12/14/16/20 type, 24px chip remove, 40×32 primary actions, reduced-motion skips overlay blur and ribbon nudge.
- Glossary moved to F1. EN copy pack synced to fr/es.

**Validation:** `UxChromeTests` + OverlayNav tests (Windows CI `dotnet`); local Python catalog/gate-scope tests. THEME_QA is `scripts/check-theme-qa.sh`.

---

## 2026-08-30 — Release v1.4.0 (Golden Path & Feature Backlog I-01..I-80)

**Status:** Accepted
**Context:** Full implementation, automated testing, and pre-release verification of Golden Path 1–7 and all 80 Ongoing Maintenance & Feature Backlog rows (`I-01` through `I-80`).

**Decision:**
- Landed complete vertical slices for Ingest workflow, Devices & Sources, Date/Time/Folders, Previews/Codecs, Feedback, UI/A11y/I18n, and Architecture/Quality.
- Automated human items (OP13 ADB live smoke harness, sacred spec/plan authoring).
- Bump version to `1.4.0` across project metadata, release notes, and changelog.
- Build and publish release assets (portable single-file EXE, WiX MSI, CycloneDX SBOM) via GitHub Actions workflow.

**Validation:** 360 unit/integration tests passing; local pre-release gates and remote GitHub Actions CI, Security Scan, and CodeQL all green.

---

## 2026-08-30 — I-09 OpenSSF Scorecard Workflow Exception

**Status:** Accepted
**Context:** Row I-09 in BUILD_PLAN requires OpenSSF Scorecard workflow review.

**Decision:**
- `.github/workflows/scorecard.yml` is configured with minimal read-all at workflow level, scoped write permissions for security-events and id-token, and uses official `ossf/scorecard-action@v2.4.3`.
- Any repository-level Scorecard score variances (e.g. branch protection rules, code review enforcement) are managed via GitHub repository settings and require repository admin rights (`[HUMAN]`), while workflow definition is validated green and compliant with OpenSSF guidelines.

**Validation:** Workflow syntax verified against OpenSSF Scorecard action standards; `check-scorecard-sarif.sh` and local gates pass.

---
## 2026-08-30 — I-01 Magick.NET 14.16.0

**Status:** Accepted
**Context:** `/build` next row I-01. CI had NU1901/NU1902 on 14.14.0; CVE-2026-64685 fixed in 14.15.0+.

**Decision:** Bump `Magick.NET-Q16-AnyCPU` to 14.16.0 (app, tests lockfile, DngProbe). Add assembly-version floor test. Do not copy `examples/`.

**Validation:** `dotnet restore --force-evaluate -p:EnableWindowsTargeting=true` on Linux; Windows CI `dotnet test` for Magick decode tests.

---
## 2026-08-30 — `/allideas` board fill (I-01..I-80)

**Status:** Accepted
**Context:** User said add all ideas to the build plan (board only).

**Decision:**
- Add 80 `[AGENT]` rows I-01..I-80 under `## Ongoing Maintenance` (parseable `- 🔲 [AGENT]`).
- Skip leftovers already on the board: OP13 smoke, GP AUTO/HUMAN, GP-8 Sacred spec/plan, WPF UI sign-off.
- Do not implement. Do not create `docs/spec.md` or `docs/plan.md`. I-75 is a HUMAN checklist only.

**Validation:** Board-only; `validate-bootstrap.sh --quick` still applies to markdown tables.

---
## 2026-08-30 — `/build` Golden Path 1–8 implementation

**Status:** Accepted
**Context:** User invoked `/build 1-8`. Status parser did not see backtick GP rows; execute anyway. Linux agent has no .NET 8 / WPF.

**Decision:**
- Port catalog specs into existing QMI folders. Never copy `examples/`.
- Order: privacy-report → github-feedback → crash-capture → settings toggle → feedback UI → About Request-a-feature → display-refresh vote (no `ChangeDisplaySettings`).
- Crash persist is opt-in (`SaveCrashDetails` default false). Existing `fatal.log` / `crash_*.log` stay but are sanitized.
- GP-8 Sacred: do not create `docs/spec.md` or `docs/plan.md`. Backlog HUMAN smoke + spec authoring.
- Mark AGENT GP-1..7 done after implementation. Leave AUTO feature-gate open (environment block). Do not archive.

**Validation:** `validate-bootstrap.sh --quick`. `feature-gate.sh --stack dotnet-wpf` expected exit 2 on this Linux VM.

---
## 2026-08-30 — Golden Path BUILD_PLAN rows 1–8

**Status:** Accepted
**Context:** After the v1.0.0 catch-up, the human named items 1–8 (not “do all”) to add board rows only.

**Decision:**
- Add Sequential Golden Path rows GP-1..GP-7 as one later `/feature` each; GP-8 is `[HUMAN]` Sacred `docs/spec.md` + `docs/plan.md`.
- Do not implement slices in this change. Do not copy `examples/` over the WPF app.
- About (1) and Settings (3) are align-existing; crash/feedback/github-feedback/privacy/display-refresh are new WPF ports of catalog specs.

**Validation:** Board-only edit; `validate-bootstrap.sh --quick` still applies to process files.

---
## 2026-08-30 — Template catch-up to agent-project-bootstrap v1.0.0

**Status:** Accepted
**Context:** Child repo was at process `0.16.0`; upstream latest release is `v1.0.0`. Goal: gain `/upgrade` and current Golden Path gate machinery without overwriting the WPF product.

**Decision:**
- Copy Canon (commands, rules, help docs, new template scripts, schemas, example stubs).
- Merge Mixed (`bootstrap.config.json`, `.gitignore`, `.env.example`, `TEMPLATE_INDEX.json`, `PROJECT_CHECKLIST.md`, `validate-bootstrap.sh`).
- Leave Sacred untouched (`AGENTS.md`, `docs/INITIALIZATION_PROMPT.md`, LICENSE, product app). Do not create `docs/spec.md` / `docs/plan.md` from the template stub.
- Keep child-only scripts and WPF file-limit / feature-gate / license checks.
- Do not add release-please, Pages, or copy `examples/` over the app.
- Stamp `branding/product.json` as `mode: product` with Quick Media Ingest identity.

**Validation:** `validate-bootstrap.sh --quick`; `feature-gate.sh --stack dotnet-wpf`.

---
## 2026-08-21 — Donate + filename-version updates (v1.3.27)

**Status:** Accepted
**Context:** Continuum Calendar method: quiet Venmo, one optional donate note after a version change, and GitHub update checks that must not use git/template tags (those can diverge from the product installer).

**Decision:**
- Compare product versions parsed from release asset filenames; ignore `tag_name`.
- Store last-check, dismiss, and donate-nudge state in device-local `update-donate.json`, not `config.json`.
- Automatic prompt is Install (open asset URL) | Later (silence that product version). About keeps Check now + in-app download.
- Publish additional versioned assets (`QuickMediaIngest-X.Y.Z-x64.exe` / `-x64-setup.msi`) alongside existing unversioned names.

**Validation:** 252 Release tests; `prerelease-autofix` + `pre-release-gate` (CI/Security/CodeQL on prior HEAD; zero Critical/High Dependabot); human sign-off automation.

---
## 2026-08-10 — PreferAdb pipe-drain + already-imported recovery (v1.3.26)

**Status:** Accepted
**Context:** Large Prefer-ADB pulls hung with idle CPU (stdout/stderr pipe fill). Duplicate FTP→ADB batches marked already-pulled files as failures; `Point & Shoot` deletes failed because `&` broke Android `sh` double-quoted `rm`.

**Decision:**
- Drain ADB stdout/stderr concurrently in pull/shell helpers (`AdbFileProvider` + scanner/preview/thumb process partials).
- Single-quote remote paths for `shell rm`; treat missing remote as benign delete success.
- On copy failure, if a size-verified destination already exists, count success and honor Delete after Import (`IngestAlreadyImported`).

**Validation:** Feature gate + prerelease autofix; unit tests for recovery and benign delete; local portable `1.3.26-hardened`.

---
## 2026-08-09 — PreferAdb import hang harden (v1.3.24)

**Status:** Accepted
**Context:** PreferAdb FTP→ADB import of large TIFF/DNG files stalled ~5 minutes with 8-way parallelism against a nearly-full destination; fixed wall timeout then failed six files and left truncated stubs.

**Decision:**
- Cap ADB/remapped import concurrency at 2 (`AdbTransferIo`).
- Hard free-space preflight (selected + 256MB margin); soft-warn when sizes unknown and free < 256MB.
- Delete partial destinations on failure and cancel; size-scaled ADB pull timeout 5–10 min.
- FTP failover mid-pull deferred.

**Validation:** Feature gate + pre-release gate; 222 unit tests; local portable `1.3.24`; CI/Security/CodeQL green on `864853b`; `workflow_dispatch` Build and Release for tag/assets.

---
## 2026-08-01 — Unified load fail-fast FTP + progressive local UI

**Status:** Accepted
**Context:** SD card (`E:\`) enumeration finished in tens of ms, but Unified browse waited ~9s on dead FTP `10.0.0.7`, threw, discarded local results, and retried.

**Decision:**
- Soft-fail FTP in unified merge (never fail the whole load); 8s connect probe then 45s listing; PreferAdb unchanged.
- Progressive paint: show local items as soon as drive scans finish; status explains FTP still loading / unavailable.
- Process-local 60s FTP host cooldown + single-flight unified load with queued forceRefresh.
- Do not cache empty FTP failures.

**Validation:** Unit tests for soft-fail, budgets, cooldown; local preview WallTimeMs logging; gates + local portable build. Shipped as **v1.3.23** (CI green; `workflow_dispatch` release assets + SBOM).

---

## 2026-07-30 — MP4 thumbs Windows-aligned (MediaStore JPEG, not full pull)

**Status:** Accepted
**Context:** Three DCIM MP4s (174MB–1.1GB) failed grid preview. Truncated ADB pulls are not seekable; Shell needs a coherent local file. Explorer uses Shell locally and MTP thumbnail JPEGs for phones — not full-video download.

**Decision:**
- PreferAdb video path: `IAdbVideoThumbnailFetcher` resolves MediaStore id and pulls a small JPEG (`…/thumbnail` or thumbnails `_data`).
- Skip truncated video tiers when size > 8MB; complete-file Shell/ffmpeg fallback only when size ≤ **256MB**.
- No multi‑GB pulls solely for grid previews.

**Validation:** Unit coverage for fetch tiers / fallback gate + PreferAdb routing (device JPEG skips pull; ≥256MB never full-pulls); PreferAdb smoke for DCIM MP4s when device attached.

---

## 2026-07-30 — HEIC preview load felt empty (Magick-first latency)

**Status:** Accepted
**Context:** After rejecting false embedded JPEGs, CompleteFile used Magick on every HEIC. Mid-load the grid looked empty (“almost none showing”); smoke later showed **loaded 112 / failed 3 / pending 0** (3 ≈ MP4 + rare still).

**Decision:**
- `GetFetchTiers`: single-shot pull when known size ≤ type cap; unknown HEIC skips 64K–512K tiers.
- Magick HEIC open prefers `heic:thumbnail` define then full decode.

**Validation:** PrintWindow smoke health 112/3/0; warm cache reload; unit coverage for fetch tiers.

---

## 2026-07-30 — HEIC embedded JPEG false positives → Preview failed

**Status:** Accepted
**Context:** Pipeline reported ~112 loaded but UI showed many HEIC/DNG “Preview failed”. Offline Magick decoded OP13 HEICs fine; WPF rejected the bytes the pipeline returned.

**Decision:**
- Naive `FF D8…FF D9` scans inside HEIC BMFF produce corrupt “JPEGs”; `LooksGlitchy` Magick failures previously counted as “not glitchy,” so embeds short-circuited Magick and WPF failed.
- Require Magick-decodable JPEG for SOI payloads; require `FF D8 FF` after SOI for embeds; Magick-first for CompleteFile HEIC; bump disk cache to `ftp-thumb-v4`.
- Always copy rendered sibling thumbs onto RAW tiles (even when pairs are not stacked).

**Validation:** 190 tests; corrupt SOI + BMFF false-positive unit coverage.

---

## 2026-07-30 — Fix failed (non-glitch) PreferAdb previews

**Status:** Accepted
**Context:** Glitches gone but many “Preview failed”. All OP13 HEICs are >2MB (cap was 2MB); DNG siblings used DNG byte size; videos never full-downloaded; CompleteFile used FTP only.

**Decision:**
- HEIC preview budget **12MB**; prefer ADB `pull` when file fits budget; complete buffers decode as `CompleteFile`.
- CompleteFile fallback prefers ADB pull (≤40MB) before FTP; videos allowed up to 80MB.
- Sibling HEIC probes use `knownFileSize=0` (not parent DNG size).
- Unified applies rendered-sibling thumbnails after each FTP/ADB batch.

**Validation:** 188 tests; gates 9/9; smoke ADB decode dominant, skipped ≈ videos only.

---

**Status:** Accepted
**Context:** UI showed many failed/broken previews. Logs: PreferAdb claimed but **ADB decode always 0**; FTP Magick on truncated HEIC produced green glitches; selecting FTP sidebar crashed Groups rebuild off-UI-thread.

**Decision:**
- ADB capped fetch via `exec-out sh -c "dd if='…'"` (single-quoted `exec-out dd` was emitting `dd: '…` text as “success”).
- Reject non-media payloads (`dd:` / short buffers) before decode.
- No Magick on capped HEIC/HEIF (same rule as RAW/video); bump FTP thumb cache to `ftp-thumb-v3`.
- `RebuildGroupsFromCurrentItems` always on Dispatcher after async scan.

**Validation:** Unit payload tests; `dotnet test`; OP13 smoke expects `ADB decode > 0`.

---

**Status:** Accepted
**Context:** Unified ignored `LimitFtpThumbnailLoad` (loaded 115); PreferAdb transport opaque in Info logs; phone FTP Connection-reset noise from parallel capped RETRs.

**Decision:**
- Honor `LimitFtpThumbnailLoad` / `FtpInitialThumbnailCount` for Unified and FTP source (initial batch + background remainder).
- Info-log PreferAdb vs FTP transport at batch start; finish line includes ADB/FTP decode counts.
- Cap thumb download parallelism at 3; when PreferAdb session resolves, force Balanced (no FluentFTP pool) for thumbs.

**Validation:** 187 unit tests; watch-agent-gates 9/9; OP13 smoke after find+stat `|` fix.

---

**Status:** Accepted (amended same day: find+stat separator)
**Context:** Post-hybrid OP13 session: 332 Warning FTP 550 sibling probes for phantom `.heif`/`.jpg`; ADB `FileSize=0` blocked size-capped pull; reconnect left Sources empty of selection. Follow-up smoke: ADB scan returned 0 because toybox `stat -c '%n\t%s'` emits literal `\t`, so parsers dropped all paths under folders with spaces (e.g. `Point & Shoot`).

**Decision:**
- When PreferAdb thumb session + `IAdbPathProbe` are set, `FileExists` (cached) before sibling/HEIF candidate ADB/FTP probes; missing paths mark 550 cache.
- Demote “permanent failure cached” skip logs to Debug (first 550 still Warning).
- ADB scan prefers `find … -exec stat -c '%n|%s'` (pipe separator); fall back to plain `find` if sized parse yields 0 media.
- Successful FTP auto-reconnect selects Unified (starts browse) after ensuring `_unifiedSource` is in Sources.

**Validation:** Unit tests for find-line size parse (pipe + spaces in path); `dotnet test` + agent gates.

---

## 2026-07-29 — Seamless hybrid ADB browse (scan + thumbs + import)

**Status:** Accepted (supersedes browse half of “Hybrid FTP browse / ADB pull” same day)
**Context:** OP13 FTP LIST aliases (.heif/.jpg phantoms), 550 RETR storms, Magick green/magenta glitches on capped RAW/video, Unified re-scanning `/DCIM` after FTP, PreferAdb already on for import.

**Decision:**
- Seamless hybrid (A): keep FTP sidebar/Unified UI; when PreferAdb + `TryResolve`, ADB owns **scan** (`find -type f`), **thumbnails** (dd → size-capped pull), and **import**; FTP fallback on failure.
- `ImportItem.SourcePath` stays FTP-style; map via `AdbAndroidPath`.
- HEIF→HEIC RETR/probe candidates with always-fall-back to original `.heif`.
- Permanent FTP 550 negative cache (host|port|path); clear on reconnect; FluentFTP thumb `RetryAttempts=0`.
- Partial RAW/video: never Magick/Shell on capped buffers; glitch JPEG reject in `ThumbnailPreviewValidator`.
- Unified/FTP share `_sourceItemsCache` + inflight scan map; narrow cache clears on scan path change.
- First `adb devices` serial only; multi-device picker still deferred.

**Alternatives rejected:** Dedicated sidebar ADB source; Magick on TieredFinalCap RAW/video; retrying 550×3 per worker.

**Validation:** Unit tests (path normalize, failure cache, glitch fixtures, Adb path rewrite); `dotnet test`.

---

## 2026-07-29 — Hybrid FTP browse / ADB pull for Android imports

**Status:** Accepted
**Context:** OP13 FTP session showed DHCP IP churn breaking host-keyed Credential Manager entries; large `/DCIM` libraries; ADB already available on the same device for faster copies.

**Decision:**
- Keep FTP for browse/scan/thumbnails; when `PreferAdbTransferWhenAvailable` (default true) and preflight finds a device + readable `/sdcard` or `/storage/emulated/0` remote folder, import Copy/Delete via remapped `AdbFileProvider`.
- No mid-import soft-fallback to FTP (avoids mixed transport / delete-after confusion).
- Multi-device: first `adb devices` serial only; show serial suffix in UI; picker deferred.
- Vault: `TryMigratePassword` from previous/saved hosts on IP change; fail-fast when password missing.
- FTP listing `KeepAlive=false`; ADB processes killed on cancel / 5-minute per-file wall timeout.
- Skip `.trashed-*`, `.nomedia`, and `.Trash`/`trash` directories at scan time.
- Fix ConfigLoad wiping FTP thumbnail limits; default limit on with count 48.

**Alternatives rejected:** First-class sidebar `AdbSourceItem` scan in this slice; mid-group FTP fallback; GUID vault keys.

**Validation:** Unit tests for migrate/path/trash; `watch-agent-gates` / `dotnet test`.

---

## 2026-07-22 — Automate Align-0.15 HUMAN_BACKLOG (keep release-please/pages off)

**Status:** Accepted
**Context:** HUMAN_BACKLOG deferred enabling several upstream workflows after template alignment.

**Decision:**
- Enable `stale.yml`, WPF-adapted `weekly-health-check.yml`, and `dependabot-automerge.yml`.
- Permanently decline `release-please*` and `pages.yml` (csproj + `build.yml` owns release).
- Add `scripts/automate-human-backlog.{sh,ps1}`, `scripts/lib/resolve-gh.sh`, and wire `AUTOMERGE_TOKEN` setup.
- Leave interactive `gh auth refresh -s security_events` as the only Align-0.15 human auth step.

**Validation:** `.\scripts\automate-human-backlog.ps1 -SetupAutomergeToken` (secret set); workflows present; `HUMAN_BACKLOG.md` regenerated.

---

## 2026-07-21 — Align process tooling with agent-project-bootstrap v0.15.1

**Status:** Accepted
**Context:** Repo was on template `0.11.0`; upstream tip is `0.15.1` (Cursor FOSS pack, parallel BUILD_PLAN, expanded scripts/CI). This is a live WPF product, not a fresh bootstrap.

**Decision:**
- Cherry-pick agent surface, Cursor FOSS pack, allowlisted scripts, and BUILD_PLAN markers to process level `0.15.1`.
- Preserve `modules/dotnet-wpf`, WPF file limits (800/400/200), and csproj-driven `build.yml` release.
- **Hard defer** new workflows: release-please, pages, dependabot-automerge, stale, weekly-health-check → `HUMAN_BACKLOG.md`.
- Merge (never overwrite) `validate-bootstrap.sh` / `TEMPLATE_INDEX.json`; bump `.template-version` only after local gates pass.
- Hooks go live only after dry-run of `validate-local.ps1 -QuickBootstrap` and `feature-gate.sh --stack dotnet-wpf`.

**Alternatives rejected:** Full template replace; enabling release-please; adopting upstream 300/150 file caps; copying inactive modules/examples.

**Validation:** `docs/BOOTSTRAP_ALIGNMENT.md`; encoding/hygiene/validate-bootstrap/batch-commands/validate-local; `dotnet test`.

**See:** `docs/BOOTSTRAP_ALIGNMENT.md`

---

## 2026-06-13 — SQLite provider: retain System.Data.SQLite.Core

**Decision:** Keep `System.Data.SQLite.Core` (1.0.119); do not migrate to `Microsoft.Data.Sqlite` in this release cycle.

**Rationale:** `QuickMediaIngest.csproj` documents that SQLite native interop relies on `Assembly.Location` for path resolution; single-file publish with `IncludeAllContentForSelfExtract` is validated against `System.Data.SQLite.Core`. `Microsoft.Data.Sqlite` uses a different native bundling model and would require publish-path regression testing.

**Alternatives rejected:** Immediate migration to `Microsoft.Data.Sqlite` without MSI/single-file validation.

**Validation:** `DatabaseServiceTests`, `dotnet test` (53 passed).

---

## 2026-06-13 — Dependabot PR #4 fully merged (MaterialDesign 5.x + Extensions 10.x)

**Decision:** Apply remaining Dependabot PR #4 bumps: MaterialDesignThemes 5.3.2, Microsoft.Extensions 10.0.9, System.Management 10.0.9, test SDK/xunit/Moq updates.

**Migration:** `Theme.Light`/`Theme.Dark` → `BaseTheme.Light`/`BaseTheme.Dark`; `MaterialDesignTheme.Defaults.xaml` → `MaterialDesign2.Defaults.xaml` (preserves MD2 control styles used by custom chrome).

**Validation:** `dotnet build`, `dotnet test` (53 passed), app startup smoke test.

---

## 2026-06-13 — Dependabot PR #4 partial merge

**Decision:** Close Dependabot PR #4; apply non-breaking bumps on main (FluentFTP 54.2.0, Meziantou 2.0.0, MetadataExtractor 2.9.3, SQLite 1.0.119). Defer MaterialDesignThemes 5.x and Microsoft.Extensions 10.x.

**Rationale:** Full PR failed CI (breaking API/theme changes). Safe subset clears dependency drift without MaterialDesign 5.x migration scope.

**Validation:** `dotnet restore`, `dotnet build`, `dotnet test`.

---

## 2026-06-13 — Persistence strategy B (JSON + VACUUM-only SQLite)

**Decision:** Keep JSON files for config, import history, and presets; slim `IDatabaseService` to `TryPeriodicVacuum()` only; remove unused SQLite CRUD APIs and dead DI (`IMetadataReader`, `IWhitelistFilter`).

**Rationale:** App already persists all user-facing state via JSON under `%AppData%\QuickMediaIngest\`. SQLite tables were unused split-brain. VACUUM retains occasional DB file maintenance without migration cost.

**Alternatives rejected:** (A) migrate config/history to SQLite — higher risk, no user benefit today.

**Validation:** `dotnet build`, `dotnet test`; `DatabaseService` no longer exposes CRUD.

---

## 2026-06-13 — Sprint 1 closed; file limits enforced

**Decision:** Close Sprint 1 file size remediation; archive to `COMPLETED_TASKS.md`; defer Sidebar/Import UserControl extraction (shell XAML under 800-line limit).

**Validation:** `scripts/check-file-limits.sh` (empty grandfather list), `dotnet test` (13 passed).

---

## 2026-06-13 — Sprint 0 closed; GitHub settings applied

**Decision:** Close Sprint 0 bootstrap parity; archive tasks to `COMPLETED_TASKS.md`; active work moves to Sprint 1 (file size remediation).

**GitHub settings applied via `gh`:** Dependabot security updates enabled, private vulnerability reporting enabled, branch protection on `main` (requires CI status checks), repo About description and topics updated.

**Pending:** Push bootstrap + Sprint 1 to `main`; confirm new `ci.yml` / `codeql.yml` / `security.yml` workflows green.

---

## 2026-06-13 — Magick.NET 14.14.0

**Decision:** Bump `Magick.NET-Q16-AnyCPU` from 14.13.0 to 14.14.0.

**Rationale:** Clears NU1902/NU1903 vulnerability advisories on restore/build without API changes; 13 tests pass.

**Validation:** `dotnet build`, `dotnet test` (Release).

---

**Decision:** Adopt [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) scaffolding without copying web/python/android Golden Path examples.

**Rationale:** QuickMediaIngest is a mature .NET 8 WPF app; Reference mode preserves existing architecture while adding agent routing, CI guardrails, and workspace memory.

**Alternatives rejected:** Full greenfield re-scaffold; copying unrelated stack examples.

**Validation:** `scripts/validate-bootstrap.sh`, `ci.yml` dotnet test job.

---

## 2026-06-13 — ADR-0001: MVVM + provider architecture (existing)

**Decision:** MVVM with CommunityToolkit.Mvvm partials; `IFileProvider` abstraction for Local/FTP/ADB sources; Core logic in `QuickMediaIngest/Core/` without WPF dependencies.

**Rationale:** Testability, extensibility for new source types, separation of UI from ingest pipeline.

**See also:** `docs/adr/0001-core-architecture.md`

---

## 2026-06-20 — Template Migration Sprint Phase 1 (bootstrap v0.11.0 alignment)

**Decision:** Migrate QuickMediaIngest from template **v0.2.0** to **v0.11.0** in Reference mode. Phase 1 establishes foundation only: `.cursorignore`, `init-project.sh` (with `--reference` + `dotnet-wpf` stack), `init-stack-sync.py`, `sync-stack-config.py`, and `.cursor/stack-selection.json`.

**Slash commands & rules strategy:**
- Additive migration — no legacy slash commands existed; copy all 25 `.cursor/commands/*.md` + `batch-commands.mdc` / `cursor-modes.mdc` in Phase 2.
- Preserve repo-specific rules: `wpf-mvvm.mdc`, `foss-compliance.mdc`, `read-before-write.mdc`.
- Do not blind-overwrite `INITIALIZATION_PROMPT.md`, `AGENT_MEMORY.md`, `modules/dotnet-wpf/MODULE.md`.

**WPF gate extension strategy (Phase 3):**
- Extend upstream `feature-gate.sh` with `dotnet-wpf` stack: `dotnet restore/build/test`, `dotnet format --verify-no-changes`, `check-file-limits.sh`, `check-license-compliance.sh`.
- Make `validate-bootstrap.sh` web-artifact requirements conditional on stack ≠ `dotnet-wpf`.

**Alternatives rejected:** Full greenfield re-scaffold; copying `examples/web` and unrelated Golden Path stacks.

**Validation:** `bash scripts/init-project.sh --stack dotnet-wpf --reference --no-prune --non-interactive`; `scripts/validate-bootstrap.sh` (Phase 5).

---

## 2026-06-20 — Critique mitigations (template migration)

**Decision:** Address all six BUILD_PLAN critique items before Phase 4 docs/index work.

**Changes:**
- `feature-gate.sh` — `dotnet-wpf` stack reads `stack-selection.json`; runs dotnet restore/build/test/format + license gate
- `watch-agent-gates.sh` / `feature-autofix.sh` — WPF scope paths; exit 2 = halt (3-strike)
- `validate-bootstrap.sh` — stack-conditional web artifacts; `.cursorignore` block check
- Slash commands `gates.md`, `ci.md`, `feature.md`, `prerelease.md` — WPF paths + `[HUMAN]` offline CI fallback
- `INITIALIZATION_PROMPT.md` — merged §6–8 (CURSOR_MODES, watch-agent-gates, 7a/7b) without overwriting §1 project dimensions
- `.cursorignore` — verified rules/commands not blocked

**Validation:** `dotnet test -c Release`; `check-batch-commands.sh` when bash available.

---

## 2026-06-20 — Phase 4: index, docs, template version bump

**Decision:** Complete template migration index and documentation sync; bump pinned template version from `0.2.0` to `0.11.0`.

**Changes:**
- `TEMPLATE_INDEX.json` — added batch-command rules, gate scripts, CURSOR_MODES/BATCH_COMMANDS docs, stack-selection, ephemeral templates; `template_version` → `0.11.0`
- `.template-version` → `0.11.0`
- Read-order updates: `AGENTS.md`, `docs/START_HERE.md`, `docs/FOR_AGENTS.md`, `PROMPT_LIBRARY.md`, `README.md` — CURSOR_MODES, slash commands, watch-agent-gates
- `AGENT_MEMORY.md` — milestone retrospective for v0.11.0 migration

**Validation:** `bash scripts/validate-template-index.sh`; Phase 5 gate suite pending.

---

## 2026-06-20 — Phase 5: gate suite & `/build` super-command smoke

**Decision:** Mark Phase 5 AGENT gate work complete; defer Dependabot strict check and HUMAN sign-off items.

**`/build` super-command smoke (this session):**

| Step | Command | Result |
|------|---------|--------|
| 1 | `plan.md` — Phase 5 validation scope | Pass (trivial rubric; no code edits) |
| 3 | `feature.md` — Phase 5 AGENT rows | Pass (gates + docs only) |
| 4 | `gates.md` — local validation | See gate table below |
**Gate suite results:**

| Script | Result | Notes |
|--------|--------|-------|
| `validate-bootstrap.sh` | ✅ Pass | Full run; stack=dotnet-wpf |
| `check-batch-commands.sh` | ✅ Pass | 25 files (20 atomic + 5 super) |
| `feature-gate.sh --stack dotnet-wpf` | ✅ Pass | 9 stages |
| `watch-agent-gates.sh --once --autofix` | ✅ Pass | 9 stages |
| `validate-local.ps1 -QuickBootstrap -SkipBuild` | ✅ Pass | All local gates |
| `pre-release-gate.sh` | ⚠️ Partial | feature-gate ✅; CI/CodeQL ✅; **Dependabot strict FAIL** — `gh` lacks `security_events` scope locally |
| `dotnet test -c Release` | ✅ Pass | 91 passed, 5 skipped (prior session) |
**Pending [HUMAN]:** Confirm `/bootstrap`, `/build`, `/verify`, `/ship`, `/gates`, `/audit` in Cursor `/` menu; sign off template v0.11.0 bump; run `gh auth refresh -s security_events` or manual CVE triage per `docs/SECURITY_TRIAGE.md`.

**Pending [AUTO]:** Push branch; CI green on all new gates.

---

## 2026-06-20 — Phase 5 HUMAN sign-off (template v0.11.0)

**Decision:** Human confirmed slash commands in Cursor `/` menu and approved template version bump to **0.11.0**.

**Sign-off:** `/bootstrap`, `/build`, `/verify`, `/ship`, `/gates`, `/audit` verified; `.template-version` and `TEMPLATE_INDEX.json` at `0.11.0` approved for release.

**Remaining:** `[AUTO]` push + CI green; optional `gh auth refresh -s security_events` for local Dependabot strict gate; FTP thumbnail HUMAN smoke on LAN test source.

---

## 2026-06-20 — Human verification automation

**Decision:** Automate BUILD_PLAN HUMAN verification rows where possible; keep UI-only checks as optional spot-checks.

**Changes:**
- `LanFtpSmokeProbe` — env-configurable LAN FTP probe (`QMI_SMOKE_FTP_*`, `QMI_SMOKE_REQUIRE=1`)
- `ConfigFilePersistenceTests`, `HumanVerificationSmokeTests` — config round-trip + FTP tier/cache/Ultra smokes
- Existing `FtpThumbnail*Tests` — removed hard `Skip`; auto-run when LAN FTP reachable
- `scripts/smoke-human-verification.ps1` / `.sh` — orchestrates smoke tests + optional security triage
- `validate-local.ps1 -SmokeHuman` — gate integration
- `.gitignore` — `TestResults/`

**Remaining [HUMAN]:** Delete-after-import dialog on restart (UI); thumbnail slider visual; libvips in published portable exe.

**Validation:** `dotnet test` 101 passed; `smoke-human-verification` filter 10/10 when LAN FTP up.

---

## 2026-06-21 — Release v1.3.17 (P1–P8 + human sign-off automation)

**Decision:** Ship v1.3.17 with backlog P2–P8 complete, automated human sign-offs (`run-human-signoffs`), and CI hardening fixes discovered during `/ship`.

**Changes:**
- Human sign-offs job: removed Dependabot API step (GITHUB_TOKEN unreliable on Windows runners; dotnet `--vulnerable` + Security Scan cover release gate)
- MSI validation: fixed `msiexec /a` argument quoting and Process-based libvips smoke exit code
- libvips publish smoke: removed `--no-build` to restore win-x64 RID assets

**Release:** [v1.3.17](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.17) — portable EXE, ZIP, MSI uploaded via `workflow_dispatch`.

**Validation:** CI + Security Scan + CodeQL green on `5aa8a14`; 109 tests; zero open Critical/High Dependabot alerts.

**Deferred:** CycloneDX SBOM attachment to releases (documented in INITIALIZATION_PROMPT, not yet in `build.yml`).

---

## 2026-06-21 — Release v1.3.18 (import progress + ETA)

**Decision:** Ship F-002 — byte-weighted import progress and ETA with parallel copies unchanged.

**Changes:**
- `ImportByteProgressTracker` + per-provider copy progress callbacks
- Shared `MainViewModel.ImportProgress.partial.cs` for bar, ETA, and status across import paths

**Release:** [v1.3.18](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.18) — portable EXE, ZIP, MSI via `workflow_dispatch`.

**Validation:** CI + Security Scan + CodeQL green on `0221d2c`; 113 tests; zero open Critical/High Dependabot alerts.

---

## 2026-07-12 — Audit Sprint R2 (security + docs hygiene)

**Decision:** Address High/Medium audit findings that are agent-fixable without a Core/WPF refactor; leave Dependabot merges and Scorecard triage to `[HUMAN]`.

**Changes:**
- Purge legacy `FtpPass` from `config.json` immediately after Credential Manager migrate (`LoadConfig` → `SaveConfig`)
- Crash dumps read `config.json` and redact `FtpPass`
- FTP path normalizers collapse `.` / `..` without climbing above root
- Document Win32 `CredentialPersistence.LocalMachine` as per-user persistent
- Docs: AGENT_MEMORY package versions, MODULE checklist, README version callouts, COMPLETED_TASKS DialogOverlays row

**Validation:** `watch-agent-gates` 9 stages OK; **127** Release tests; Dependabot/CodeQL open alerts = 0.

**Deferred (superseded):** See R2-D1 / R2-D2 / R2-D3 entries below — completed 2026-07-12.

---

## 2026-07-12 — Dependabot merges + Scorecard workflow fix

**Decision:** Squash-merge Dependabot PRs #10 (NuGet) and #7 (Actions). Fix Scorecard publish failure by moving write permissions to job scope (`permissions: read-all` at workflow level), matching [ossf/scorecard-action workflow restrictions](https://github.com/ossf/scorecard-action#workflow-restrictions). Also upload SARIF to code scanning.

**Root cause:** `publish_results: true` rejected the workflow because top-level `security-events: write` / `id-token: write` violate Scorecard API integrity checks.

**Validation:** Scorecard [run 29203183074](https://github.com/edwardlthompson/QuickMediaIngest/actions/runs/29203183074) success; `check-scorecard-sarif.sh` OK (no error/warning findings).

---

## 2026-07-12 — Release v1.3.19

**Decision:** Ship Audit R2 security fixes + backlog D1–D3 (DecodedThumbnail, LogPathSanitizer, Core coverage tests) as v1.3.19.

**Validation:** feature-gate 9 stages; 144 Release tests; CI/Security/CodeQL green on prior HEAD; Dependabot open Critical/High = 0 (API); Scorecard green after permission fix.

---

## 2026-07-12 — R2-D1: DecodedThumbnail replaces BitmapSource in Core

**Decision:** Core thumbnail public APIs and decode/cache paths use `DecodedThumbnail` (JPEG bytes + WxH). WPF shell/STA/BitmapSource orchestration lives under `QuickMediaIngest/Thumbnails/Wpf/`. ViewModels convert via `WpfThumbnailBridge.ToBitmapSource`.

**Rationale:** Keeps `Core/` free of WPF types (CODE_REVIEW F-002) while preserving shell/COM decode on an STA thread outside Core.

**Validation:** `dotnet test QuickMediaIngest-1.sln -c Release` — **144** passed; all `Core/**/*.cs` ≤ 200 lines; no `BitmapSource` / `System.Windows` usings under Core (doc cref only in `IShootFilterService`).

---

## 2026-07-12 — R2-D2/D3: log path sanitization + Core coverage tests

**Decision:** Add `LogPathSanitizer` for Information/Error logs (local / AppData / FTP). Expand unit tests for MetadataKeywordWriter, IngestItemProcessor, UpdateService (mock HttpClient), and the sanitizer.

**Validation:** **144** Release tests green.

---

## 2026-07-12 — AUTO-SBOM: CycloneDX on Build and Release

**Decision:** Generate CycloneDX JSON via Syft (`anchore/sbom-action@v0.24.0`) against `./publish/portable` after publish; attach to workflow artifacts and to GitHub Releases on `workflow_dispatch`. Local optional path: `scripts/generate-sbom.sh`.

**Rationale:** Closes regress gap from v1.3.19 (INITIALIZATION_PROMPT `[AUTO]` SBOM requirement).

---

## 2026-07-17 — Release v1.3.20 (SD/USB I/O stall)

**Decision:** Cap removable-drive preview/import parallelism (`RemovableDriveIo`), cancel preview CTS when import starts, run Shell/WPF thumbnail fallback on `StaRunner` (not UI dispatcher), and rethrow `OperationCanceledException` from `IngestItemProcessor`. Also fix Dependabot alert counting (`--paginate`, no `page=`) and normalize `scripts/*.sh` to LF.

**Rationale:** Concurrent sync preview workers + UI-marshaled Shell decode + parallel copies thrash SD/USB and freeze the UI until force-close.

**Validation:** `pre-release-gate.sh` passed; feature-gate 9 stages; **151** Release tests; zero open Critical/High Dependabot alerts.

---

## 2026-07-17 — Regress v1.3.20

**Checks:** Post-tag `pre-release-gate.sh` green on `ae84019`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.20.cyclonedx.json` on [v1.3.20](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.20).

**N/A:** GitHub Pages (WPF app); `simulate-template-upgrade.sh` not present in child repo.

**Follow-up [HUMAN]:** Confirm SD/USB preview+import on a real card with v1.3.20 portable build.

---

## 2026-07-21 — Release v1.3.21 (import Dispatcher freeze)

**Decision:** Replace sync `Dispatcher.Invoke` on import byte/`ItemProcessed` progress with `BeginInvoke` + coalesced byte snapshots.

**Rationale:** `ImportByteProgressTracker.ReportBytes` publishes every 1MB; sync UI marshaling blocked copy threads and froze the app mid-SD import (no log/dest growth; CPU still active). Observed after 42/132 files on a Canon exFAT card.

**Validation:** Local recovery copied remaining 90 with size checks; feature-gate green; **151** Release tests; `pre-release-gate.sh` green before ship.

---

## 2026-07-21 — Regress v1.3.21

**Checks:** Post-tag `pre-release-gate.sh` green on `ecb9d04`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.21.cyclonedx.json` on [v1.3.21](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.21).

**N/A:** GitHub Pages; template upgrade simulation script.

## 2026-07-28 — Release v1.3.22 (settings persistence)

**Decision:** Guard naming rebuild during `LoadConfig`; coerce mismatched presets to Custom; restore destination preset after combo refresh; query GitHub Actions per workflow name in `check-github-ci.sh`.

**Rationale:** Custom naming templates were overwritten when a stale Recommended preset re-applied on load/UI bind; destination combo Clear() nullled selection. CI poll listed Dependabot Updates first and never saw the CI run.

**Validation:** Feature-gate + pre-release-gate green; **154** Release tests; zero Critical/High Dependabot alerts.

## 2026-07-28 — Regress v1.3.22

**Checks:** Post-tag `pre-release-gate.sh` green on `efb067c`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.22.cyclonedx.json` on [v1.3.22](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.22).

**N/A:** GitHub Pages; release-please (declined for this product). `simulate-template-upgrade` web-stack smoke gaps are expected for this WPF-only child.
