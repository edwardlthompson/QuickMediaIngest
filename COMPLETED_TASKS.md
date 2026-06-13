# Completed Tasks

> Archive of finished BUILD_PLAN items.

---

## Sprint 0 — Bootstrap Parity (2026-06-13)

### Sequential

- [x] [HUMAN] Click **Use this template** *(N/A — existing repo, Reference mode)*
- [x] [AGENT] Fill placeholders in `docs/INITIALIZATION_PROMPT.md`
- [x] [AGENT] Add agent scaffolding: `AGENTS.md`, `docs/START_HERE.md`, memory files, `.cursor/rules/`
- [x] [HUMAN] Enable Dependabot alerts + security updates
- [x] [HUMAN] Enable private vulnerability reporting + branch protection on `main`
- [x] [AGENT] Create `SECURITY.md`, `CODE_OF_CONDUCT.md`, `docs/THREAT_MODEL.md`, `docs/PRIVACY.md`, `docs/RUNBOOK.md`
- [x] [AGENT] Add `.github/CODEOWNERS` and `THIRD_PARTY_LICENSES.md`
- [x] [AGENT] Initialize workspace memory files
- [x] [AGENT] Wire update checker config (weekly)
- [x] [HUMAN] Set GitHub repo About description from `docs/GITHUB_ABOUT.md`
- [x] [AGENT] Add `.env.example`
- [x] [AGENT] Ensure `TEMPLATE_INDEX.json` complete
- [x] [AGENT] Add `modules/dotnet-wpf/MODULE.md`
- [x] [AGENT] Wire `QuickMediaIngest.Tests` into solution; add `.editorconfig`
- [x] [AGENT] Add CI workflows (`ci.yml`, `codeql.yml`, `security.yml`, `dependency-review.yml`, `dependabot.yml`)
- [x] [AUTO] `scripts/check-file-encoding.sh` passes *(verified locally; CI after push)*
- [x] [AUTO] `scripts/validate-bootstrap.sh` passes *(verified locally; CI after push)*
- [x] [AUTO] `dotnet test` passes *(13 tests, Release)*
- [x] [HUMAN] Approve Sprint 0 *(local gates pass; bootstrap CI workflows pending first push to `main`)*

### Parallel

- [x] [AGENT] Update README bootstrap sections
- [x] [HUMAN] Configure `.template-update.json` interval (weekly)
- [x] [AUTO] Pre-commit config present (`.pre-commit-config.yaml`)

---

## Sprint 1 — File Size Remediation (2026-06-13)

**Goal:** Split oversized WPF files to meet adapted line limits; remove grandfather entries from `scripts/check-file-limits.sh`.

**Adapted limits:** `.xaml` 800, ViewModels/`*.xaml.cs` 400, `Core/` 200 lines.

### Sequential

- [x] [AGENT] Split `MainWindow.xaml` into overlay UserControls (`DialogOverlays`, `ImportHistoryOverlay`, `PreferencesOverlay`, `ScanExclusionsOverlay`); sidebar/import remain in shell (755 lines, under 800)
- [x] [AGENT] Split `MainWindow.xaml.cs` into partials (`Chrome`, `Ribbon`, `Settings`); moved converters to `Converters/`
- [x] [AGENT] Split `MainViewModel.cs` into 18 partial files + `SupportTypes.cs`; retained `Config` and `Tokens` partials
- [x] [AGENT] Split `App.xaml.cs` → `App.Theme.partial.cs` (under 400-line code-behind limit)
- [x] [AUTO] `scripts/check-file-limits.sh` passes with empty grandfather list *(verified locally)*
- [x] [AUTO] `dotnet test` passes *(13 tests, Release)*
- [x] [HUMAN] Approve Sprint 1 *(local gates pass; confirm CI green after push)*

### Parallel

- [x] [AGENT] Extract Preferences overlay UserControl — `QuickMediaIngest/Controls/PreferencesOverlay/`
- [x] [AGENT] `MainViewModel.Scan.partial.cs`, `MainViewModel.Import.partial.cs` (and related partials)
- [-] [AGENT] Extract Sidebar UserControl — deferred (not required; `MainWindow.xaml` under limit)
- [-] [AGENT] Extract Import panel UserControl — deferred (not required; `MainWindow.xaml` under limit)

### Tooling added

- `tools/split_mainviewmodel.py`, `tools/split_mainwindow.py`, `tools/split_mainwindow_cs.py`

---

## Sprint 2 — Deferred Features (partial, 2026-06-13)

- [x] [AGENT] SQLite index on `DeviceId` + `Path` in `DatabaseService` *(already present: `idx_whitelist_device_path`, `idx_importhistory_device_path`)*
- [x] [AGENT] Bump Magick.NET `14.13.0` → `14.14.0` *(clears NU1902/NU1903 advisories; build + 13 tests pass)*

---

## Sprint 2 — Release Readiness & Critical Fixes (2026-06-13)

### Sequential (agent)

- [x] [AGENT] Fix Settings **Save & Close** — `SaveAndCloseSettingsCommand` on VM; wired `PreferencesOverlayView` button; removed dead `Settings_Save` handler
- [x] [AGENT] Unify folder naming — `Core/GroupFolderNaming.cs` shared by `GroupBuilder` and `IngestEngine`
- [x] [AGENT] Fix CodeQL workflow — `github/codeql-action/init` before `dotnet build`
- [x] [AGENT] Gate `build.yml` on `dotnet test`; release/tag/upload only on `workflow_dispatch`

### Parallel (agent)

- [x] [AGENT] `ci.yml`: vulnerable packages, license compliance, `dotnet format --verify-no-changes`
- [x] [AGENT] `FolderNamingTests.cs` — parity between `GroupBuilder` and `GroupFolderNaming`
- [x] [AGENT] `MainViewModelProofTests.SaveAndCloseSettings_ClosesDialog` + `RUNBOOK.md` QA checklist
- [x] [AGENT] `KeywordInputParserTests.cs` (4 cases)

### Sprint 3 partial (agent, persistence B)

- [x] [AGENT] Persistence strategy B — JSON config + VACUUM-only SQLite (`DECISION_LOG.md`)
- [x] [AGENT] Slim `IDatabaseService` / `DatabaseService` to `TryPeriodicVacuum()` only
- [x] [AGENT] Remove `IMetadataReader`, `IWhitelistFilter` from DI; delete `WhitelistFilterTests`

### Sprint 4 partial (agent)

- [x] [AGENT] `SelectAllCheckBox` → `SelectAllShootsCommand` / `DeselectAllShootsCommand`
- [x] [AGENT] Delete dead `Token_*`, `Settings_MoveToken*`, `Settings_Save`/`Settings_Close` handlers
- [x] [AGENT] Translate `A11y_NotificationsIcon` in `Strings.es.resx`, `Strings.fr.resx`

### Sprint 6 (agent)

- [x] [AGENT] `scripts/validate-local.ps1`
- [x] [AGENT] `RestorePackagesWithLockFile` + `packages.lock.json` (main + test projects)
- [x] [AGENT] Expand `docs/FOR_AGENTS.md`; add `docs/DEV_SETUP_WINDOWS.md`
- [x] [AGENT] Pre-commit: `check-file-limits.sh`, `check-license-compliance.sh`
- [x] [AGENT] Bump test packages: xunit 2.9.2, Test.Sdk 17.11.1
- [x] [AUTO] `dotnet format` applied solution-wide; verify passes locally

**Validation:** `dotnet build` + `dotnet test` (20 passed, Release); `dotnet format --verify-no-changes` clean.

**Pending human:** Push to `main`; confirm remote CI green; release sign-off.

---

## Post-Push Checklist (Sprint 0 + Sprint 1 closure)

- [x] [AUTO] Local bootstrap artifact check *(all required files present)*
- [x] [AUTO] Local file line limits check *(all OK)*
- [x] [AUTO] Local `dotnet test` *(20 passed)*
- [ ] [AUTO] Confirm `ci.yml` all jobs green on `main` *(pending push of bootstrap + Sprint 1 changes)*
- [ ] [AUTO] Confirm `codeql.yml` and `security.yml` green *(pending push)*
- [ ] [HUMAN] Mark Sprint 0/1 CI approval after green run on `main`

---

## Pre-Bootstrap Milestones

---

## Milestone 1 – Bug Fixes & Foundation

### Sprint 1.1: Core Bug Fixes & Licensing

- [x] Fix `ItemProcessed` event signature in `IngestEngine.cs`
- [x] Add MIT license file (`LICENSE`) and credit third-party assets

### Sprint 1.2: Documentation & Nullability

- [x] Add basic XML documentation comments to public APIs in Core & Providers
- [x] Enable nullable reference types project-wide (`#nullable enable`)

### Sprint 1.3: Logging & Test Scaffolding

- [x] Add initial unit tests (xUnit + Moq) for:
  - `IngestEngine.ResolveFileName`
  - `GroupBuilder` logic
  - Whitelist filter matching

---

## Milestone 2 – Architecture & Developer Experience

### Sprint 2.1: Dependency Injection

- [x] Introduce Microsoft.Extensions.DependencyInjection
- [x] Register all services/providers in `App.xaml.cs`
- [x] Inject into `MainViewModel`, `IngestEngine`, providers, etc.

### Sprint 2.2: MVVM Toolkit Migration

- [x] Migrate `MainViewModel` to CommunityToolkit.Mvvm
- [x] Use `[ObservableProperty]`, `[RelayCommand]`
- [x] Remove manual `INotifyPropertyChanged` + `ICommand` boilerplate

### Sprint 2.3: Model & Logging Improvements

- [x] Convert suitable models to `record` types
- [x] Add structured logging levels throughout ingestion flow

---

## Milestone 3 – Performance & Speed Optimizations

### Sprint 3.1: Parallelization & Caching

- [x] Parallelize file copy loop in `IngestEngine.IngestGroupAsync`
- [x] Implement thumbnail disk cache in `%AppData%\QuickMediaIngest\Thumbnails\`

### Sprint 3.2: FTP & Indexing Optimizations

- [x] Optimize FTP provider (binary mode, FluentFTP, batch listings)

---

## Milestone 4 – UX/UI Polish & Usability Wins

### Sprint 4.1: Settings & Preview

- [x] Naming template live preview
- [x] Whitelist rule manager (add/edit/delete)
- [x] Toggle move/copy, post-import delete option

### Sprint 4.2: Progress & Grid Enhancements

- [x] Dedicated Ingest Log panel
- [x] Overall + per-group progress bars
- [x] Working cancel button (propagate CancellationToken)
- [x] Hover zoom / right-click full preview
- [x] Better multi-select (Shift/Ctrl)
- [x] Ensure virtualization works with large sets
- [x] Add first-run onboarding tooltips / tour

---

## Milestone 5 – High-Value Features

### Sprint 5.1: Post-Import & Filtering

- [x] Quick dark/light mode toggle in title bar
- [x] Post-import actions (open folder, eject, sidecar export)
- [x] Advanced grid filtering / search (date range, file type, keyword)

### Sprint 5.2: Duplicates & Stretch Goals

- [x] Duplicate detection across sources
- [x] ADB fallback for faster Android transfers
- [x] Export/import app settings (JSON)

---

## Nice-to-Have / Future Ideas

- [x] Better error recovery (retry failed files, skip vs abort)
