# Project Checklist

> Child product checklist (merged from template v1.0.0). Status: 🔲 open · ✅ done · ❌ blocked.
> Project: **Quick Media Ingest** · Stack: `dotnet-wpf` · License: `MIT`

## Setup

- ✅ README updated with value proposition and quickstart
- ✅ Environment variables configured (`.env.example` mirrored; `.env` not committed)
- 🔲 `docs/spec.md` and `docs/plan.md` filled for the first milestone (Sacred — human; not created from template stub)
- ✅ Initial tests passing in the local environment
- 🔲 Pre-commit hooks installed (`pre-commit install`)

## Security & CI (defaults on)

- ✅ CI workflow verified on GitHub (required check: **CI**)
- ✅ Security Scan / CodeQL / secret scanning green
- ✅ Dependabot alerts enabled
- 🔲 Branch protection applied to the default branch
- ✅ `SECURITY.md` reporting channel confirmed

## Agent adapters

- ✅ `AGENTS.md` reviewed for this product (Sacred — do not overwrite from template)
- ✅ Adapters current (`bash scripts/bootstrap-lifecycle.sh --sync-adapters`)
  - `.cursor/rules/main.mdc`
  - `CLAUDE.md`
  - `.github/copilot-instructions.md`

## Next

1. `python3 scripts/agent-run.py validate-bootstrap --quick`
2. `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`
3. `scripts/setup-github-repo.sh` (or `.ps1`) for alerts and branch protection
