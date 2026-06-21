# Agent Router

1. **First read:** `docs/START_HERE.md`
2. **Cursor modes:** `docs/CURSOR_MODES.md` (Ask / Plan / Agent / Debug)
3. **Bootstrap mode:** `docs/INITIALIZATION_PROMPT.md`
4. **Reference mode:** `docs/FOR_AGENTS.md` + `TEMPLATE_INDEX.json`
5. **Task board:** `BUILD_PLAN.md` (Sequential before Parallel)
6. **Slash commands:** type `/` in Agent chat — see `docs/help/BATCH_COMMANDS.md`
7. **Living memory:** update `AGENT_MEMORY.md` only at milestone boundaries

> Legacy `.cursorrules` is deprecated. Use `.cursor/rules/*.mdc` and this file instead.

## Architecture Constraints

- Pure FOSS under MIT license; no proprietary closed-source SDKs in production path
- WPF file limits (adapted): `.xaml` 800 lines, ViewModels/`*.xaml.cs` 400 lines, `Core/` 200 lines
- Strict type safety (`#nullable enable`) and runtime validation at data boundaries
- Core business logic in `QuickMediaIngest/Core/` — no WPF references in Core
- MVVM via CommunityToolkit.Mvvm; partial ViewModels for large concerns
- Opt-in only telemetry; no tracking by default

## Coding Style

- Conventional Commits for all changes
- Small, modular functions; split large files per Sprint 1 backlog
- Read-before-write: inspect types/interfaces via `@filename` before editing
- Plan Mode for all non-trivial tasks; include `### Critique` in plans

## Build & Test

```bash
# Local portable build
build_local_test.bat

# Restore, build, test
dotnet restore QuickMediaIngest-1.sln
dotnet build -c Release
dotnet test -c Release --no-build
```

Output: `publish/local-test/QuickMediaIngest.exe`

## Gate Loop

After major `[AGENT]` steps:

```powershell
.\scripts\validate-local.ps1 -QuickBootstrap
# or with agent autofix loop (Git Bash):
bash scripts/watch-agent-gates.sh --once --autofix
```

On exit **2**: halt (3-strike or environment block). See `.cursor/agent-progress.json`.

## Session Protocol

- On session start: read `START_HERE.md`, `docs/CURSOR_MODES.md`, then `BUILD_PLAN.md` Sequential lane
- On milestone end: update `AGENT_MEMORY.md`, append to `DECISION_LOG.md` or `docs/adr/`
- On 3-strike failure: halt and escalate to human
- On context bloat: write `.cursor-session-state`, ask human to clear chat
- Destructive operations require `[HUMAN]` approval (see `.cursor/rules/destructive-ops.mdc`)
- Log significant agent actions in `DECISION_LOG.md` at milestone boundaries

## Module Activation

Active stack: **`modules/dotnet-wpf/MODULE.md`** only.

## Ecosystem-Specific Rules

- **.NET/WPF:** MVVM separation, xUnit for Core, MaterialDesignThemes for UI, theme QA via `docs/THEME_QA_CHECKLIST.md`
- **Distribution:** portable EXE + WiX MSI via `.github/workflows/build.yml`
