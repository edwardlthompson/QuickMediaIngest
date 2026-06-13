# ADR-0001: Core Architecture

**Status:** Accepted (pre-existing; documented 2026-06-13)

## Context

Quick Media Ingest is a Windows desktop media importer supporting multiple source types (local/removable drives, FTP, optional ADB) with a rich WPF UI.

## Decision

1. **MVVM** with CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`). Large `MainViewModel` split into partial files by concern.
2. **Dependency injection** via `Microsoft.Extensions.DependencyInjection` in `App.xaml.cs`.
3. **Provider pattern:** `IFileProvider` implemented by `LocalFileProvider`, `FtpFileProvider`, `AdbFileProvider`. Scanners merge via `UnifiedConcreteSourceScanService`.
4. **Core isolation:** Business logic (`IngestEngine`, `GroupBuilder`, `WhitelistFilter`, services) lives in `QuickMediaIngest/Core/` with no WPF references — unit-testable with xUnit + Moq.
5. **Persistence:** SQLite via `DatabaseService` for config/history; user settings in `AppConfig`.
6. **UI:** MaterialDesignThemes; localized strings via `.resx` (en/es/fr).

## Consequences

- New source types: implement `IFileProvider` + register in DI.
- UI changes: ViewModels + XAML; keep ingest logic out of code-behind where possible.
- Sprint 1 backlog: split oversized `MainWindow.xaml` / `MainViewModel.cs` into UserControls and additional partials.

## Trust Boundaries

```
[User] → [WPF UI / ViewModels] → [Core Services / IngestEngine] → [File Providers] → [Local/FTP/ADB]
                ↓
         [SQLite / AppConfig / Credential Manager]
```
