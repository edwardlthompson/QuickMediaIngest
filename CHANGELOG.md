# Changelog

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

