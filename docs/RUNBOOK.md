# Runbook

> Operational guide for Quick Media Ingest releases and incident response.

## QA Checklist (manual)

| Step | Action | Expected |
|------|--------|----------|
| Settings save | Open Preferences → change destination → **Save & Close** → reopen | Change persisted in `config.json` |
| Folder naming | Import a shoot → check destination folder name → export album to same root | Import and export use `yyyyMMdd_HHmmss_Title` format |

## Health Checks (Desktop)

| Check | Command / Action | Expected |
|-------|------------------|----------|
| Build | `dotnet build -c Release` | Exit 0 |
| Tests | `dotnet test -c Release` | All pass |
| Portable launch | `publish/local-test/QuickMediaIngest.exe` | App starts, no crash |
| Logs | `%AppData%\QuickMediaIngest\Logs\` | Readable log files on error |

## Structured Logging

- File logger in `QuickMediaIngest/Core/Logging/FileLogger.cs`
- Levels: Info, Warning, Error
- **Never** log FTP passwords or tokens
- User-visible errors in ingest log panel + notification feed

## Deploy (Release)

1. `[AUTO]` CI green on `main`
2. `[HUMAN]` Approve release (Milestone Gates in `BUILD_PLAN.md`)
3. `[HUMAN]` Bump `<Version>` in `QuickMediaIngest/QuickMediaIngest.csproj`
4. `[HUMAN]` Update `CHANGELOG.md`
5. `[AUTO]` Push to `main` — `build.yml` builds portable EXE + MSI, creates GitHub Release
6. `[HUMAN]` Verify release assets on GitHub

Alternative local release: `build_and_push.bat`

## Rollback

1. Direct users to prior GitHub Release (portable EXE or MSI)
2. External updater can install previous version if configured
3. Log incident in `DECISION_LOG.md` if user-impacting

## Common Failures

| Symptom | Check | Fix |
|---------|-------|-----|
| CI failing on tests | `dotnet test` locally | Fix test or code |
| SQLite crash on portable build | `IncludeAllContentForSelfExtract` in csproj | Verify csproj setting |
| FTP connection fails | Credential Manager entry | Re-enter credentials in UI |
| Dependabot alert | `docs/SECURITY_TRIAGE.md` | Merge bump PR |
| File in use on update | Updater handoff / close app | Use external updater wait |

## Backup & Restore

| Target | Procedure |
|--------|-----------|
| User settings | Export JSON from app; copy `%AppData%\QuickMediaIngest\` |
| Repository | `git clone` |

## Escalation

1. Check `BUILD_PLAN.md` Ongoing Maintenance
2. Review `docs/SECURITY_TRIAGE.md` for security issues
3. Contact maintainers in `.github/CODEOWNERS`

## Secret Rotation

When credentials leak:

1. **`[HUMAN]`** Revoke compromised FTP credentials at source
2. **`[HUMAN]`** Remove stale entries from Windows Credential Manager
3. **`[AGENT]`** Rotate any CI secrets in GitHub Settings
4. **`[HUMAN]`** Log incident in `DECISION_LOG.md`
