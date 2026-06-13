# For Agents

## Phased Loading

SessionStart → START_HERE.md → Mode → AGENTS.md → BUILD_PLAN Sequential → Active module → Plan Mode → Execute

## Token Economy

1. Never read inactive stack modules or examples
2. Never fill KNOWLEDGE_BASE.md with generic framework docs
3. Update memory files only at session start, milestone end, or architectural pivot
4. Read-before-write: @filename before edits
5. Sequential before Parallel in BUILD_PLAN

## Architecture Map

```
QuickMediaIngest/                 # WPF shell (net8.0-windows)
├── App.xaml.cs                   # DI composition root
├── MainWindow.xaml               # Shell layout (sidebar, groups, overlays host)
├── MainWindow.*.partial.cs       # Code-behind split: Chrome, Ribbon, Settings
├── Controls/                     # Overlay UserControls (Preferences, History, Exclusions)
├── ViewModels/
│   ├── MainViewModel.cs          # Shell properties, ctor, sidebar init
│   └── MainViewModel.*.partial.cs # Semantic domains: Config, Import, Scan, Ftp, …
├── Core/                         # No WPF references — unit-testable
│   ├── IngestEngine.cs           # Import orchestration
│   ├── GroupBuilder.cs           # Post-import album/XMP export
│   ├── GroupFolderNaming.cs      # Shared shoot-folder naming
│   ├── FtpScanner.cs, LocalScanner.cs, ThumbnailService.cs
│   └── ServiceContracts.cs       # Core interfaces + factories
├── Data/
│   └── DatabaseService.cs        # SQLite VACUUM maintenance only
└── Localization/                 # Strings.resx (+ es, fr)

QuickMediaIngest.Tests/           # xUnit + Moq
scripts/                          # CI gates (bash + validate-local.ps1)
.github/workflows/                # ci.yml, build.yml, codeql.yml, security.yml
```

**Persistence:** User config, import history, and presets live in JSON under `%AppData%\QuickMediaIngest\`. SQLite is retained for periodic `VACUUM` only (see `DECISION_LOG.md`).

## Partial-File Conventions

| Area | Rule | Split tool |
|------|------|------------|
| `MainViewModel` | One domain per `MainViewModel.<Domain>.partial.cs`; avoid new `PartN` files | `tools/split_mainviewmodel.py` |
| `MainWindow.xaml.cs` | Chrome / Ribbon / Settings partials; ≤400 lines each | `tools/split_mainwindow_cs.py` |
| `MainWindow.xaml` | Overlays in `Controls/*`; shell stays in `MainWindow.xaml` | `tools/split_mainwindow.py` |
| `Core/**/*.cs` | ≤200 lines (enforced in CI) | Manual extract |

Overlay UserControls inherit `DataContext` from the shell; prefer VM `RelayCommand` bindings over `MainWindow` event forwarding.

## Gate Commands

**Windows (recommended):**

```powershell
.\scripts\validate-local.ps1
```

**Individual gates:**

```bash
bash scripts/validate-bootstrap.sh
bash scripts/check-file-limits.sh
bash scripts/check-file-encoding.sh
bash scripts/check-license-compliance.sh
dotnet build QuickMediaIngest-1.sln -c Release
dotnet test QuickMediaIngest-1.sln -c Release
dotnet list QuickMediaIngest-1.sln package --vulnerable --include-transitive
dotnet format QuickMediaIngest-1.sln --verify-no-changes
```

## Parallel Guardrails

- Branch: `feature/agent-[task-name]` per agent, separate worktree
- No overlapping file scopes
- Shared schema/types: sequential agent only first

## 3-Strike Rule

After 3 failed fix attempts: halt, summarize conflict, request human direction.

## Session Checkpoint

Write `.cursor-session-state`, clear chat, restore on restart, delete file.
