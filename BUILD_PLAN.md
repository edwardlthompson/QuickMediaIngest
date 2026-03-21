# QuickMediaIngest Build Plan & Sprint Roadmap

This document breaks down the development roadmap into actionable sprints for incremental delivery. Each milestone is split into focused sprints with clear goals and deliverables. Check off each sprint as it is completed.

---

## Milestone 1 – Bug Fixes & Foundation (1–2 weeks)

**Goal:** Stabilize core, fix compilation/runtime issues, improve maintainability

### Sprint 1.1: Core Bug Fixes & Licensing

- [x] Fix `ItemProcessed` event signature in `IngestEngine.cs`  
  → Change to `public event Action<IngestProgressInfo>? ItemProcessed;`
- [x] Add MIT license file (`LICENSE`) and credit any third-party assets

### Sprint 1.2: Documentation & Nullability

- [x] Add basic XML documentation comments to public APIs in Core & Providers
- [x] Enable nullable reference types project-wide (`#nullable enable`)

### Sprint 1.3: Logging & Test Scaffolding


- `IngestEngine.ResolveFileName`
- `GroupBuilder` logic
- Whitelist filter matching


- [x] Add initial unit tests (xUnit + Moq) for:
  - `IngestEngine.ResolveFileName`
  - `GroupBuilder` logic
  - Whitelist filter matching

## Milestone 2 – Architecture & Developer Experience (2–3 weeks)

**Goal:** Make the codebase more testable, extensible, and pleasant to work on

### Sprint 2.1: Dependency Injection

- [x] Introduce Microsoft.Extensions.DependencyInjection
  - Register all services/providers in `App.xaml.cs` (complete)
  - Inject into `MainViewModel`, `IngestEngine`, providers, etc. (complete)

### Sprint 2.2: MVVM Toolkit Migration

- [x] Migrate `MainViewModel` (and others) to CommunityToolkit.Mvvm
  - Use `[ObservableProperty]`, `[RelayCommand]` (complete)
  - Remove manual `INotifyPropertyChanged` + `ICommand` boilerplate (complete)

### Sprint 2.3: Model & Logging Improvements

- [x] Convert suitable models to `record` types (`DeviceConfig`, `WhitelistRule`, etc.)
- [x] Add structured logging levels (Info/Warning/Error) throughout ingestion flow

---

## Milestone 3 – Performance & Speed Optimizations (2–3 weeks)

**Goal:** Make ingestion noticeably faster, especially for large cards / repeated scans


### Sprint 3.1: Parallelization & Caching

- [x] Parallelize file copy loop in `IngestEngine.IngestGroupAsync`
  - Use `Parallel.ForEachAsync` or `SemaphoreSlim` (target 4–8 concurrent)
- [x] Implement thumbnail disk cache
  - Store in `%AppData%\QuickMediaIngest\Thumbnails\`
  - Key by file hash or SourcePath + DateTaken


### Sprint 3.2: FTP & Indexing Optimizations

- [x] Optimize FTP provider
  - Use binary mode, thread-safe data connections (FluentFTP if upgrading)
  - Batch directory listings where possible
- [ ] Add lightweight SQLite index on `DeviceId` + `Path` in `DatabaseService` (if using SQLite)  <!-- Not implemented -->

---

## Milestone 4 – UX/UI Polish & Usability Wins (2–4 weeks)

**Goal:** Make the app feel more professional and photographer-friendly

### Sprint 4.1: Settings & Preview


  - [x] Naming template live preview
  - [x] Whitelist rule manager (add/edit/delete)
  - [x] Toggle move/copy, post-import delete option

### Sprint 4.2: Progress & Grid Enhancements


  - [x] Dedicated Ingest Log panel (ListView with icons + expandable errors)
  - [x] Overall + per-group progress bars
  - [x] Working cancel button (propagate CancellationToken)
  - [x] Hover zoom / right-click full preview
  - [x] Better multi-select (Shift/Ctrl)
  - [x] Ensure virtualization works with large sets


  - [x] Add first-run onboarding tooltips / tour


## Milestone 5 – High-Value Features (3–6 weeks – can be split)

 [x] Quick dark/light mode toggle in title bar
**Goal:** Add features photographers frequently request

### Sprint 5.1: Post-Import & Filtering

- [x] Post-import actions
  - [x] Auto-open destination folder
  - [x] Eject/unmount source device (if possible)
  - [x] Optional sidecar `.xmp` or album JSON export
  - [x] Advanced grid filtering / search
    - [x] Date range, file type, filename keyword
    - [x] Use `CollectionViewSource`

### Sprint 5.2: Duplicates & Stretch Goals

- [x] Duplicate detection across sources (quick hash comparison)
- [x] (Stretch) ADB fallback for faster Android transfers
- [x] (Stretch) Export/import app settings (JSON)

---

## Nice-to-Have / Future Ideas

- [x] Better error recovery (retry failed files, skip vs abort)

---

**How to Use:**

- Work through each sprint in order, checking off tasks as completed.
- Adjust sprint boundaries as needed for team size and velocity.
- Use this document to track progress and plan future releases.
