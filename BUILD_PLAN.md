# Build Plan

> Prioritized task board with owner labels. Completed milestones live in `COMPLETED_TASKS.md`.

## Owner Label Legend

| Label | Owner | When to use |
|-------|-------|-------------|
| `AGENT` | Cursor Agent | Code, docs, scaffolding, tests, CI config |
| `HUMAN` | Human developer | Approvals, credentials, GitHub settings, product decisions |
| `ADB` | Human (Android) | Android SDK, emulator/device testing (ADB provider testing) |
| `AUTO` | CI/scripts/bots | GitHub Actions, Dependabot, pre-commit, update checker |

**Task format:** `- [ ] [OWNER] Description`

**Current release:** v1.3.16

**Active work:** HUMAN smoke for FTP thumbnails on LAN (`10.0.0.23:2221/DCIM`)

---

## HUMAN Verification — v1.3.12 / v1.3.13

Source: `10.0.0.23:2221/DCIM` · Thumbnail Performance: **Ultra**

### Settings (v1.3.12)

- [ ] [HUMAN] Delete After Import: enable → confirm → restart → toggle stays on; `config.json` has `"DeleteAfterImport": true`; no startup dialog when previously confirmed

### FTP thumbnails (v1.3.12 + v1.3.13)

- [ ] [HUMAN] Cold load: log shows most files succeed at 64–512 KB tier (not 8 MB each); top/expanded shoots populate first
- [ ] [HUMAN] Reconnect same source: `FTP thumbnail cache hit` before network work; near-instant grid
- [ ] [HUMAN] Tier 3 (Max/Ultra): FluentFTP pool active; libvips decode when native DLLs load in published exe
- [ ] [HUMAN] Zoom: set slider to 200 → restart → slider at 200; `config.json` has `"ThumbnailSize": 200`

### CI

- [ ] [AUTO] `dotnet test` + CI green after push

**Acceptance reference:** see Milestones 8–9 in `COMPLETED_TASKS.md` for shipped features and key files.

---

## Ongoing Maintenance

### Weekly (recurring)

- [x] [AGENT] CVE triage pass — 2026-06-13: no open critical Dependabot alerts
- [ ] [HUMAN] Run weekly CVE triage per `docs/SECURITY_TRIAGE.md` (recommended: Monday; sign-off)
- [ ] [AUTO] Trivy + CodeQL + CI green after next push

### Backlog (not blocking)

| Task | Owner | Notes |
|------|-------|-------|
| Extract Sidebar / ShootGroups UserControls | AGENT | Optional polish |
| Remove WPF types from Core models (`ImportItem.Thumbnail`) | AGENT | Long-term |
| Unify FTP scan listing with thumbnail transport stack | AGENT | Thumbnail path done in M9; scan listing remains |
| Headless WPF smoke test in CI | AGENT | Sprint 6 follow-up |
| MSI install/uninstall CI step | AGENT | WiX validation |

---

## Open Milestone Gates

| Gate | Owner | Status |
|------|-------|--------|
| Core `/**/*.cs` ≤ 200 lines (no grandfather) | AUTO | ✓ |
| Unit test baseline (89 tests, excl. integration/smoke) | AUTO | ✓ |
| ADB provider device smoke test | ADB | ✓ (device `b5214fc6`) |
| Overlay MVVM decouple | AUTO | ✓ |
| Weekly CVE triage within last 7 days | HUMAN | Agent pass done; human sign-off pending |
| FTP thumbnails on LAN test source | HUMAN | Code shipped v1.3.13; smoke pending |
| Delete After Import persists across restart | HUMAN | Code shipped v1.3.12; smoke pending |
| Thumbnail zoom (`ThumbnailSize`) persists | HUMAN | Code shipped v1.3.13; smoke pending |
