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

**Current release:** [v1.3.5](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.5) (2026-06-13)

**All agent/automatable sprints complete** — see [`COMPLETED_TASKS.md`](COMPLETED_TASKS.md).

**Remaining:** HUMAN-only maintenance (weekly CVE sign-off).

---

## Ongoing Maintenance

### Weekly (recurring)

- [x] [AGENT] CVE triage pass — 2026-06-13: no open critical Dependabot alerts (`gh api dependabot/alerts`)
- [ ] [HUMAN] Run weekly CVE triage pass per `docs/SECURITY_TRIAGE.md` (recommended: Monday; sign-off)
- [ ] [AUTO] Trivy + CodeQL + CI green after next push

### Backlog (not blocking)

| Task | Owner | Notes |
|------|-------|-------|
| Plan MaterialDesignThemes 4.x → 5.x migration | AGENT | ✓ MD 5.3.2 with MaterialDesign2.Defaults |
| Microsoft.Extensions 8 → 10 bump | AGENT | ✓ 10.0.9 on net8.0-windows |
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
