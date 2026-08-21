# Build Plan

> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.
> Alignment record: `docs/BOOTSTRAP_ALIGNMENT.md`.

**Release:** v1.3.27 · **Template:** v0.16.0 · **Tests:** 252 (Release)

> **Bootstrap alignment 0.11 → 0.16.0** (process); 0.11 → 0.15.1 archive in COMPLETED_TASKS.md; 0.16.0 cherry-pick in `docs/BOOTSTRAP_ALIGNMENT.md`.
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

✅ `[AGENT]` FTP vault migrate on DHCP host change + reconnect/test success logs + listing KeepAlive=false
✅ `[AGENT]` Skip Android `.trashed-*` / trash dirs; restore FTP thumbnail limit defaults (48)
✅ `[AGENT]` Hybrid FTP browse / ADB pull import with media-root preflight + PreferAdb setting
✅ `[AGENT]` Seamless hybrid ADB scan+thumbs; HEIF normalize; 550 fail-fast; safe Magick/glitch reject; Unified scan dedupe
✅ `[AGENT]` PreferAdb polish: sibling FileExists gate; quiet 550 cache logs; ADB find+stat sizes; reconnect auto-select Unified
✅ `[AGENT]` Thumb limit on Unified/FTP; ADB vs FTP transport Info logs; cap thumb RETR parallelism=3; find+stat `|` fix
✅ `[AGENT]` Fix ADB dd quoting (sh -c) + media magic gate; no Magick on capped HEIC; Dispatcher group rebuild
✅ `[AGENT]` HEIC 12MB ADB pull + CompleteFile ADB; video full download; Unified sibling thumbs
🔲 `[HUMAN]` Live OP13 smoke: PreferAdb browse/previews/transfer (USB debugging)

---

## Parallel lane

_(none)_

---

## Human & device (after automation)

🔲 `[HUMAN]` WPF UI sign-off via `.\scripts\run-human-signoffs.ps1` when shipping product changes

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
