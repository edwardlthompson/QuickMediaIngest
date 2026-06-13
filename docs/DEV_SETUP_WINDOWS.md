# Windows Developer Setup

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| [.NET SDK](https://dotnet.microsoft.com/download) | 8.0.x | `dotnet --version` |
| [Git for Windows](https://gitforwindows.org/) | Latest | Provides Git Bash for shell scripts |
| [WiX Toolset](https://wixtoolset.org/) | 4.0.4+ | MSI builds (`dotnet tool install --global wix`) |
| [Python 3](https://www.python.org/) | 3.10+ | Optional; splitter maintenance scripts |

## Clone and build

```powershell
git clone https://github.com/edwardlthompson/QuickMediaIngest.git
cd QuickMediaIngest
dotnet restore QuickMediaIngest-1.sln
dotnet build QuickMediaIngest-1.sln -c Release
dotnet test QuickMediaIngest-1.sln -c Release
```

## Local validation (all CI gates)

```powershell
.\scripts\validate-local.ps1
```

Skip format or build when iterating:

```powershell
.\scripts\validate-local.ps1 -SkipFormat
.\scripts\validate-local.ps1 -SkipBuild
```

Bash equivalents (Git Bash):

```bash
bash scripts/validate-bootstrap.sh
bash scripts/check-file-limits.sh
bash scripts/check-file-encoding.sh
bash scripts/check-license-compliance.sh
```

## Pre-commit hooks

```powershell
pip install pre-commit
pre-commit install
pre-commit run --all-files
```

Hooks run UTF-8 encoding checks, YAML/JSON validation, gitleaks, file limits, and license compliance.

## Logs and config paths

| Path | Purpose |
|------|---------|
| `%AppData%\QuickMediaIngest\config.json` | User preferences |
| `%AppData%\QuickMediaIngest\Logs\` | File logs |
| `%AppData%\QuickMediaIngest\database.db` | SQLite file (VACUUM maintenance only) |

## Release build (local)

Portable publish:

```powershell
dotnet publish QuickMediaIngest/QuickMediaIngest.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o ./publish/portable
```

MSI (requires WiX):

```powershell
wix build Installer/QuickMediaIngest.wxs -ext WixToolset.UI.wixext -o ./publish/QuickMediaIngest.msi
```

GitHub Actions `build.yml` runs the same pipeline on `main`; use **workflow_dispatch** to create a GitHub Release.

## Agent entry points

- `START_HERE.md` — session routing
- `AGENTS.md` — stack rules
- `docs/FOR_AGENTS.md` — architecture map and conventions
- `BUILD_PLAN.md` — active sprint tasks
