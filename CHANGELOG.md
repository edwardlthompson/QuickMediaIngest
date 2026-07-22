# Changelog

## [Unreleased]

### Added

- **Template alignment v0.15.1**: Cursor FOSS pack (hooks, skills, agents), `/cleanup`, `HUMAN_BACKLOG.md`, parallel-dispatch helpers, `docs/BOOTSTRAP_ALIGNMENT.md`.
- **CI**: `stale.yml`, WPF-adapted `weekly-health-check.yml`, `dependabot-automerge.yml` (release-please/pages remain declined).
- **Scripts**: `automate-human-backlog.{ps1,sh}`, `setup-automerge-token`, `resolve-gh.sh`, Dependabot alerts GET probe fix.

### Changed

- **BUILD_PLAN**: Official emoji status markers (🔲/✅/❌) and Sequential / Parallel / Human lanes.
- **Agent docs**: START_HERE, AGENTS, FOR_AGENTS, batch commands registry synced to template process level 0.15.1 (WPF module and `build.yml` preserved).

## [1.3.21] — 2026-07-21

### Fixed

- **Import freeze mid-card**: Progress updates no longer use sync `Dispatcher.Invoke` on every 1MB copy buffer (and `ItemProcessed`). Copy threads post coalesced UI updates via `BeginInvoke`, so imports cannot deadlock the UI while still burning CPU with no dest growth.

## [1.3.20] — 2026-07-17

### Added

- **RemovableDriveIo**: Caps preview workers (≤2) and import copies (1) when the source path is on a removable drive.
- **Release SBOM**: CycloneDX JSON via Syft (`anchore/sbom-action`) on Build and Release; attached to CI artifacts and GitHub Release assets. Local helper: `scripts/generate-sbom.sh`.
- **Tests**: `RemovableDriveIoTests`; ingest cancel propagates `OperationCanceledException`.

### Fixed

- **SD card / USB stall**: High-parallelism preview decode no longer fights import I/O; import start cancels in-flight previews; Shell/WPF thumbnail fallback uses `StaRunner` instead of blocking the UI dispatcher; `IngestItemProcessor` rethrows cancel instead of treating it as a failed file.
- **Dependabot alert count**: `count-critical-high-dependabot.sh` no longer uses unsupported `page=` pagination (HTTP 400).
- **Shell scripts**: Normalized `scripts/*.sh` to LF to match `.gitattributes` (Windows bash `pipefail` breakage).

## [1.3.19] — 2026-07-12

### Added

- **DecodedThumbnail**: Core thumbnail pipeline uses JPEG payload + dimensions instead of WPF `BitmapSource`.
- **WpfThumbnailBridge**: UI-layer conversion from Core payloads to bindable bitmaps; WPF/STA/shell under `Thumbnails/Wpf/`.
- **LogPathSanitizer**: Redacts sensitive path prefixes in Information/Error logs (local, AppData, FTP).
- **Tests**: FtpPass purge on migrate, FTP path traversal collapse, LogPathSanitizer, MetadataKeywordWriter, IngestItemProcessor, UpdateService (mock HTTP).

### Changed

- **Dependabot**: Merged NuGet and GitHub Actions dependency bumps (#10, #7).
- **Scorecard**: Job-scoped write permissions for `publish_results`; SARIF upload to code scanning.
- **Docs**: AGENT_MEMORY package versions, MODULE checklist, README version callouts.

### Fixed

- **Legacy FTP passwords**: Purge plaintext `FtpPass` from `config.json` immediately after Credential Manager migration.
- **Crash dumps**: Read live `config.json` and redact `FtpPass`.
- **FTP paths**: Collapse `.` / `..` without climbing above the remote root.

---

## [1.3.18] — 2026-06-21

### Added

- **Import byte progress tracker**: `ImportByteProgressTracker` aggregates completed + in-flight bytes across parallel copies (up to 8).
- **Per-copy progress**: `IFileProvider.CopyAsync` optional `IProgress<long>` for Local, FTP, and ADB providers.
- **Tests**: `ImportByteProgressTrackerTests` and ingest incremental copy progress coverage.

### Changed

- **Import progress UI**: Progress bar and ETA/MB/s use byte-weighted math; status updates when each file starts copying.
- **BUILD_PLAN**: Slimmed active board; F-002 archived to `COMPLETED_TASKS.md`.

### Fixed

- **Import ETA accuracy**: No longer derived from file-count averages that jumped when parallel batches completed.

---

### Added

- **Human sign-off automation**: `run-human-signoffs.ps1/.sh` orchestrates config, UI binding, FTP, and security triage checks.
- **Headless UI verification**: `HumanSignoffVerificationTests` for Delete After Import and ThumbnailSize bindings after config reload.
- **Published exe smoke**: `--smoke-libvips` flag and `smoke-published-exe` scripts for portable build validation.
- **CI**: Headless libvips smoke, automated human-signoffs job, OpenSSF Scorecard workflow, MSI admin-extract validation.
- **Gate scripts**: `check-readme-health.sh`, `ensure-gh-security-scope.sh`, `check-scorecard-sarif.sh`, `validate-msi-install.ps1`.

### Changed

- **File limits**: Cleared grandfather list; split ViewModel and Core/Ftp partials under line budgets.
- **MainWindow**: Removed dead overlay code-behind handlers; overlays bind VM commands.
- **BUILD_PLAN**: Slimmed active board; completed work archived to `COMPLETED_TASKS.md`.
- **WpfTestFixture**: Dedicated STA thread for reliable headless WPF tests (109 tests).

### Fixed

- **NetVips native DLL**: `NetVips.Native.win-x64` in publish path for single-file portable exe.

---

## [1.3.16] — 2026-06-13

### Fixed

- **FTP thumbnails stuck pending**: Purging corrupt in-memory cache no longer skips FTP re-fetch; items are correctly re-queued.
- **Preview health during load**: Health summary updates incrementally as each thumbnail completes.
- **Content validator**: Relaxed variance threshold so legitimate dark/uniform photos are not rejected.

---

## [1.3.15] — 2026-06-13

### Fixed

- **FTP thumbnail green/black corrupt previews**: Content validator rejects solid-green, solid-black, and flat-color decode garbage; stale corrupt FTP disk cache entries are purged on load.
- **FTP cache version bump** (`ftp-thumb-v2`): Forces rebuild of previews poisoned by v1.3.13/v1.3.14 bad decodes.
- **RAW/DNG FTP previews**: When HEIC/JPG sibling loads, skipped DNG items now inherit the rendered thumbnail.

---

## [1.3.14] — 2026-06-13

### Fixed

- **FTP thumbnail green/corrupt previews**: Tiered decode no longer accepts libvips output on partial HEIC buffers; early tiers use embedded-preview-only; Magick only at final cap tier.
- **FTP thumbnail missing previews**: Reject undersized/corrupt decode results and escalate tiers; cap parallel full-file fallbacks to 2; log background batch failures.
- **HEIC embedded reader**: Require ≥2 KB JPEG segment and validate decoded dimensions before accepting.

---

## [1.3.13] — 2026-06-13

### Added

- **Tiered FTP preview download**: Escalates 64 KB → 256 KB → 512 KB → type cap only when decode fails; HEIC cap lowered to 2 MB.
- **HEIC embedded preview reader**: Scans partial downloads for JPEG segments before full Magick decode.
- **FluentFTP streaming pool** (Max/Ultra): Reused connections for capped preview downloads with FtpWebRequest fallback.
- **libvips decode path** (NetVips): Shrink-on-load thumbnails when native libs are available; Magick remains fallback.
- **Viewport-priority FTP thumbnails**: Expanded / top shoot groups load first.
- **FTP disk cache pre-check** in ViewModel before scheduling network work.

### Fixed

- **Thumbnail zoom persistence**: Ctrl+wheel and slider both use 50–300 range; `ThumbnailSize` clamped on save.

### Changed

- **FTP thumbnail pipeline**: Separate download parallelism (capped at 6) and decode parallelism (CPU-scaled via `FtpThumbnailLoadOptions`).

---

## [1.3.12] — 2026-06-13

### Fixed

- **Delete After Import persistence**: Safety confirmation runs only on user-initiated toggles, not when the setting is restored from `config.json` on startup. `DeleteAfterImportPromptDismissed` is set only after the user confirms.

### Added

- **Delete After Import in Preferences**: Same setting available under Import Settings alongside other persistent import options.
- **FTP thumbnail disk cache**: Decoded FTP previews are cached by `host|port|remotePath|fileSize` for fast reload on reconnect.
- **Thumbnail Performance hint**: Preferences explains Ultra vs Low for fast PCs vs phone FTP servers.

### Changed

- **FTP thumbnail parallelism**: `GetFtpThumbnailWorkerCount()` scales with CPU count and Thumbnail Performance mode (Ultra up to 16 workers on powerful PCs).
- **RAW preview dedup**: When RAW+rendered grouping is on, standalone DNG FTP downloads are skipped when a same-stem HEIC/JPG is in the batch.

---

## [1.3.5] — 2026-06-13

### Changed

- **Dependabot — full dependency refresh**: MaterialDesignThemes 5.3.2, Microsoft.Extensions 10.0.9, System.Management 10.0.9, test SDK/xunit/Moq updates. Clears all outstanding Dependabot PR #4 deferred bumps.

### Fixed

- **MaterialDesign 5.x migration**: Runtime theme switching uses `BaseTheme.Light`/`BaseTheme.Dark`; defaults dictionary switched to `MaterialDesign2.Defaults.xaml` to preserve existing MD2 control styles.

---

## [1.3.4] — 2026-06-13

### Fixed

- **Startup crash**: Overlay blur bindings no longer combine `ElementName` and `RelativeSource` (invalid in WPF); backdrop uses `MainWindow.BlurBackdropSource` instead.
- **Missing overlay resources**: `BooleanToVisibilityConverter`, `BoolToInvertedBoolConverter`, and `FtpInputTextBoxStyle` moved to application resources so extracted overlay UserControls resolve them at load time.
- **Release workflow**: Tag creation is idempotent when re-running Build and Release for an existing version.

---

## [1.3.3] — 2026-06-13

### Fixed

- **Overlay MVVM**: Preferences, scan exclusions, import history, and dialog overlays bind to ViewModel commands instead of forwarding to `MainWindow` code-behind.
- **FTP password binding**: `PasswordBoxAssist` keeps `FtpPass` in sync without MainWindow handlers.

### Changed

- **Core architecture (Sprint 3)**: Split oversized Core files — `FtpScanner`, `IngestEngine`, `ThumbnailService`, `ServiceContracts` — into focused modules (`FtpDirectoryClient`, `IngestItemProcessor`, `ThumbnailDiskCache`, factories, etc.). All `Core/**/*.cs` now ≤ 200 lines; grandfather list cleared.
- **MainViewModel partials**: Renamed `Part9–Part17` to semantic names (`Thumbnails`, `ImportEngine`, `DriveScan`, `FtpWorkflow`, `SourceLoad`, etc.).
- **ShootFilterService**: Decoupled from Localization; filter type IDs live in `Core/Models/FilterFileTypeIds`.
- **FTP workflow strings**: Status messages moved to `Strings.resx` (`Vm_Ftp_*` keys).
- **Dependencies**: FluentFTP 54.2.0, Meziantou.CredentialManager 2.0.0, MetadataExtractor 2.9.3, SQLite 1.0.119 (MaterialDesign 5.x and Microsoft.Extensions 10.x deferred).

### Added

- **`IFileDialogService` / `IShellService`**: WPF implementations for folder browse, logs folder, and URL launch.
- **ADB provider tests**: `AdbDeviceProbe` + live device smoke test (`AdbFileProviderTests`).
- **Test expansion (53 total)**: `FtpListingParser`, `IngestFileNaming`, `IngestVerification`, `LocalScanner`, `MediaExtensions`, `DatabaseService` injectable-path tests.
- **Accessibility**: `AutomationProperties.Name` on collapsed sidebar icon buttons.

---

## [1.3.2] — 2026-06-13

### Fixed

- **Preferences Save & Close**: The button now calls `SaveConfig()` before closing; settings changes persist to `config.json` instead of being discarded.
- **Folder naming parity**: Import and post-import album/XMP export both use the shared `GroupFolderNaming` format (`yyyyMMdd_HHmmss_Title`), so destination folders match across workflows.
- **CodeQL workflow**: Initialize CodeQL before `dotnet build` so static analysis runs correctly.

### Changed

- **WPF structure**: `MainViewModel` and `MainWindow` split into domain partials; overlay dialogs moved to `Controls/*` UserControls; converters extracted to `Converters/`.
- **Persistence**: User config and history remain JSON under `%AppData%\QuickMediaIngest\`; SQLite is VACUUM-only maintenance (unused CRUD APIs removed).
- **CI / release**: `ci.yml` adds vulnerable-package scan, license check, and `dotnet format`; `build.yml` runs tests before publish and creates GitHub Releases only via **workflow_dispatch**.
- **Dependencies**: Magick.NET `14.14.0` (clears NU190 advisories); test packages bumped; NuGet lock files enabled.

### Added

- Agent/bootstrap scaffolding (`AGENTS.md`, `docs/FOR_AGENTS.md`, CI workflows, pre-commit hooks).
- `scripts/validate-local.ps1` and `docs/DEV_SETUP_WINDOWS.md` for Windows dev validation.
- Unit tests: folder naming parity, keyword parser, settings save-and-close (20 tests total).
- Spanish/French translation for notifications accessibility label.

### Removed

- Dead DI registrations (`IMetadataReader`, `IWhitelistFilter`) and unused settings code-behind handlers.

---

## [1.3.1] — 2026-05-01

### Changed

- **Preferences**: **Language** is now only display language and restart hint. All import-related options (keywords, confirmations, import behavior, thumbnails/preview performance, RAW stacking, duplicate and verification modes, presets) sit under one **Import Settings** expander. Collapsed/expanded state is stored as **`SettingsPrefsImportSettingsExpanded`** in **`config.json`**, with migration from the former **Import behavior** and **Advanced** expander flags.

---

## [1.3.0] — 2026-04-30

### Fixed

- **Delete after import (FTP)**: Post-import verification used **`FileInfo`** / **`SHA-256`** on **`SourcePath`**, which for FTP is a **server path**, not a local file—verification always failed and **remote files were never deleted**. FTP sources now verify using **listing size** (`ImportItem.FileSize`) vs the **downloaded file** size before calling **`DeleteAsync`**. Strict mode documents **size-only** verification for FTP (cannot hash server bytes via **`System.IO`**).
- **Duplicate policy `OverwriteIfNewer` (FTP)**: Compare **scan metadata time** (`DateTaken`) to the existing destination file instead of **`FileInfo`** on the FTP path.

---

## [1.2.2] — 2026-04-29

### Fixed

- **Single-file publish**: Set **IncludeAllContentForSelfExtract** so bundled assemblies extract at startup and **System.Data.SQLite** can resolve native interop paths (fixes **`ArgumentNullException`** in **`SQLiteConnection`** during startup—the splash hang / error dialog some users saw with portable **`QuickMediaIngest.exe`**).

### Changed

- **Preferences**: Expanded/collapsed state for each modal section (**Save destination**, **File naming**, **Language & prompts**, **Import behavior**, **Advanced**) is saved to **`config.json`** and restored on launch.
- **Sidebar**: **Collapse toggle** state and **Notifications** expander state are persisted and restored (**`SidebarCollapsed`**, **`SidebarNotificationsExpanded`** in **`config.json`**).

---

## [1.2.1] — 2026-04-28

### Added

- **Update discovery**: When GitHub reports a **newer release**, the app can show a **desktop popup** (in addition to status text). Each **release tag** is remembered so the same build is not announced repeatedly across background checks (`LastNotifiedUpdateTag` in `config.json`).
- **Update API**: `UpdateCheckResult` returns **download URL** and **remote tag** from the latest release; startup runs an update check according to **About → interval** (still gated by `%AppData%\QuickMediaIngest\last_update_check.txt`).
- **Shortcuts**: **F1** opens an in-app **keyboard shortcuts** reference window.
- **Architecture**: **`AppConfig`** for persisted settings; **`MainViewModel`** split into partial files (`Config`, tokens); additional **`Core/Services`** types and unit tests (`FtpWorkflowService`, `ShootFilterService`, `UnifiedConcreteSourceScanService`).

### Changed

- **Notifications sidebar**: Removed redundant **Check for updates** (About & Updates remains the place to check manually); removed **Mute** on the notification feed; **Delete after import** status line moved to the **top** of the readiness summary so it reads naturally before a scan.
- **Import history**: Shown as an **in-app overlay** aligned with other modals (standalone `ImportHistoryWindow` removed).
- **Theming**: Legacy `DarkTheme` / `LightTheme` / `ControlStyles` resource dictionaries removed; styling consolidated with **MaterialDesign BundledTheme**, **`Themes/Brushes.xaml`**, and inline **`App.xaml`** resources.
- **Localization**: Additional strings in **English**, **Spanish**, and **French**.
- **SQLite**: **ImportHistory** table and index created when initializing the database (supports per-file history features).

---

## [1.2.0] — 2026-04-27

### Added

- **Localization**: Resource-based UI strings with **English** defaults and satellite resources for **Spanish** and **French**; language selector in Preferences (`Strings.resx`, `Strings.es.resx`, `Strings.fr.resx`).
- **Scan exclusions UX**: Dedicated overlay for excluding drives / blacklisting folders, aligned with Preferences modal chrome.
- **Skipped-folder summary**: Distinguished **user exclusion rules** vs **FTP listing failures** in titles, body copy, and status messages; optional **“do not remind when skips are only from exclusions”** (persisted in `config.json` as `SuppressExcludedFolderScanReminders`).
- **Skipped-folder dialog**: Completes the panel with title, scrollable body, Copy, Close, and optional suppression checkbox.

### Changed

- **Unified source refresh**: Scan progress overlay dismisses immediately after merge / group rebuild; thumbnail loading no longer keeps the modal open (fixes perceived hang and stuck blur during long preview phases).
- **Modal backdrops**: Preferences and Scan exclusions overlays moved to **root-level** siblings (with other modals) so **VisualBrush blur + dim** match FTP / drive / About / progress dialogs; `Stretch="Fill"` and margins reduce edge artifacts.
- **Escape / Cancel**: Single command dismisses topmost overlay in a defined priority order (includes scan progress, dialogs, preferences, exclusions).
- **Sidebar**: Theme control relocated **under the collapse toggle** in the header; **collapsed sidebar** hides the labeled theme row and uses **icon-only** theme on the collapsed rail (no duplicate wide button).
- **Theme**: Continued alignment of custom `Theme.*` brushes with Material Design surfaces for light/dark switching.

### Fixed

- **Unified / scan UX**: Blurred overlay could remain until Escape when preview work extended past merge; overlay lifecycle tied to scan/merge completion instead.
- **Messaging**: Misleading “FTP listing error” wording when folders were skipped only because of **Scan exclusions**.

---

## [1.1.10] and earlier

See git history and tags prior to `v1.2.0` for incremental changes. The pre-1.2.0 working notes below were folded into **1.2.0** where shipped:

<details>
<summary>Historical “Unreleased” draft (superseded by 1.2.0)</summary>

- Import History: confirmation before clearing history; improved CSV export (headers, escaping, UTF-8).
- Preview: RAW shell preview aspect ratio; video thumbnails in local, FTP, and unified paths.
- Import workflow: preflight, queue, retry failed, resume pending plan.
- Ingest: duplicate policies (`Suffix`, `Skip`, `OverwriteIfNewer`); verification modes (`Fast`, `Strict`).
- Naming: EXIF subsecond / millisecond robustness.
- UX: stack compare toggle, presets, top-bar filters.
- Updates: external updater handoff for MSI/EXE after app exit.
- About: build date, changelog/release links.
- FTP video previews: timeouts and shell fallback.
- Build metadata: `BuildDate` resolution for single-file and deployed layouts.

</details>

## Unreleased

_(Nothing staged.)_

