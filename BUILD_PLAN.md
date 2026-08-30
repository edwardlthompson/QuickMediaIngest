# Build Plan

> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.
> Alignment record: `docs/BOOTSTRAP_ALIGNMENT.md`.

**Release:** v1.3.27 · **Template:** v1.0.0 · **Tests:** 360 (Release)

> **Bootstrap alignment 0.11 → 1.0.0** (process); 0.16.0 and earlier archives in COMPLETED_TASKS.md / `docs/BOOTSTRAP_ALIGNMENT.md`.
>
> **Ongoing Maintenance and Feature Backlog (I-02 to I-80)** archived in COMPLETED_TASKS.md.
>
> **Golden Path catch-up and Sequential hybrid ADB** archived in COMPLETED_TASKS.md.
>
> **Release v1.3.27** donate + filename-version updates archived in COMPLETED_TASKS.md.
>
> **Release v1.3.22** archived in COMPLETED_TASKS.md.

---

## Legend

| Label | Owner |
|-------|-------|
| `[AGENT]` | Cursor Agent — code, docs, tests, CI |
| `[HUMAN]` | Human — OAuth, optional UI glance, push approval, deferred CI policy |
| `[ADB]` | Human — Android device/emulator testing (N/A for this product) |
| `[AUTO]` | CI, Dependabot, gate scripts |
Status markers (emoji only — never GitHub `- [ ]` checkboxes):

| Marker | Meaning |
|--------|---------|
| 🔲 | Open |
| ✅ | Done |
| ❌ | Blocked |
Format: `🔲 [OWNER] Description`

---

## Sequential lane

> **Sequential hybrid ADB & Live OP13 Smoke** archived in COMPLETED_TASKS.md.

---

## Golden Path catch-up (named 1–8)

> **Golden Path 1–8 (including docs/spec.md & docs/plan.md)** archived in COMPLETED_TASKS.md.

---

## Ongoing Maintenance

> **Ongoing Maintenance and Feature Backlog (I-02 to I-80)** archived in `COMPLETED_TASKS.md` @ `2026-08-30`.

---

## Parallel lane

_(none — Sequential Golden Path rows first; `/scope` only after a feature’s public API is locked)_

---

## Human & device (after automation)

_(none — automated sign-off suite archived in COMPLETED_TASKS.md)_

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
| Human backlog automation | `.\scripts\automate-human-backlog.ps1` |
---

## Archive

| Sprint | Location |
|--------|----------|
| Ongoing Maintenance and Feature Backlog | `COMPLETED_TASKS.md` § Ongoing Maintenance and Feature Backlog (2026-08-30) |
| Golden Path catch-up and Sequential hybrid ADB | `COMPLETED_TASKS.md` § Golden Path catch-up and Sequential hybrid ADB |
| Donate + filename-version updates | `COMPLETED_TASKS.md` § Donate and filename-version updates |
| Bootstrap alignment 0.15.1 | `COMPLETED_TASKS.md` § Bootstrap alignment 0.11 → 0.15.1 |
| AUTO-SBOM | `COMPLETED_TASKS.md` § AUTO-SBOM |
| Audit R2 backlog (D1–D3) | `COMPLETED_TASKS.md` § Audit R2 Backlog D1–D3 |
| Audit R2 | `COMPLETED_TASKS.md` § Audit Sprint R2 |
| Import progress + ETA (F-002) | `COMPLETED_TASKS.md` § Import Progress + ETA |
| Human sign-off automation | `COMPLETED_TASKS.md` § Human Sign-off Automation |
| Backlog P1–P8 + v1.3.17 | `COMPLETED_TASKS.md` § Backlog Parallel Lane |
| Template migration v0.11.0 | `COMPLETED_TASKS.md` § Template Migration |
| Human verification automation | `COMPLETED_TASKS.md` § Human Verification Automation |
| Audit R1 | `COMPLETED_TASKS.md` § Audit Sprint R1 |
