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

`/build 1-8` implemented AGENT slices in existing QMI folders. **Never** copy `examples/` over `QuickMediaIngest/`. Thin wiring only. WPF limits: `.xaml` 800 · ViewModels 400 · `Core/` 200.

AUTO `feature-gate.sh --stack dotnet-wpf` stays open: this Linux agent has no .NET 8 SDK / cannot compile `net8.0-windows`. HUMAN smoke + Sacred spec stay open (`HUMAN_BACKLOG.md`).

### 1 — About (`docs/features/donations-updates.md`)

Already shipped (Venmo + filename-version updates). Added Request a feature + feedback entry from About.

- ✅ [AGENT] /feature GP-1: gap existing About overlay vs spec; port into current About folders + QuickMediaIngest.Tests; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke About donate / Check now after align

### 2 — Crash capture (`docs/features/crash-capture.md`)

Opt-in local crash queue; never auto-send. Catalog stacks exclude WPF — port patterns into a QMI feature folder.

- ✅ [AGENT] /feature GP-2: WPF crash-capture vertical slice from spec (opt-in, sanitize-before-persist, at-most-one); tests; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke: setting off = no persist; setting on = one sanitized record

### 3 — Settings (`docs/features/settings.md`)

Settings overlay already exists. Align theme persist + optional save-crash toggle vs spec.

- ✅ [AGENT] /feature GP-3: gap existing Preferences/Settings vs spec; port into current Settings folders; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke theme persist + crash-save toggle (after GP-2)

### 4 — Feedback (`docs/features/feedback.md`)

About **Report a bug** currently opens the GitHub issues URL only. Need review dialogs (escaped preview, Copy, Open GitHub, Discard).

- ✅ [AGENT] /feature GP-4: WPF feedback dialogs from spec; wire from existing About; tests; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke Report a bug / Request a feature from About

### 5 — GitHub issue composer (`docs/features/github-feedback.md`)

Compose issue-form URLs + clipboard fallback + fail-soft search. Logic-only container.

- ✅ [AGENT] /feature GP-5: WPF/Core github-feedback composer from spec; tests; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke Open GitHub uses https only; offline Copy still works

### 6 — Privacy sanitizer (`docs/features/privacy-report.md`)

Shared sanitize / fingerprint / markdown. No UI, no network. Run before persist and before Copy / Open GitHub.

- ✅ [AGENT] /feature GP-6: Core privacy-report sanitizer from spec + unit tests; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Confirm crash/feedback text never keeps tokens/home paths

### 7 — Display refresh (`docs/features/display-refresh.md`)

Android-only catalog slice. Optional WPF port: request highest same-resolution refresh for About/Settings scroll when the OS allows it.

- ✅ [AGENT] /feature GP-7: WPF display-refresh port from spec (reference Android stub only); tests or documented fallback; no examples/ copy
- 🔲 [AUTO] feature-gate.sh --stack dotnet-wpf
- 🔲 [HUMAN] Smoke About/Settings scroll on a high-refresh display

### 8 — Sacred product spec / plan

- 🔲 [HUMAN] Author docs/spec.md and docs/plan.md for Quick Media Ingest. Sacred — do not paste the template stub. Agent must not create or refresh these files.

---

## Ongoing Maintenance

Rows from `/allideas` (2026-08-30). One `[AGENT]` row per idea. Skipped leftovers already on this board (OP13 smoke, GP AUTO/HUMAN, GP-8 Sacred spec, WPF sign-off). Do not copy `examples/`. Do not create `docs/spec.md` or `docs/plan.md`.

### Security and privacy

- 🔲 [AGENT] /feature I-01: bump Magick.NET past current GHSA set; tests; no examples/ copy
- 🔲 [AGENT] /feature I-02: LibRaw/libvips-first decode path for common RAW; tests; no examples/ copy
- 🔲 [AGENT] /feature I-03: GPS/PII strip option on embed-keywords / copy; tests; no examples/ copy
- 🔲 [AGENT] /feature I-04: settings JSON export/import matching PRIVACY.md; schema version; tests; no examples/ copy
- 🔲 [AGENT] /feature I-05: config schema migration on version bump; tests; no examples/ copy
- 🔲 [AGENT] /feature I-06: Discard Feedback also clears clipboard we wrote; tests; no examples/ copy
- 🔲 [AGENT] /feature I-07: sanitize import-report paths with privacy-report sanitizer; tests; no examples/ copy
- 🔲 [AGENT] /feature I-08: optional destination encryption hint (BitLocker/VeraCrypt detect only); tests; no examples/ copy
- 🔲 [AGENT] /feature I-09: Scorecard workflow green or documented exception in DECISION_LOG
- 🔲 [AGENT] /feature I-10: Dependabot Magick/NuGet weekly apply ADR (what we will not bump blindly)

### Ingest workflow

- 🔲 [AGENT] /feature I-11: safely eject / dismount source after verified import; tests; no examples/ copy
- 🔲 [AGENT] /feature I-12: dry-run import (plan only, no copy); tests; no examples/ copy
- 🔲 [AGENT] /feature I-13: pick / reject cull on the review grid before Import; tests; no examples/ copy
- 🔲 [AGENT] /feature I-14: star ratings + color labels persisted in import report; tests; no examples/ copy
- 🔲 [AGENT] /feature I-15: per-source naming template; tests; no examples/ copy
- 🔲 [AGENT] /feature I-16: copyright / creator IPTC-IIM or XMP stamp on import; tests; no examples/ copy
- 🔲 [AGENT] /feature I-17: XMP sidecar write (do not mutate RAW); tests; no examples/ copy
- 🔲 [AGENT] /feature I-18: destination free-space forecast from selected bytes; tests; no examples/ copy
- 🔲 [AGENT] /feature I-19: collision report for Skip / Suffix / Overwrite before copy; tests; no examples/ copy
- 🔲 [AGENT] /feature I-20: resume-pending-plan visible banner + one-click resume; tests; no examples/ copy
- 🔲 [AGENT] /feature I-21: import history search / filter / export CSV; tests; no examples/ copy
- 🔲 [AGENT] /feature I-22: SHA-256 checksum manifest next to destination shoot; tests; no examples/ copy
- 🔲 [AGENT] /feature I-23: 3-2-1 second destination (copy to two roots); tests; no examples/ copy
- 🔲 [AGENT] /feature I-24: watch-folder auto-scan (opt-in, local only); tests; no examples/ copy
- 🔲 [AGENT] /feature I-25: eject reminder if delete-after-import left files; tests; no examples/ copy
- 🔲 [AGENT] /feature I-26: milliseconds in naming live preview for all source types; tests; no examples/ copy
- 🔲 [AGENT] /feature I-27: shoot-title batch rename with uniqueness check; tests; no examples/ copy
- 🔲 [AGENT] /feature I-28: skip already-imported via local hash catalog across destinations; tests; no examples/ copy

### Sources and devices

- 🔲 [AGENT] /feature I-29: MTP / WPD phone import without ADB; tests; no examples/ copy
- 🔲 [AGENT] /feature I-30: iPhone USB (Windows portable-device) scan; tests; no examples/ copy
- 🔲 [AGENT] /feature I-31: camera Wi-Fi profile presets (Sony / Canon / Nikon folder maps); tests; no examples/ copy
- 🔲 [AGENT] /feature I-32: PTP / USB tether browse (FOSS libusb, no vendor SDK); tests; no examples/ copy
- 🔲 [AGENT] /feature I-33: PreferAdb dual-FTP alias de-dupe; tests; no examples/ copy
- 🔲 [AGENT] /feature I-34: FTP bandwidth / connection cap slider; tests; no examples/ copy
- 🔲 [AGENT] /feature I-35: offline mock FTP + mock removable volume for UI tests; tests; no examples/ copy
- 🔲 [AGENT] /feature I-36: DeviceWatcher live tests behind an opt-in flag
- 🔲 [AGENT] /feature I-37: show transport (ADB vs FTP vs local) on each shoot row; tests; no examples/ copy
- 🔲 [AGENT] /feature I-38: removable-drive throttle QA checklist + automated timing harness

### Date, time, and folders

- 🔲 [AGENT] /feature I-39: DateTaken timezone override (camera vs local vs UTC); tests; no examples/ copy
- 🔲 [AGENT] /feature I-40: manual shoot-split / merge after hours-slider; tests; no examples/ copy
- 🔲 [AGENT] /feature I-41: destination folder template tokens (job / client / camera); tests; no examples/ copy
- 🔲 [AGENT] /feature I-42: same card new-day rescan keeps prior cull/selection; tests; no examples/ copy

### Previews and codecs

- 🔲 [AGENT] /feature I-43: HEIC/HEIF decode without Magick when libvips/WIC can; tests; no examples/ copy
- 🔲 [AGENT] /feature I-44: video proxy or first-frame-only mode for huge MP4s; tests; no examples/ copy
- 🔲 [AGENT] /feature I-45: optional ffmpeg transcode-on-import (FOSS, off by default); tests; no examples/ copy
- 🔲 [AGENT] /feature I-46: compare two files side-by-side beyond RAW+JPEG stack; tests; no examples/ copy
- 🔲 [AGENT] /feature I-47: preview cache cap + purge in Preferences; tests; no examples/ copy
- 🔲 [AGENT] /feature I-48: color-managed preview (system ICC, no extra CMS SDK); tests; no examples/ copy

### Feedback and Golden Path leftovers

- 🔲 [AGENT] /feature I-49: GitHub duplicate search with 60s cooldown (real API, fail-soft); tests; no examples/ copy
- 🔲 [AGENT] /feature I-50: follow Windows system theme (light / dark / system); tests; no examples/ copy
- 🔲 [AGENT] /feature I-51: high-contrast / reduced-motion honor in overlays; tests; no examples/ copy
- 🔲 [AGENT] /feature I-52: keyboard map for Feedback (Esc, Ctrl+C, Ctrl+Enter opens GitHub); tests; no examples/ copy
- 🔲 [AGENT] /feature I-53: remove dead ReportBug_Click URL opener
- 🔲 [AGENT] /feature I-54: pending-crash review must not re-offer a Discarded fingerprint; tests; no examples/ copy
- 🔲 [AGENT] /feature I-55: add GitHub issue-form templates bug.yml and feature.yml

### UI, a11y, i18n

- 🔲 [AGENT] /feature I-56: THEME_QA pass on Feedback and crash-review overlays
- 🔲 [AGENT] /feature I-57: screen-reader pass: overlays announce title and live preview; tests; no examples/ copy
- 🔲 [AGENT] /feature I-58: German and optional Japanese .resx; no examples/ copy
- 🔲 [AGENT] /feature I-59: language change without full restart; tests; no examples/ copy
- 🔲 [AGENT] /feature I-60: high-DPI 150/200 layout pass on About diagnostics WrapPanel
- 🔲 [AGENT] /feature I-61: F1 shortcuts page lists Feedback / crash / eject commands

### Architecture and quality

- 🔲 [AGENT] /feature I-62: extract QuickMediaIngest.Core csproj; tests; no examples/ copy
- 🔲 [AGENT] /feature I-63: split remaining 400-line ViewModel partials (UiState at cap)
- 🔲 [AGENT] /feature I-64: headless --smoke-libvips plus Core unit job on Linux CI
- 🔲 [AGENT] /feature I-65: make Golden Path / Sequential HUMAN rows parseable by build-sprint-status
- 🔲 [AGENT] /feature I-66: README process target 0.15.1 to 1.0.0
- 🔲 [AGENT] /feature I-67: write docs/FIRST_30_DAYS.md child week-1 playbook (not the template stub; not spec.md)
- 🔲 [AGENT] /feature I-68: Feedback overlay ViewModel tests (preview enablement, https-only)
- 🔲 [AGENT] /feature I-69: BUILD_PLAN test count + CHANGELOG Unreleased for GP 1-7
- 🔲 [AGENT] /feature I-70: workflow_dispatch always uploads versioned EXE/MSI names
- 🔲 [AGENT] /feature I-71: portable EXE cold-start prefs smoke in CI
- 🔲 [AGENT] /feature I-72: structured JSON log option (local file, no network); tests; no examples/ copy

### Distribution and docs

- 🔲 [AGENT] /feature I-73: WiX repair/upgrade does not clobber AppData config; tests; no examples/ copy
- 🔲 [AGENT] /feature I-74: About shows channel (portable vs MSI) and update asset that would apply; tests; no examples/ copy
- 🔲 [AGENT] /feature I-75: add HUMAN checklist for authoring spec.md/plan.md; do not create those files
- 🔲 [AGENT] /feature I-76: Runbook PreferAdb + OP13 + delete-after-import failure modes
- 🔲 [AGENT] /feature I-77: THIRD_PARTY_LICENSES refresh after Magick/NetVips bumps
- 🔲 [AGENT] /feature I-78: optional in-app What's new from local CHANGELOG only; tests; no examples/ copy
- 🔲 [AGENT] /feature I-79: keyboard-only first-run onboarding (no mouse); tests; no examples/ copy
- 🔲 [AGENT] /feature I-80: per-shoot keyword autocomplete from last 50 imports (local only); tests; no examples/ copy

---

## Parallel lane

_(none — Sequential Golden Path rows first; `/scope` only after a feature’s public API is locked)_

---

## Human & device (after automation)

- 🔲 [HUMAN] WPF UI sign-off via scripts/run-human-signoffs.ps1 when shipping product changes

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
