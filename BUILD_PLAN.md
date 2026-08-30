# Build Plan

> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.
> Alignment record: `docs/BOOTSTRAP_ALIGNMENT.md`.

**Release:** v1.3.27 · **Template:** v1.0.0 · **Tests:** 252 (Release)

> **Bootstrap alignment 0.11 → 1.0.0** (process); 0.16.0 and earlier archives in COMPLETED_TASKS.md / `docs/BOOTSTRAP_ALIGNMENT.md`.
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

## Golden Path catch-up (named 1–8)

User named items **1–8** after the v1.0.0 template upgrade. These are **board rows only** — one `/feature` task later per AGENT feature. Port into existing QMI folders. **Never** copy `examples/` over `QuickMediaIngest/`. Thin wiring only (≤10 lines in `App.xaml.cs`). WPF limits: `.xaml` 800 · ViewModels 400 · `Core/` 200.

### 1 — About (`docs/features/donations-updates.md`)

Already shipped (Venmo + filename-version updates). Align leftover Golden Path About patterns only.

🔲 `[AGENT]` `/feature` GP-1: gap existing About overlay vs spec; port into current About folders + `QuickMediaIngest.Tests`; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke About donate / Check now after align

### 2 — Crash capture (`docs/features/crash-capture.md`)

Opt-in local crash queue; never auto-send. Catalog stacks exclude WPF — port patterns into a QMI feature folder.

🔲 `[AGENT]` `/feature` GP-2: WPF crash-capture vertical slice from spec (opt-in, sanitize-before-persist, at-most-one); tests; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke: setting off = no persist; setting on = one sanitized record

### 3 — Settings (`docs/features/settings.md`)

Settings overlay already exists. Align theme persist + optional save-crash toggle vs spec.

🔲 `[AGENT]` `/feature` GP-3: gap existing Preferences/Settings vs spec; port into current Settings folders; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke theme persist + crash-save toggle (after GP-2)

### 4 — Feedback (`docs/features/feedback.md`)

About **Report a bug** currently opens the GitHub issues URL only. Need review dialogs (escaped preview, Copy, Open GitHub, Discard).

🔲 `[AGENT]` `/feature` GP-4: WPF feedback dialogs from spec; wire from existing About; tests; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke Report a bug / Request a feature from About

### 5 — GitHub issue composer (`docs/features/github-feedback.md`)

Compose issue-form URLs + clipboard fallback + fail-soft search. Logic-only container.

🔲 `[AGENT]` `/feature` GP-5: WPF/Core github-feedback composer from spec; tests; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke Open GitHub uses `https` only; offline Copy still works

### 6 — Privacy sanitizer (`docs/features/privacy-report.md`)

Shared sanitize / fingerprint / markdown. No UI, no network. Run before persist and before Copy / Open GitHub.

🔲 `[AGENT]` `/feature` GP-6: Core privacy-report sanitizer from spec + unit tests; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Confirm crash/feedback text never keeps tokens/home paths

### 7 — Display refresh (`docs/features/display-refresh.md`)

Android-only catalog slice. Optional WPF port: request highest same-resolution refresh for About/Settings scroll when the OS allows it.

🔲 `[AGENT]` `/feature` GP-7: WPF display-refresh port from spec (reference Android stub only); tests or documented fallback; no `examples/` copy
🔲 `[AUTO]` `feature-gate.sh --stack dotnet-wpf`
🔲 `[HUMAN]` Smoke About/Settings scroll on a high-refresh display

### 8 — Sacred product spec / plan

🔲 `[HUMAN]` Author `docs/spec.md` and `docs/plan.md` for Quick Media Ingest. Sacred — do **not** paste the template stub. Agent must not create or refresh these files.

---

## Parallel lane

_(none — Sequential Golden Path rows first; `/scope` only after a feature’s public API is locked)_

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
