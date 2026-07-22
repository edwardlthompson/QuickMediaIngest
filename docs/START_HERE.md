# Start Here

> **Read this file first** — whether you are a human or a Cursor agent.

## What is this?

**Quick Media Ingest** is a FOSS .NET 8 WPF desktop app for ingesting photos and videos from SD cards, local drives, and FTP into dated shoot-based folders. This repo was bootstrapped from [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) (process aligned to template **v0.15.1**; see `docs/BOOTSTRAP_ALIGNMENT.md`).

## Which mode are you in?

- **Bootstrap:** New project from **Use this template** → read `INITIALIZATION_PROMPT.md` next
- **Reference:** Existing project (this repo) → read `FOR_AGENTS.md` next

This repo is in **Reference mode** — the application already exists at v1.3.x.

## Bootstrap Read Order

1. `README.md`
2. `docs/START_HERE.md`
3. `docs/INITIALIZATION_PROMPT.md`
4. `AGENTS.md`
5. `BUILD_PLAN.md` Sequential lane
6. Active `modules/dotnet-wpf/MODULE.md` only

## Reference Read Order

1. `docs/START_HERE.md`
2. `docs/CURSOR_MODES.md`
3. `docs/FOR_AGENTS.md`
4. `TEMPLATE_INDEX.json`
5. `AGENTS.md`
6. `modules/dotnet-wpf/MODULE.md` only

## Slash Commands

Type `/` in Cursor Agent chat. Full registry: `docs/BATCH_COMMANDS.md` (agents) and `docs/help/BATCH_COMMANDS.md` (humans). Supers: `/bootstrap`, `/verify`, `/build`, `/ship`, `/maintain`. Also `/cleanup`.

## Do Not Read Yet

- `KNOWLEDGE_BASE.md` (until bugs are resolved)
- Upstream-only maintainer docs (this repo is a **child product**, not the template)

## BUILD_PLAN Labels

`[AGENT]` | `[HUMAN]` | `[ADB]` | `[AUTO]` — status: 🔲 open · ✅ done · ❌ blocked

## Security

Enable Dependabot alerts on GitHub (Settings → Code security and analysis). Weekly CVE triage: `docs/SECURITY_TRIAGE.md`. Vulnerability reporting: `SECURITY.md`.

## Agent Prompts

**Bootstrap:** Read @docs/START_HERE.md and @docs/INITIALIZATION_PROMPT.md. Follow Section 8. Use BUILD_PLAN Sequential lane.

**Reference:** Read @docs/FOR_AGENTS.md and @TEMPLATE_INDEX.json. Apply matching rules. Follow `modules/dotnet-wpf/MODULE.md`.
