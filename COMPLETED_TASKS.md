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
- [x] [AUTO] `dotnet test` passes *(20 tests, Release; CI green on `main` as of v1.3.2)*
- [x] [HUMAN] Approve Sprint 0 *(CI confirmed green after push)*

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
- [x] [HUMAN] Approve Sprint 1 *(CI confirmed green after push)*

### Parallel

- [x] [AGENT] Extract Preferences overlay UserControl — `QuickMediaIngest/Controls/PreferencesOverlay/`
- [x] [AGENT] `MainViewModel.Scan.partial.cs`, `MainViewModel.Import.partial.cs` (and related partials)
- [-] [AGENT] Extract Sidebar UserControl — deferred (not required; `MainWindow.xaml` under limit)
- [-] [AGENT] Extract Import panel UserControl — deferred (not required; `MainWindow.xaml` under limit)

### Tooling added

- `tools/split_mainviewmodel.py`, `tools/split_mainwindow.py`, `tools/split_mainwindow_cs.py`

---

## Sprint 2 — Release Readiness & v1.3.2 (2026-06-13)

**Release:** [v1.3.2](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.2)

### Agent work

- [x] [AGENT] Fix Settings **Save & Close** — `SaveAndCloseSettingsCommand`; wired `PreferencesOverlayView`
- [x] [AGENT] Unify folder naming — `Core/GroupFolderNaming.cs` shared by `GroupBuilder` and `IngestEngine`
- [x] [AGENT] Fix CodeQL workflow — `init` before `dotnet build`
- [x] [AGENT] Gate `build.yml` on `dotnet test`; release/tag/upload only on `workflow_dispatch`
- [x] [AGENT] `ci.yml`: vulnerable packages, license compliance, `dotnet format --verify-no-changes`
- [x] [AGENT] `FolderNamingTests.cs`, `KeywordInputParserTests.cs`, settings save-and-close test + `RUNBOOK.md` QA
- [x] [AGENT] Bump Magick.NET `14.13.0` → `14.14.0` *(NU190 advisories cleared)*
- [x] [AGENT] SQLite index on `DeviceId` + `Path` *(already in `DatabaseService`)*

### Human / release closure

- [x] [HUMAN] Push to `main` *(commit `c805aac`)*
- [x] [AUTO] `ci.yml`, `codeql.yml`, `security.yml` green on `main`
- [x] [HUMAN] Sprint 0/1 CI sign-off
- [x] [HUMAN] Release v1.3.2 — `CHANGELOG.md`, GitHub Release with portable EXE, zip, MSI

### Post-push CI fixes

- [x] [AGENT] Grandfather Sprint 3 Core files in `check-file-limits.sh` pending splits
- [x] [AGENT] Pin .NET SDK `8.0.422` via `global.json`
- [x] [AGENT] Pin `trivy-action` to v0.35.0 SHA (0 open Dependabot alerts)

**Commits:** `c805aac`, `8c5666a`, `f4d5a5a`, `9c01f23`

---

## Sprint 3 — Persistence & Dead DI (2026-06-13)

- [x] [HUMAN→AGENT] Persistence strategy **B** — JSON config + VACUUM-only SQLite (`DECISION_LOG.md`)
- [x] [AGENT] Slim `IDatabaseService` / `DatabaseService` to `TryPeriodicVacuum()` only
- [x] [AGENT] Remove `IMetadataReader`, `IWhitelistFilter` from DI; delete `WhitelistFilterTests`

---

## Sprint 4 — Quick Wins (2026-06-13)

- [x] [AGENT] Delete dead code-behind: `Token_*`, `Settings_MoveToken*`, `Settings_Save`/`Settings_Close`
- [x] [AGENT] `SelectAllCheckBox` → `SelectAllShootsCommand` / `DeselectAllShootsCommand`
- [x] [AGENT] Translate `A11y_NotificationsIcon` in `Strings.es.resx`, `Strings.fr.resx`

---

## Sprint 5 — Tests (partial, 2026-06-13)

- [x] [AGENT] `FolderNamingTests.cs` — import/export folder naming parity
- [x] [AGENT] `KeywordInputParserTests.cs` (4 cases)

---

## Sprint 6 — Infrastructure (2026-06-13)

- [x] [AGENT] `scripts/validate-local.ps1`
- [x] [AGENT] `RestorePackagesWithLockFile` + `packages.lock.json`
- [x] [AGENT] Expand `docs/FOR_AGENTS.md`; add `docs/DEV_SETUP_WINDOWS.md`
- [x] [AGENT] Pre-commit: `check-file-limits.sh`, `check-license-compliance.sh`
- [x] [AGENT] Bump test packages: xunit 2.9.2, Test.Sdk 17.11.1
- [x] [AGENT] `Microsoft.Extensions.*` already at 8.0.1 (latest 8.0.x patch)

---

## Milestone Gates — Closed (through v1.3.2)

| Gate | Sprint | Closed |
|------|--------|--------|
| Regression tests: zero failures (20 tests) | 2 | 2026-06-13 |
| Settings Save & Close persists config | 2 | 2026-06-13 |
| Import folder names = export folder names | 2 | 2026-06-13 |
| No dead DI registrations | 3 | 2026-06-13 |
| CodeQL init before build | 2 | 2026-06-13 |
| Release gated on tests | 2 | 2026-06-13 |
| `dotnet format` enforced in CI | 2 | 2026-06-13 |
| NuGet audit clean in CI | 2 | 2026-06-13 |
| Static analysis and vulnerability scans clean | 2 | 2026-06-13 |
| `CHANGELOG.md` updated | 2 | 2026-06-13 |
| Version bumped + GitHub Release (v1.3.2) | 2 | 2026-06-13 |
| Windows local validation script | 6 | 2026-06-13 |
| ViewModel/`*.xaml.cs` line limits | 1 | 2026-06-13 |
| UTF-8 encoding / template index / bootstrap artifacts | 0 | 2026-06-13 |
| Zero open Critical/High Dependabot alerts | — | 2026-06-13 |

---

## Post-Push Checklist (closed 2026-06-13)

- [x] [AUTO] Local bootstrap artifact check
- [x] [AUTO] Local file line limits check
- [x] [AUTO] Local `dotnet test` (20 passed)
- [x] [AUTO] `ci.yml` all jobs green on `main`
- [x] [AUTO] `codeql.yml` and `security.yml` green on `main`
- [x] [HUMAN] Sprint 0/1 CI approval after green run on `main`

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

---

## Sprint 3 — Core Integrity (2026-06-13)

**Goal:** Enforce Core 200-line limit; remove grandfather entries from `check-file-limits.sh`.

### Sequential

- [x] [AGENT] Split `Core/FtpScanner.cs` → `FtpListingParser`, `FtpDirectoryClient`, `FtpScanPlanner`, `FtpScanProgress` (195 lines)
- [x] [AGENT] Split `Core/IngestEngine.cs` → `IngestFileNaming`, `IngestVerification`, `IngestItemProcessor` (162 lines)
- [x] [AGENT] Split `Core/ThumbnailService.cs` → `ThumbnailDiskCache`, `ExifThumbnailReader`, `ShellThumbnailInterop` (198 lines)
- [x] [AGENT] Split `Core/ServiceContracts.cs` → `IDatabaseService` in `Data/`; factories in `Core/Factories/` (101 lines)
- [x] [AUTO] `scripts/check-file-limits.sh` passes with empty grandfather list for Core

### Parallel (partial)

- [x] [AGENT] Extract shared media-extension constants — `Core/MediaExtensions.cs` (`IsRawExtension`, extended video list)
- [-] [AGENT] Unify FTP stacks — deferred to backlog
- [-] [AGENT] Decouple `ShootFilterService` from ViewModels — deferred to backlog
- [-] [AGENT] Remove WPF `BitmapSource` from Core contracts — deferred to backlog

---

## Sprint 4 — ViewModel & UI Cleanup (partial, 2026-06-13)

### Sequential

- [x] [AGENT] Rename `MainViewModel.Part9–Part17` → semantic partials (`Thumbnails`, `FtpThumbnails`, `UnifiedSource`, `ImportEngine`, `ImportPostProcess`, `ImportExecution`, `DriveExclusions`, `DriveScan`, `Onboarding`)
- [x] [AGENT] Rename `Filters` → `FtpWorkflow`, `Updates` → `SourceLoad`; moved `BuildUpdateHandoffScript` to `Import.partial.cs`
- [x] [AGENT] Add `ViewModels/GlobalUsings.cs` (non-conflicting project globals)
- [x] [AGENT] Overlay decouple (partial): `PreferencesOverlay`, `ScanExclusionsOverlay`, `ImportHistoryOverlay` bind to VM commands; code-behind emptied
- [ ] [AGENT] `DialogOverlaysView` + remaining MainWindow handlers — see `BUILD_PLAN.md`

---

## Sprint 5 — Test Coverage Expansion (2026-06-13)

**Baseline:** 51 unit tests, all passing (`dotnet test`).

| Component | Tests added |
|-----------|-------------|
| `FtpListingParser` | 10+ cases (`FtpListingParserTests`) |
| `IngestFileNaming` duplicate policies | 4 cases (`IngestFileNamingTests`) |
| `IngestVerification` strict/fast | 2 cases (`IngestVerificationTests`) |
| `DatabaseService` injectable path | `DatabaseServiceTests` |
| `LocalScanner` extension filter | 3 cases (`LocalScannerTests`) |
| `MediaExtensions` | `MediaExtensionsTests` |

### Ongoing maintenance (agent)

- [x] [AGENT] Merged Dependabot PR #3 (github-actions group)
- [x] [AGENT] Dependabot PR #4 — safe bumps applied locally; PR closed (MD5/Extensions 10 deferred)
- [x] [AGENT] CVE triage automation pass — no open critical alerts (2026-06-13)

---

## Sprint 4 — ViewModel & UI Cleanup (complete, 2026-06-13)

- [x] [AGENT] `IFileDialogService` / `IShellService` + WPF implementations; wired into `BrowseDestination`, About actions
- [x] [AGENT] `DialogOverlaysView` decoupled — `PasswordBoxAssist`, `OpenLogsFolderCommand`, `ReportBugCommand`; code-behind emptied
- [x] [AGENT] FTP workflow status strings moved to `Strings.resx` (`Vm_Ftp_*` keys)
- [x] [AGENT] `AutomationProperties.Name` on collapsed sidebar icon buttons
- [x] [AGENT] `ShootFilterService` decoupled from Localization — uses `Core/Models/FilterFileTypeIds`

---

## ADB Provider Testing (2026-06-13)

**Device:** `b5214fc6` (physical Android, USB debugging)

| Test | Result |
|------|--------|
| `AdbDeviceProbe.ListDeviceSerials` | Pass — 1 device |
| `AdbFileProvider.CopyAsync` pull from `/sdcard/DCIM/*.jpg` | Pass — file copied to temp |

**New code:** `Core/AdbDeviceProbe.cs`, `Tests/AdbFileProviderTests.cs`

**Test baseline:** 53 unit tests, all passing.

---

## Milestone 7 — FTP Preview Reliability (v1.3.6, 2026-06-13)

### Sprint 7.1: Diagnose + Reliability

- [x] [AGENT] Analyze FTP thumbnail failure (code evidence: empty `ftp.Pass`, no download retries, full-file download, broken retry path)
- [x] [AGENT] Add `FtpSourceCredentials.ResolvePassword` + `EnsureFtpSourceCredentials`; apply at scan, thumbnail, unified, reconnect
- [x] [AGENT] Extract `FtpFileDownloader` (retries, canonical `FtpUriBuilder`, zero-byte guard, `ILogger`)
- [x] [AGENT] Fix `RetryFailedPreviewLoadsAsync` for `IsFtpSource` items
- [x] [AGENT] Unit tests: `FtpUriBuilderTests`, `FtpSourceCredentialsTests`, `FtpPreviewDownloadLimitsTests`

### Sprint 7.2: Performance + Consolidation

- [x] [AGENT] `FtpThumbnailBatchFetcher` with FluentFTP capped stream download (512 KB images, 4 MB video, 2 MB RAW)
- [x] [AGENT] `IFtpThumbnailService` / `FtpThumbnailService` consolidates FTP + unified thumbnail paths
- [x] [AGENT] Remove `DebugAgentLog` instrumentation

### Sprint 7.3: Release

- [x] [AGENT] Bump to v1.3.6; 80 unit tests passing
- [ ] [HUMAN] Manual smoke: `10.0.0.23:2221/DCIM` — thumbnails load fast and reliably
- [ ] [AUTO] CI green after push

**New code:** `Core/Ftp/FtpUriBuilder.cs`, `FtpFileDownloader.cs`, `FtpThumbnailBatchFetcher.cs`, `FtpPreviewDownloadLimits.cs`, `Core/Services/FtpThumbnailService.cs`, `FtpSourceCredentials.cs`, `FtpEndpoint.cs`

**Test baseline:** 80 unit tests, all passing.

---

## Milestone 8 — Thumbnail Performance + Settings Persistence (v1.3.12, 2026-06-13)

### Sprint 8.1: Settings persistence

- [x] [AGENT] `DeleteAfterImportConfirmHelper` — safety dialog only on user-initiated toggle; prompt dismissed only after OK
- [x] [AGENT] Delete After Import checkbox in Preferences Import Settings
- [x] [AGENT] `ConfigPersistenceTests` round-trip for `DeleteAfterImport` + `DeleteAfterImportPromptDismissed`

### Sprint 8.2: Thumbnail performance

- [x] [AGENT] CPU-scaled `GetFtpThumbnailWorkerCount()` (Ultra up to 16 workers)
- [x] [AGENT] Thumbnail Performance hint in Preferences (en/fr/es)
- [x] [AGENT] FTP thumbnail disk cache (`host|port|remotePath|fileSize`) in `ThumbnailDiskCache` + `FtpThumbnailService`
- [x] [AGENT] Skip redundant DNG FTP download when same-stem HEIC/JPG in batch (`GroupRawAndRenderedPairs`)

### Sprint 8.3: Release

- [x] [AGENT] Bump to v1.3.12; `CHANGELOG.md` updated
- [x] [AGENT] Deploy test build to Desktop (`QuickMediaIngest.exe` v1.3.12.0)
- [x] [AGENT] 85 unit tests passing (excl. integration/smoke)
- [ ] [HUMAN] Delete After Import + Ultra speed + FTP cache smoke on LAN FTP source

---

## Milestone 9 — FTP Thumbnail Pipeline v2 (v1.3.13, 2026-06-13)

### Sprint 9.1: Tier 1 — Fetch strategy

- [x] [AGENT] Zoom persistence: Ctrl+wheel + slider aligned to 50–300; `OnThumbnailSizeChanged` clamp; `ConfigPersistenceTests` for `ThumbnailSize`
- [x] [AGENT] `FtpPreviewDownloadLimits` tier API (64K → 256K → 512K → cap); HEIC cap lowered to 2 MB
- [x] [AGENT] `FtpTieredPreviewLoader` + `FtpThumbnailPipeline` — tiered download with decode-after-each-tier
- [x] [AGENT] ViewModel FTP disk cache pre-check (`FtpThumbnailCache.TryLoad`) before scheduling network work
- [x] [AGENT] `FtpPreviewDownloadTierTests`, `HeicEmbeddedPreviewReaderTests`

### Sprint 9.2: Tier 2 — Pipeline architecture

- [x] [AGENT] Viewport-priority thumbnail queue (expanded / top shoot groups first)
- [x] [AGENT] Split download pool (capped at 6) vs decode pool (`FtpThumbnailLoadOptions`)
- [x] [AGENT] `FtpStreamingDownloader` per-host FluentFTP connection pool (Max/Ultra); `FtpWebRequest` fallback

### Sprint 9.3: Tier 3 — Advanced decode + transport

- [x] [AGENT] `HeicEmbeddedPreviewReader` — partial-buffer JPEG segment extraction before Magick
- [x] [AGENT] NetVips / `VipsThumbnailDecoder` shrink-on-load path; Magick fallback

### Sprint 9.4: Release

- [x] [AGENT] Bump to v1.3.13; `CHANGELOG.md` updated
- [x] [AGENT] Deploy test build to Desktop (`QuickMediaIngest.exe` v1.3.13.0)
- [x] [AGENT] 89 unit tests passing (excl. integration/smoke)
- [ ] [HUMAN] Cold load `10.0.0.23:2221/DCIM` — tiered bytes in log; cache hit on reconnect; zoom persists; FluentFTP + libvips on Max/Ultra

---

## Template Migration Sprint — v0.11.0 (2026-06-20)

Aligned with upstream [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) **v0.11.0** (was pinned v0.2.0). Critique mitigations in `DECISION_LOG.md`.

- [x] [AGENT] Phases 1–3 — slash commands, Cursor rules, gate scripts, CI jobs (`batch-commands`, `repo-hygiene`, WPF `feature-gate.sh`)
- [x] [AGENT] Phase 4 — `TEMPLATE_INDEX.json`, docs (`AGENTS.md`, `FOR_AGENTS.md`, `START_HERE.md`, etc.), `.template-version` bump
- [x] [AGENT] Phase 5 — gate suite + 101 tests; super-command smoke in `DECISION_LOG.md`
- [x] [HUMAN] Slash-command `/` menu sign-off; template version bump approval
- [x] [AUTO] CI green — `e59ec6f`, CRLF fix `7720986`

---

## Human Verification Automation (2026-06-20)

Automated BUILD_PLAN HUMAN rows for v1.3.12 / v1.3.13 acceptance:

- [x] [AGENT] `LanFtpSmokeProbe` + `HumanVerificationSmokeTests` — tier caps, reconnect cache, Ultra mode (skip when LAN FTP offline)
- [x] [AGENT] `ConfigFilePersistenceTests`, `MainViewModelConfigReloadTests` — Delete After Import, `ThumbnailSize`
- [x] [AGENT] `DeleteAfterImportConfirmHelperTests`, `PublishedExeSmokeTests`
- [x] [AGENT] `scripts/smoke-human-verification.ps1/.sh`, `scripts/smoke-published-exe.ps1/.sh`
- [x] [AGENT] `--smoke-libvips` headless flag; `NetVips.Native.win-x64` for single-file publish
- [x] [AUTO] Optional CI job for human-verification smoke; `--smoke-libvips` in `ci.yml`

**Test baseline:** 106 unit tests (Release).

---

## Human Sign-off Automation (2026-06-21)

Single orchestrator replaces BUILD_PLAN HUMAN checklist items:

- [x] [AGENT] `scripts/run-human-signoffs.ps1/.sh` — tests, security triage, optional push/CI poll
- [x] [AGENT] `scripts/ensure-gh-security-scope.sh` — Dependabot API probe + optional `gh auth refresh`
- [x] [AGENT] `HumanSignoffVerificationTests` — Delete After Import + ThumbnailSize bindings after config reload
- [x] [AGENT] STA `WpfTestFixture` for reliable headless WPF control tests
- [x] [AUTO] CI job `human-signoffs` with `GITHUB_TOKEN` security-events read

**Test baseline:** 109 unit tests (Release).

---

## Import Progress + ETA — F-002 (2026-06-21)

Byte-weighted import progress with parallel copies (up to 8) unchanged:

- [x] [AGENT] `ImportByteProgressTracker` + `ImportByteProgressSnapshot` — thread-safe completed + in-flight bytes
- [x] [AGENT] `IFileProvider.CopyAsync` optional `IProgress<long>` — Local, FTP, ADB providers
- [x] [AGENT] `IngestItemProcessor` start/complete events; `MainViewModel.ImportProgress.partial.cs` shared ETA/bar
- [x] [AGENT] `ImportByteProgressTrackerTests` + `IngestEngineTests` incremental copy progress

**Test baseline:** 113 unit tests (Release).

---

## Backlog Parallel Lane — P1–P8 (2026-06-20)

| P | Task | Status |
|---|------|--------|
| 1 | Published portable exe libvips + native DLL smoke | ✅ |
| 2 | `DialogOverlaysView` + remaining MainWindow handlers | ✅ |
| 3 | Grandfather file-limit cleanup (MainViewModel + Core/Ftp) | ✅ |
| 4 | Headless WPF smoke in CI | ✅ |
| 5 | Core architecture (`object?` thumbnail, `ShootFilterService`, FTP stack unification) | ✅ |
| 6 | UI UserControl extractions | ✅ N/A — `MainWindow.xaml` under 800-line limit |
| 7 | MSI admin-extract validation in `build.yml` | ✅ |
| 8 | Template update check + OpenSSF Scorecard workflow | ✅ |

### P3 file splits (grandfather cleared)

- `MainViewModel.ConfigLoad.partial.cs`, `FtpCollections.partial.cs`, `ThumbnailsLoad.partial.cs`
- `FtpDownloadSync.cs`, `FtpTieredPreviewDecoder.cs`, `FtpThumbnailPipeline.{Fetch,Tiered,Helpers}.cs`
- `ThumbnailService.Decode.partial.cs`

### P7 / P8 CI

- `scripts/validate-msi-install.ps1` — administrative MSI extract + libvips smoke
- `.github/workflows/scorecard.yml`, `scripts/check-scorecard-sarif.sh`

---

## Audit Sprint R1 — 2026-06-21 (partial)

Full report: `CODE_REVIEW.md` (gitignored, local only).

### Completed

- [x] [AGENT] F-002 — `scripts/check-readme-health.sh` (README sections + doc link validation)
- [x] [AGENT] F-003 — Gitignore `CODE_REVIEW.md` + `build_errors.md`; untrack stale `build_errors.md`
- [x] [AUTO] Gate verification — bootstrap, feature-gate (9 stages), hygiene, readme health, 106 tests
- [x] [AGENT] F-001 — P1–P8 backlog + v1.3.17 release (shipped `8556015`)

### Optional (not blocking)

- [ ] [HUMAN] F-004 — `gh auth refresh -s security_events` (local Dependabot strict only; CI uses `GITHUB_TOKEN`)
- [ ] [HUMAN] Optional live UI glance — headless sign-off tests cover bindings
