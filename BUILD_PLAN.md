# Build Plan

<!-- remaining-tally -->
**Remaining:** AGENT 0 · AUTO 0 · HUMAN 0 · ADB 0 · **0 open**
<!-- /remaining-tally -->
> Open work only. Finished sprints: [COMPLETED_TASKS.md](COMPLETED_TASKS.md). Alignment: `docs/BOOTSTRAP_ALIGNMENT.md`.

**Release:** v1.5.0 · **Template:** v1.5.0 · **Tests:** 360 (Release)

<!-- product-brief-sync:begin -->
> Read `AGENT.md` before any sprint row.

**One-liner:** Ingest photos and videos into dated, shoot-based folders
**Do not drift:** ingest, photos, videos, wpf, foss
<!-- product-brief-sync:end -->

## Legend

| Label | Owner |
|-------|-------|
| `[AGENT]` | Cursor Agent — code, docs, tests, CI |
| `[HUMAN]` | Human — OAuth, optional UI glance, push approval, deferred CI policy |
| `[ADB]` | Human — Android device/emulator testing (N/A for this product) |
| `[AUTO]` | CI, Dependabot, gate scripts |
Status: 🔲 open · ✅ done · ❌ blocked — format `🔲 [OWNER] Description`. Done rows leave this file for `COMPLETED_TASKS.md`.

## Sequential lane

Avalonia is a second **view**, not a fork. Do not compile `MainViewModel` into Desktop. Keep `.cursor/stack-selection.json` as `dotnet-wpf`. Specs: `docs/features/ingest-chrome.md`, `docs/features/linux-parity.md`.

_(none open — history in [COMPLETED_TASKS.md](COMPLETED_TASKS.md))_

## Ongoing Maintenance

_(none open — history in [COMPLETED_TASKS.md](COMPLETED_TASKS.md))_

## Parallel lane

_(none — Sequential lock first; `/scope` only after a feature’s public API is locked)_

## Human & device (after automation)

_(none — history in [COMPLETED_TASKS.md](COMPLETED_TASKS.md))_

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
## Archive

Finished work lives in [COMPLETED_TASKS.md](COMPLETED_TASKS.md). Do not keep completed sprints or ✅ rows on this board.

## Open PRs

<!-- open-prs-sync:begin -->
_No open Dependabot or Release Please PRs._
<!-- open-prs-sync:end -->
