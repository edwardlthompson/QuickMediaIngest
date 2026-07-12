# Module E: .NET / WPF Desktop Applications

> Activate when your stack includes a .NET WPF (or WinUI) Windows desktop app.

## Requirements

- **Nullable & Type Safety:** `#nullable enable` project-wide; strict types at data boundaries.
- **MVVM Separation:** Core business logic in a non-UI project/folder with no WPF references. ViewModels use CommunityToolkit.Mvvm.
- **Testing:** xUnit + Moq for Core and services; `dotnet test` in CI on every PR.
- **Formatting:** `dotnet format --verify-no-changes` in CI (or `.editorconfig` enforced).
- **Dependencies:** Pin NuGet versions in `.csproj`; Dependabot for `nuget` and `github-actions`.
- **Distribution:** Portable EXE + optional MSI/Winget; GitHub Releases.

## Activation Checklist

- [x] Solution includes app + test projects
- [x] `QuickMediaIngest/Core/` isolated from WPF
- [x] xUnit tests for ingest engine, filters, grouping
- [x] CI runs `dotnet build` + `dotnet test`
- [x] Theme QA checklist for UI changes
- [x] File size remediation (Sprint 1): split oversized XAML/ViewModels
- [x] `dotnet format` gate in CI

## Build Commands

```bash
# Local portable build
build_local_test.bat

# Restore, build, test
dotnet restore QuickMediaIngest-1.sln
dotnet build -c Release
dotnet test -c Release
```

## File Size Limits (adapted for WPF)

| File type | Max lines | Notes |
|-----------|-----------|-------|
| `*.xaml` | 800 | Views; split into UserControls |
| `ViewModels/*.cs`, `*.xaml.cs` | 400 | Use partial classes |
| `Core/**/*.cs` | 200 | Pure logic |

Grandfathered files are listed in `scripts/check-file-limits.sh` until Sprint 1 splits complete.

## Operations

- Logs: `%AppData%\QuickMediaIngest\Logs\`
- Config: SQLite + `AppConfig` JSON
- Release: `build_and_push.bat` or `.github/workflows/build.yml`
- Rollback: install prior GitHub Release artifact

## Owner Labels for This Module

| Task type | Label |
|-----------|-------|
| Scaffold, tests, CI, docs | `AGENT` |
| Release approval, GitHub settings | `HUMAN` |
| `dotnet test`, encoding, bootstrap validation | `AUTO` |

After each `[AGENT]` step, run `bash scripts/watch-agent-gates.sh --once --autofix` (or `.\scripts\validate-local.ps1`). On exit **2**, halt per 3-strike rule.
