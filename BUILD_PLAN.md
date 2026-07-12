# Build Plan

> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.

**Release:** v1.3.19 · **Template:** v0.11.0 · **Tests:** 144 (Release)

**Active lane:** Maintenance — `[AGENT]` adds next scoped task to Backlog before release.

---

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | Human — OAuth, optional UI glance, push approval |
| `ADB` | Human — Android device/emulator testing |
| `AUTO` | CI, Dependabot, gate scripts |

Icons: ✅ done · ⬜ open · ⚠️ optional once

---

## Backlog

| ID | Task | Owner | Status |
|----|------|-------|--------|
| TBD | Next scoped feature or fix | AGENT | ⬜ |

---

## Before you ship

```powershell
.\scripts\run-human-signoffs.ps1              # full automated pass
.\scripts\run-human-signoffs.ps1 -PublishedExe  # + portable exe libvips smoke
$env:QMI_ALLOW_PUSH='1'; .\scripts\run-human-signoffs.ps1 -Push -WaitCi 300
```

| Check | Command |
|-------|---------|
| Human sign-offs | `.\scripts\run-human-signoffs.ps1` |
| LAN FTP smoke (optional) | `.\scripts\smoke-human-verification.ps1` |
| WPF feature gates | `bash scripts/feature-gate.sh --stack dotnet-wpf` |
| Pre-release | `bash scripts/pre-release-gate.sh` |

**Optional once:** `gh auth refresh -s security_events` for local Dependabot strict gate (CI does not need this).

---

## Archive

| Sprint | Location |
|--------|----------|
| Audit R2 backlog (D1–D3) | `COMPLETED_TASKS.md` § Audit R2 Backlog D1–D3 |
| Audit R2 | `COMPLETED_TASKS.md` § Audit Sprint R2 |
| Import progress + ETA (F-002) | `COMPLETED_TASKS.md` § Import Progress + ETA |
| Human sign-off automation | `COMPLETED_TASKS.md` § Human Sign-off Automation |
| Backlog P1–P8 + v1.3.17 | `COMPLETED_TASKS.md` § Backlog Parallel Lane |
| Template migration v0.11.0 | `COMPLETED_TASKS.md` § Template Migration |
| Human verification automation | `COMPLETED_TASKS.md` § Human Verification Automation |
| Audit R1 | `COMPLETED_TASKS.md` § Audit Sprint R1 |
