# Upgrading From Template

Child repos do not auto-sync with the upstream template. Use this guide when the update checker notifies you of a new release.

## Step 1: Read the Notification

Run `scripts/check-template-updates.sh` or `pwsh scripts/check-template-updates.ps1`.

## Step 2: Review CHANGELOG

Read the upstream release notes at [github.com/edwardlthompson/agent-project-bootstrap/releases](https://github.com/edwardlthompson/agent-project-bootstrap/releases).

## Step 3: Cherry-Pick by Area

| Changed area | Strategy | Owner |
|-------------|----------|-------|
| `.github/workflows/` | Cherry-pick or manual merge | AGENT + HUMAN review |
| `.cursor/rules/` | Copy new/changed `.mdc` files | AGENT |
| `docs/INITIALIZATION_PROMPT.md` | Manual review; do not blind overwrite | HUMAN |
| `scripts/` | Copy updated scripts | AGENT |
| `scripts/check-file-encoding.sh` | Copy + verify CI/pre-commit gate | AGENT |
| `scripts/validate-bootstrap.sh` | Merge new required artifacts | AGENT |
| `.env.example` | Merge new vars; never overwrite local `.env` | AGENT |
| `TEMPLATE_INDEX.json` | Run validate script after merge | AGENT |

## Version Compatibility

| Upgrade | Notes |
|---------|-------|
| 0.1.x → 0.1.y | Safe PATCH; cherry-pick freely |
| 0.1.x → 0.2.0 | Check CHANGELOG for new files/schema changes |
| 0.x → 1.0.0 | Full review; init prompt structure may have changed |

## Decision Points

- `[HUMAN]` Approve which upstream changes to adopt
- `[AGENT]` Apply diffs to matching files
- `[AUTO]` CI validates after merge

## QuickMediaIngest-Specific Notes

- Do not copy `examples/web`, `examples/python`, or `examples/android` — not applicable
- Preserve `modules/dotnet-wpf/MODULE.md` customizations when merging
- Re-run `scripts/validate-bootstrap.sh` and `dotnet test` after any merge
