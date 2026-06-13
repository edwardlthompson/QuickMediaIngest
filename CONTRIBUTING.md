# Contributing

Thank you for contributing to **Quick Media Ingest**.

## Who contributes what

| Label | Contributor | Examples |
|-------|-------------|----------|
| `AGENT` | Cursor Agent | Scaffolding, tests, CI config, docs |
| `HUMAN` | Human developer | Approvals, credentials, product decisions |
| `AUTO` | CI/scripts | GitHub Actions, Dependabot, pre-commit |

## Getting started

1. Fork the repository and create a feature branch.
2. Read `docs/START_HERE.md`, `AGENTS.md`, and `CODE_OF_CONDUCT.md`.
3. Report security issues via `SECURITY.md` (private reporting preferred).
4. Build and test locally:

```bash
dotnet restore QuickMediaIngest-1.sln
dotnet build -c Release
dotnet test -c Release
build_local_test.bat   # optional portable EXE
```

5. Open a PR; ensure CI passes.

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/).

## BUILD_PLAN labels

Filter tasks: `grep '\[AGENT\]' BUILD_PLAN.md`

## Pre-commit hooks

```bash
pip install pre-commit
pre-commit install
pre-commit run --all-files
```

## UI changes

Run through `docs/THEME_QA_CHECKLIST.md` before submitting large UI PRs.

## Security triage

Maintainers run a weekly CVE triage pass per `docs/SECURITY_TRIAGE.md`. Review Dependabot alerts before each release.

## Release process

1. `[AUTO]` CI green on `main`
2. `[HUMAN]` Approve release per `BUILD_PLAN.md` Milestone Gates
3. Bump `<Version>` in `QuickMediaIngest/QuickMediaIngest.csproj`
4. Update `CHANGELOG.md` (Keep a Changelog format)
5. Push to `main` — `.github/workflows/build.yml` publishes portable EXE + MSI
