# Build Plan

> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.

**Release:** v1.3.17 · **Template:** v0.11.0 · **Tests:** 109 (Release)

**Active lane:** Shipped — run `.\scripts\run-human-signoffs.ps1` for release verification

---

## Legend

| Label | Owner |
|-------|-------|
| `AGENT` | Cursor Agent — code, docs, tests, CI |
| `HUMAN` | Human — interactive OAuth, optional live UI glance, push approval |
| `ADB` | Human — Android device/emulator testing |
| `AUTO` | CI, Dependabot, gate scripts |

Icons: ✅ done/automated · 🔲 open · ⚠️ needs interactive step once

---

## Automated human sign-offs

One command replaces the old HUMAN checklist (config, UI bindings, FTP smoke, security triage):

```powershell
.\scripts\run-human-signoffs.ps1              # full automated pass
.\scripts\run-human-signoffs.ps1 -Strict      # fail if Dependabot scope missing
.\scripts\run-human-signoffs.ps1 -RefreshGh   # try gh auth refresh (browser)
.\scripts\run-human-signoffs.ps1 -PublishedExe  # include portable exe libvips smoke
$env:QMI_ALLOW_PUSH='1'; .\scripts\run-human-signoffs.ps1 -Push -WaitCi 300  # after commit
```

| Former HUMAN item | Automation |
|-------------------|------------|
| Delete After Import UI | `HumanSignoffVerificationTests` + `DeleteAfterImportConfirmHelperTests` |
| Thumbnail slider at 200 | `HumanSignoffVerificationTests` (binding + config reload) |
| Weekly CVE triage | `check-security-triage.sh` via sign-off script |
| `gh security_events` scope | `ensure-gh-security-scope.sh` (`-RefreshGh` when interactive) |
| CI green after push | `--Push -WaitCi` when `QMI_ALLOW_PUSH=1` |

### Still interactive (by design)

- ✅ [AGENT] F-001 — Backlog P1–P8 + human sign-off automation (this release)
- ⚠️ [HUMAN] `gh auth refresh -s security_events` — only when `-RefreshGh` / first-time scope (browser OAuth); CI uses `GITHUB_TOKEN`
- ⚠️ [HUMAN] Optional live UI glance — headless tests cover bindings; not required for release

---

## Smoke & gates (quick reference)

| Check | Command |
|-------|---------|
| **All human sign-offs** | `.\scripts\run-human-signoffs.ps1` |
| Config + optional LAN FTP only | `.\scripts\smoke-human-verification.ps1` |
| Published exe libvips | `.\scripts\smoke-published-exe.ps1 -Rebuild` |
| README health | `bash scripts/check-readme-health.sh` |
| WPF feature gates | `bash scripts/feature-gate.sh --stack dotnet-wpf` |
| Pre-release | `bash scripts/pre-release-gate.sh` |

---

## Release gates

| Gate | Owner | Status |
|------|-------|--------|
| File line limits (no grandfather) | AUTO | ✅ |
| Human sign-off automation | AUTO | ✅ `run-human-signoffs` |
| Unit test baseline | AUTO | ✅ |
| Overlay MVVM decouple | AUTO | ✅ |
| CI + headless `--smoke-libvips` | AUTO | ✅ |
| MSI admin-extract validation | AUTO | ✅ |
| Dependabot strict (local) | HUMAN | ⚠️ `-RefreshGh` once, then AUTO |
| Optional live UI glance | HUMAN | Optional |

---

## Archive

| Sprint | Location |
|--------|----------|
| Template migration v0.11.0 | `COMPLETED_TASKS.md` § Template Migration |
| Human verification automation | `COMPLETED_TASKS.md` § Human Verification Automation |
| Backlog P1–P8 | `COMPLETED_TASKS.md` § Backlog P1–P8 |
| Audit R1 | `COMPLETED_TASKS.md` § Audit Sprint R1 |
