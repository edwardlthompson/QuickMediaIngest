# Build Plan

<!-- remaining-tally -->
**Remaining:** AGENT 0 · AUTO 1 · HUMAN 1 · ADB 0 · **2 open**
<!-- /remaining-tally -->
> Prioritized task board. Finished milestones and sprint detail live in `COMPLETED_TASKS.md`.
> Alignment record: `docs/BOOTSTRAP_ALIGNMENT.md`.

**Release:** v1.4.0 · **Template:** v1.5.0 · **Tests:** 360 (Release)

<!-- product-brief-sync:begin -->
> Read `AGENT.md` before any sprint row.

**One-liner:** Ingest photos and videos into dated, shoot-based folders
**Do not drift:** ingest, photos, videos, wpf, foss
<!-- product-brief-sync:end -->

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

### Template catch-up v1.0.0 → v1.5.0 (named 1–14)

Canon copy + Mixed merge. Sacred and the WPF app are never overwritten. Golden Path 15–19 wait for named numbers (not on this board).

1. ✅ [AGENT] T1 Fetch `edwardlthompson/agent-project-bootstrap` tag `v1.5.0`; snapshot child-only scripts; sacred denylist
2. ✅ [AGENT] T2 Canon 1: copy `.cursor/commands/` including `resume.md` with `docs/BATCH_COMMANDS.md` + `docs/help/BATCH_COMMANDS.md`
3. ✅ [AGENT] T3 Canon 2–3: copy `.cursor/rules/` including `product-brief.mdc` (keep `wpf-mvvm.mdc`); copy `docs/CURSOR_MODES.md` + `docs/help/`
4. ✅ [AGENT] T4 Canon 4–5: additive template scripts + example stubs; merge (do not replace) `feature-gate.sh`, `validate-bootstrap.sh`, `check-license-compliance.sh`, `watch-agent-gates.sh`; keep WPF 800/400/200
5. ✅ [AGENT] T5 Mixed 7–12: workflows additive only (no Pages/release-please; open-PR sync as workflow-example); union `.gitignore`; merge `bootstrap.config.json`, `TEMPLATE_INDEX.json` keys, `.env.example`, `PROJECT_CHECKLIST.md`
6. ✅ [AGENT] T6 Mixed 13: stamp `AGENT.md` + `BUILD_PLAN` `product-brief-sync` from `branding/product.json` (not the template About stub)
7. ✅ [AGENT] T7 Canon 6: `bash scripts/bootstrap-lifecycle.sh --sync-adapters`; empty diff on Sacred + `QuickMediaIngest/`
8. ✅ [AUTO] T8 `validate-bootstrap --quick` then stamp `.template-version` / `TEMPLATE_INDEX` / `.template-update.json` to `1.5.0`
9. ❌ [AUTO] T9 `feature-gate --stack dotnet-wpf` — environment block on this host (no .NET 8 SDK); Windows `dotnet` job in `ci.yml` unchanged
10. 🔲 [HUMAN] T10 Review additive gitleaks/semgrep if not made required (Trivy stays the Security Scan bar)

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
