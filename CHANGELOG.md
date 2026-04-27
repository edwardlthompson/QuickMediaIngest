# Changelog

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

_(Nothing staged yet.)_

