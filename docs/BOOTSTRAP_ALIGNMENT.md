# Bootstrap Alignment — QuickMediaIngest ↔ agent-project-bootstrap

> Alignment of this live .NET 8 WPF repo with upstream
> [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) **v1.0.0**.
> Not a fresh bootstrap. Application code and `modules/dotnet-wpf` are preserved.

**Started:** 2026-07-21 · **From:** template `0.11.0` · **Aligned process level:** `1.0.0` (Canon + Mixed catch-up)

---



## 1.0.0 sync (2026-08-30)

Catch-up from process `0.16.0` to upstream release **v1.0.0**:

- Copied Canon: `.cursor/commands/` (incl. `/upgrade`, `/coach`, `/tour`, `/adr`, `/update-deps`), `.cursor/rules/` (kept `wpf-mvvm.mdc`), `docs/help/`, `docs/CURSOR_MODES.md`, `docs/BATCH_COMMANDS.md`, new template scripts, Golden Path schemas, example stubs
- Merged Mixed: `bootstrap.config.json` (QMI values), `.gitignore`, `.env.example`, `TEMPLATE_INDEX.json`, `PROJECT_CHECKLIST.md`, `validate-bootstrap.sh` (still skips web-only artifacts)
- Synced adapters via `bootstrap-lifecycle.sh --sync-adapters` (did not rewrite Sacred `AGENTS.md`)
- Created `branding/product.json` with `mode: product` and QMI name/tagline/pitch
- Skipped: `examples/**`, release-please/Pages, `docs/spec.md` / `docs/plan.md` stubs, product app under `QuickMediaIngest/`

## 0.16.0 sync (2026-08-10)

Cherry-picked upstream `agent-project-bootstrap` **0.16.0** process tooling:

- Opt-in Codex review (`/codex-review`, `docs/CODEX_REVIEW.md`, workflow example — not enabled as active CI)
- Expanded `/prerelease` (autofix → optional Codex → hard gate); QMI human sign-off note preserved
- Resolved `### Critique` (Issue→Resolution) in Plan Mode docs/rules/commands
- Multi-stack autofix helpers + `apply-suggested-gate-fixes` (WPF `dotnet-format` allowlisted)
- Version pins: `.template-version` / `TEMPLATE_INDEX` / manifest → `0.16.0`

Skipped: `examples/**` npm security (no examples tree), release-please/Pages, enabling Codex CI.

## Gap analysis

| Dimension | This repo (0.11.0) | Upstream (0.15.1) | Verdict |
|-----------|--------------------|-------------------|---------|
| Template version | `.template-version` / `TEMPLATE_INDEX` = `0.11.0` | `0.15.1` | Align process/tooling |
| Stack | .NET 8 WPF + `modules/dotnet-wpf` | android/web/python/node/lightroom/rust/go (no WPF) | Keep QMI module only |
| Agent entrypoints | AGENTS, START_HERE, CURSOR_MODES, FOR_AGENTS, 25 commands | + FOSS Cursor pack, `/cleanup`, parallel/`HUMAN_BACKLOG` | Update overlays |
| Memory / process | AGENT_MEMORY, DECISION_LOG, KB, COMPLETED_TASKS | + HUMAN_BACKLOG; BUILD_PLAN `🔲/✅/❌` | Adopt structure |
| `.cursor/rules` | 12 incl. `wpf-mvvm.mdc` | 15 (+ local-compute, feature-modules, design-system, commercial) | Add FOSS rules; keep WPF |
| Cursor FOSS | Missing hooks/skills/agents | Full pack + docs + checks | Additive adopt |
| Scripts | ~45 WPF-aware gates | ~90+ (parallel, cursor, purge-ephemeral, …) | Allowlist cherry-pick |
| CI | `build.yml` + ci/codeql/security/scorecard/dependency-review | + release-please, pages, stale, weekly-health, automerge | Keep WPF release; **no new workflows** |
| Broken refs | Cited `REPO_HYGIENE.md`, `purge-ephemeral.sh`, maintainer docs | Present upstream | Restore or drop citations |

### Already matches

Core agent router, most slash commands, security docs, Dependabot, CodeQL/Scorecard/security workflows, pre-commit, template update checker, encoding/hygiene philosophy, FOSS/MIT.

### Conflicts (handled carefully)

1. **Release automation** — csproj + Windows `build.yml`; do not enable release-please or replace `build.yml`.
2. **File limits** — QMI WPF 800 / 400 / 200 (not upstream 300/150).
3. **BUILD_PLAN icons** — normalize to `🔲` / `✅` / `❌`.
4. **dotnet-wpf** — downstream-only module; keep in TEMPLATE_INDEX.
5. **INITIALIZATION_PROMPT** — manual merge only.

### Recommended stack

Active: **dotnet-wpf only**. Do not copy inactive `modules/*` or `examples/*`.

---

## Risk mitigations (binding)

| Risk | Mitigation |
|------|------------|
| Release-please / Pages / automerge | Hard ban this pass; defer to HUMAN_BACKLOG |
| Blind overwrite validate-bootstrap / TEMPLATE_INDEX | Merge only; keep WPF paths |
| Hooks block PowerShell gates | Dry-run validate-local + feature-gate after copy |
| Mass script copy | Allowlist only; skip android/fdroid/web-only |
| Parallel races | Single-writer Sequential on shared control files |
| UTF-16 / BOM | UTF-8 writes + encoding check |
| Broken doc links | Link audit; restore REPO_HYGIENE; drop unused maintainer citations |
| File-limit regression | Preserve 800/400/200 |
| Premature version bump | Bump `.template-version` only after S5 green |

---

## Migration notes

### Done by agent (this pass) — completed 2026-07-21

- Gap analysis + decision record (this file + DECISION_LOG).
- Agent surface refresh (docs, rules, commands, FOSS Cursor pack, `/cleanup`).
- Hygiene/docs scripts (REPO_HYGIENE, purge-ephemeral, parallel/cursor helpers, agent-run).
- BUILD_PLAN official labels + emoji status + Sequential/Parallel/Human lanes.
- Merge-updated `validate-bootstrap.sh`; TEMPLATE_INDEX additive FOSS paths; kept `modules.dotnet-wpf`.
- Hooks dry-run + `validate-local -QuickBootstrap` + feature-gate + `dotnet test` (151 passed).
- Template version bumped to `0.15.1` (`.template-version`, manifest, TEMPLATE_INDEX).
- Follow-up (2026-07-22): automated former HUMAN_BACKLOG CI items via `automate-human-backlog.ps1`.

### Still needs `[HUMAN]`

| Item | Reason |
|------|--------|
| `gh auth refresh -s security_events` | One-time browser OAuth for local Dependabot alert API |
| WPF UI sign-off | Product release glance via `run-human-signoffs.ps1` |

### Automated from former HUMAN_BACKLOG (2026-07-22)

| Item | Resolution |
|------|------------|
| release-please / pages | Declined permanently (csproj + `build.yml`) |
| stale.yml | Enabled |
| weekly-health-check.yml | Enabled (WPF-adapted) |
| dependabot-automerge.yml + AUTOMERGE_TOKEN | Enabled + secret set |
| Re-run helper | `.\scripts\automate-human-backlog.ps1` |

### Do not expect

- Upstream `modules/dotnet-wpf` (does not exist).
- Full `examples/` sync.
- Replacement of `build.yml` with release-please.
