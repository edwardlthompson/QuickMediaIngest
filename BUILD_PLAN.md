# Build Plan

> Prioritized task board with owner labels, Sequential and Parallel lanes per sprint.
> Move completed items to `COMPLETED_TASKS.md`.

## Owner Label Legend

| Label | Owner | When to use |
|-------|-------|-------------|
| `AGENT` | Cursor Agent | Code, docs, scaffolding, tests, CI config |
| `HUMAN` | Human developer | Approvals, credentials, GitHub settings, product decisions |
| `ADB` | Human (Android) | Android SDK, emulator/device testing (ADB provider testing) |
| `AUTO` | CI/scripts/bots | GitHub Actions, Dependabot, pre-commit, update checker |

**Current release:** [v1.3.3](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.3) (2026-06-13)

**All agent/automatable sprints complete** — see [`COMPLETED_TASKS.md`](COMPLETED_TASKS.md).

**Remaining:** HUMAN-only maintenance (MaterialDesign 5.x migration plan, release approval).

---

## Ongoing Maintenance

### Weekly (recurring)

- [x] [AGENT] CVE triage pass — 2026-06-13: no open critical Dependabot alerts (`gh api dependabot/alerts`)
- [ ] [HUMAN] Run weekly CVE triage pass per `docs/SECURITY_TRIAGE.md` (recommended: Monday; sign-off)
- [ ] [AUTO] Trivy + CodeQL + CI green after next push

### Backlog (not blocking)

| Task | Owner | Notes |
|------|-------|-------|
| Plan MaterialDesignThemes 4.x → 5.x migration | HUMAN | Breaking change; blocked Dependabot PR #4 subset |
| Microsoft.Extensions 8 → 10 bump | AGENT + HUMAN | Defer with MD5 migration |
| Extract Sidebar / ShootGroups UserControls | AGENT | Optional polish |
| Remove WPF types from Core models (`ImportItem.Thumbnail`) | AGENT | Long-term |
| Unify FTP stacks (`FtpWebRequest` scan vs FluentFTP import) | AGENT | `FtpScanner`, `FtpFileProvider` |
| Headless WPF smoke test in CI | AGENT | Sprint 6 follow-up |
| MSI install/uninstall CI step | AGENT | WiX validation |

---

## Open Milestone Gates

| Gate | Owner | Status |
|------|-------|--------|
| Core `/**/*.cs` ≤ 200 lines (no grandfather) | AUTO | ✓ |
| Core test coverage baseline (53 tests) | AUTO | ✓ |
| ADB provider device smoke test | ADB | ✓ (device `b5214fc6`) |
| Overlay MVVM decouple | AUTO | ✓ |
| Weekly CVE triage within last 7 days | HUMAN | Agent pass done; human sign-off pending |
| MaterialDesign 5.x migration plan | HUMAN | Pending |
