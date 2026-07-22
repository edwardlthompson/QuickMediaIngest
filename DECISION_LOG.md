# Decision Log

> Append-only register of major technical trade-offs. Past entries are immutable.

## 2026-07-22 — Automate Align-0.15 HUMAN_BACKLOG (keep release-please/pages off)

**Status:** Accepted  
**Context:** HUMAN_BACKLOG deferred enabling several upstream workflows after template alignment.

**Decision:**
- Enable `stale.yml`, WPF-adapted `weekly-health-check.yml`, and `dependabot-automerge.yml`.
- Permanently decline `release-please*` and `pages.yml` (csproj + `build.yml` owns release).
- Add `scripts/automate-human-backlog.{sh,ps1}`, `scripts/lib/resolve-gh.sh`, and wire `AUTOMERGE_TOKEN` setup.
- Leave interactive `gh auth refresh -s security_events` as the only Align-0.15 human auth step.

**Validation:** `.\scripts\automate-human-backlog.ps1 -SetupAutomergeToken` (secret set); workflows present; `HUMAN_BACKLOG.md` regenerated.

---

## 2026-07-21 — Align process tooling with agent-project-bootstrap v0.15.1

**Status:** Accepted  
**Context:** Repo was on template `0.11.0`; upstream tip is `0.15.1` (Cursor FOSS pack, parallel BUILD_PLAN, expanded scripts/CI). This is a live WPF product, not a fresh bootstrap.

**Decision:**
- Cherry-pick agent surface, Cursor FOSS pack, allowlisted scripts, and BUILD_PLAN markers to process level `0.15.1`.
- Preserve `modules/dotnet-wpf`, WPF file limits (800/400/200), and csproj-driven `build.yml` release.
- **Hard defer** new workflows: release-please, pages, dependabot-automerge, stale, weekly-health-check → `HUMAN_BACKLOG.md`.
- Merge (never overwrite) `validate-bootstrap.sh` / `TEMPLATE_INDEX.json`; bump `.template-version` only after local gates pass.
- Hooks go live only after dry-run of `validate-local.ps1 -QuickBootstrap` and `feature-gate.sh --stack dotnet-wpf`.

**Alternatives rejected:** Full template replace; enabling release-please; adopting upstream 300/150 file caps; copying inactive modules/examples.

**Validation:** `docs/BOOTSTRAP_ALIGNMENT.md`; encoding/hygiene/validate-bootstrap/batch-commands/validate-local; `dotnet test`.

**See:** `docs/BOOTSTRAP_ALIGNMENT.md`

---

## 2026-06-13 — SQLite provider: retain System.Data.SQLite.Core

**Decision:** Keep `System.Data.SQLite.Core` (1.0.119); do not migrate to `Microsoft.Data.Sqlite` in this release cycle.

**Rationale:** `QuickMediaIngest.csproj` documents that SQLite native interop relies on `Assembly.Location` for path resolution; single-file publish with `IncludeAllContentForSelfExtract` is validated against `System.Data.SQLite.Core`. `Microsoft.Data.Sqlite` uses a different native bundling model and would require publish-path regression testing.

**Alternatives rejected:** Immediate migration to `Microsoft.Data.Sqlite` without MSI/single-file validation.

**Validation:** `DatabaseServiceTests`, `dotnet test` (53 passed).

---

## 2026-06-13 — Dependabot PR #4 fully merged (MaterialDesign 5.x + Extensions 10.x)

**Decision:** Apply remaining Dependabot PR #4 bumps: MaterialDesignThemes 5.3.2, Microsoft.Extensions 10.0.9, System.Management 10.0.9, test SDK/xunit/Moq updates.

**Migration:** `Theme.Light`/`Theme.Dark` → `BaseTheme.Light`/`BaseTheme.Dark`; `MaterialDesignTheme.Defaults.xaml` → `MaterialDesign2.Defaults.xaml` (preserves MD2 control styles used by custom chrome).

**Validation:** `dotnet build`, `dotnet test` (53 passed), app startup smoke test.

---

## 2026-06-13 — Dependabot PR #4 partial merge

**Decision:** Close Dependabot PR #4; apply non-breaking bumps on main (FluentFTP 54.2.0, Meziantou 2.0.0, MetadataExtractor 2.9.3, SQLite 1.0.119). Defer MaterialDesignThemes 5.x and Microsoft.Extensions 10.x.

**Rationale:** Full PR failed CI (breaking API/theme changes). Safe subset clears dependency drift without MaterialDesign 5.x migration scope.

**Validation:** `dotnet restore`, `dotnet build`, `dotnet test`.

---

## 2026-06-13 — Persistence strategy B (JSON + VACUUM-only SQLite)

**Decision:** Keep JSON files for config, import history, and presets; slim `IDatabaseService` to `TryPeriodicVacuum()` only; remove unused SQLite CRUD APIs and dead DI (`IMetadataReader`, `IWhitelistFilter`).

**Rationale:** App already persists all user-facing state via JSON under `%AppData%\QuickMediaIngest\`. SQLite tables were unused split-brain. VACUUM retains occasional DB file maintenance without migration cost.

**Alternatives rejected:** (A) migrate config/history to SQLite — higher risk, no user benefit today.

**Validation:** `dotnet build`, `dotnet test`; `DatabaseService` no longer exposes CRUD.

---

## 2026-06-13 — Sprint 1 closed; file limits enforced

**Decision:** Close Sprint 1 file size remediation; archive to `COMPLETED_TASKS.md`; defer Sidebar/Import UserControl extraction (shell XAML under 800-line limit).

**Validation:** `scripts/check-file-limits.sh` (empty grandfather list), `dotnet test` (13 passed).

---

## 2026-06-13 — Sprint 0 closed; GitHub settings applied

**Decision:** Close Sprint 0 bootstrap parity; archive tasks to `COMPLETED_TASKS.md`; active work moves to Sprint 1 (file size remediation).

**GitHub settings applied via `gh`:** Dependabot security updates enabled, private vulnerability reporting enabled, branch protection on `main` (requires CI status checks), repo About description and topics updated.

**Pending:** Push bootstrap + Sprint 1 to `main`; confirm new `ci.yml` / `codeql.yml` / `security.yml` workflows green.

---

## 2026-06-13 — Magick.NET 14.14.0

**Decision:** Bump `Magick.NET-Q16-AnyCPU` from 14.13.0 to 14.14.0.

**Rationale:** Clears NU1902/NU1903 vulnerability advisories on restore/build without API changes; 13 tests pass.

**Validation:** `dotnet build`, `dotnet test` (Release).

---

**Decision:** Adopt [agent-project-bootstrap](https://github.com/edwardlthompson/agent-project-bootstrap) scaffolding without copying web/python/android Golden Path examples.

**Rationale:** QuickMediaIngest is a mature .NET 8 WPF app; Reference mode preserves existing architecture while adding agent routing, CI guardrails, and workspace memory.

**Alternatives rejected:** Full greenfield re-scaffold; copying unrelated stack examples.

**Validation:** `scripts/validate-bootstrap.sh`, `ci.yml` dotnet test job.

---

## 2026-06-13 — ADR-0001: MVVM + provider architecture (existing)

**Decision:** MVVM with CommunityToolkit.Mvvm partials; `IFileProvider` abstraction for Local/FTP/ADB sources; Core logic in `QuickMediaIngest/Core/` without WPF dependencies.

**Rationale:** Testability, extensibility for new source types, separation of UI from ingest pipeline.

**See also:** `docs/adr/0001-core-architecture.md`

---

## 2026-06-20 — Template Migration Sprint Phase 1 (bootstrap v0.11.0 alignment)

**Decision:** Migrate QuickMediaIngest from template **v0.2.0** to **v0.11.0** in Reference mode. Phase 1 establishes foundation only: `.cursorignore`, `init-project.sh` (with `--reference` + `dotnet-wpf` stack), `init-stack-sync.py`, `sync-stack-config.py`, and `.cursor/stack-selection.json`.

**Slash commands & rules strategy:**
- Additive migration — no legacy slash commands existed; copy all 25 `.cursor/commands/*.md` + `batch-commands.mdc` / `cursor-modes.mdc` in Phase 2.
- Preserve repo-specific rules: `wpf-mvvm.mdc`, `foss-compliance.mdc`, `read-before-write.mdc`.
- Do not blind-overwrite `INITIALIZATION_PROMPT.md`, `AGENT_MEMORY.md`, `modules/dotnet-wpf/MODULE.md`.

**WPF gate extension strategy (Phase 3):**
- Extend upstream `feature-gate.sh` with `dotnet-wpf` stack: `dotnet restore/build/test`, `dotnet format --verify-no-changes`, `check-file-limits.sh`, `check-license-compliance.sh`.
- Make `validate-bootstrap.sh` web-artifact requirements conditional on stack ≠ `dotnet-wpf`.

**Alternatives rejected:** Full greenfield re-scaffold; copying `examples/web` and unrelated Golden Path stacks.

**Validation:** `bash scripts/init-project.sh --stack dotnet-wpf --reference --no-prune --non-interactive`; `scripts/validate-bootstrap.sh` (Phase 5).

---

## 2026-06-20 — Critique mitigations (template migration)

**Decision:** Address all six BUILD_PLAN critique items before Phase 4 docs/index work.

**Changes:**
- `feature-gate.sh` — `dotnet-wpf` stack reads `stack-selection.json`; runs dotnet restore/build/test/format + license gate
- `watch-agent-gates.sh` / `feature-autofix.sh` — WPF scope paths; exit 2 = halt (3-strike)
- `validate-bootstrap.sh` — stack-conditional web artifacts; `.cursorignore` block check
- Slash commands `gates.md`, `ci.md`, `feature.md`, `prerelease.md` — WPF paths + `[HUMAN]` offline CI fallback
- `INITIALIZATION_PROMPT.md` — merged §6–8 (CURSOR_MODES, watch-agent-gates, 7a/7b) without overwriting §1 project dimensions
- `.cursorignore` — verified rules/commands not blocked

**Validation:** `dotnet test -c Release`; `check-batch-commands.sh` when bash available.

---

## 2026-06-20 — Phase 4: index, docs, template version bump

**Decision:** Complete template migration index and documentation sync; bump pinned template version from `0.2.0` to `0.11.0`.

**Changes:**
- `TEMPLATE_INDEX.json` — added batch-command rules, gate scripts, CURSOR_MODES/BATCH_COMMANDS docs, stack-selection, ephemeral templates; `template_version` → `0.11.0`
- `.template-version` → `0.11.0`
- Read-order updates: `AGENTS.md`, `docs/START_HERE.md`, `docs/FOR_AGENTS.md`, `PROMPT_LIBRARY.md`, `README.md` — CURSOR_MODES, slash commands, watch-agent-gates
- `AGENT_MEMORY.md` — milestone retrospective for v0.11.0 migration

**Validation:** `bash scripts/validate-template-index.sh`; Phase 5 gate suite pending.

---

## 2026-06-20 — Phase 5: gate suite & `/build` super-command smoke

**Decision:** Mark Phase 5 AGENT gate work complete; defer Dependabot strict check and HUMAN sign-off items.

**`/build` super-command smoke (this session):**

| Step | Command | Result |
|------|---------|--------|
| 1 | `plan.md` — Phase 5 validation scope | Pass (trivial rubric; no code edits) |
| 3 | `feature.md` — Phase 5 AGENT rows | Pass (gates + docs only) |
| 4 | `gates.md` — local validation | See gate table below |

**Gate suite results:**

| Script | Result | Notes |
|--------|--------|-------|
| `validate-bootstrap.sh` | ✅ Pass | Full run; stack=dotnet-wpf |
| `check-batch-commands.sh` | ✅ Pass | 25 files (20 atomic + 5 super) |
| `feature-gate.sh --stack dotnet-wpf` | ✅ Pass | 9 stages |
| `watch-agent-gates.sh --once --autofix` | ✅ Pass | 9 stages |
| `validate-local.ps1 -QuickBootstrap -SkipBuild` | ✅ Pass | All local gates |
| `pre-release-gate.sh` | ⚠️ Partial | feature-gate ✅; CI/CodeQL ✅; **Dependabot strict FAIL** — `gh` lacks `security_events` scope locally |
| `dotnet test -c Release` | ✅ Pass | 91 passed, 5 skipped (prior session) |

**Pending [HUMAN]:** Confirm `/bootstrap`, `/build`, `/verify`, `/ship`, `/gates`, `/audit` in Cursor `/` menu; sign off template v0.11.0 bump; run `gh auth refresh -s security_events` or manual CVE triage per `docs/SECURITY_TRIAGE.md`.

**Pending [AUTO]:** Push branch; CI green on all new gates.

---

## 2026-06-20 — Phase 5 HUMAN sign-off (template v0.11.0)

**Decision:** Human confirmed slash commands in Cursor `/` menu and approved template version bump to **0.11.0**.

**Sign-off:** `/bootstrap`, `/build`, `/verify`, `/ship`, `/gates`, `/audit` verified; `.template-version` and `TEMPLATE_INDEX.json` at `0.11.0` approved for release.

**Remaining:** `[AUTO]` push + CI green; optional `gh auth refresh -s security_events` for local Dependabot strict gate; FTP thumbnail HUMAN smoke on LAN test source.

---

## 2026-06-20 — Human verification automation

**Decision:** Automate BUILD_PLAN HUMAN verification rows where possible; keep UI-only checks as optional spot-checks.

**Changes:**
- `LanFtpSmokeProbe` — env-configurable LAN FTP probe (`QMI_SMOKE_FTP_*`, `QMI_SMOKE_REQUIRE=1`)
- `ConfigFilePersistenceTests`, `HumanVerificationSmokeTests` — config round-trip + FTP tier/cache/Ultra smokes
- Existing `FtpThumbnail*Tests` — removed hard `Skip`; auto-run when LAN FTP reachable
- `scripts/smoke-human-verification.ps1` / `.sh` — orchestrates smoke tests + optional security triage
- `validate-local.ps1 -SmokeHuman` — gate integration
- `.gitignore` — `TestResults/`

**Remaining [HUMAN]:** Delete-after-import dialog on restart (UI); thumbnail slider visual; libvips in published portable exe.

**Validation:** `dotnet test` 101 passed; `smoke-human-verification` filter 10/10 when LAN FTP up.

---

## 2026-06-21 — Release v1.3.17 (P1–P8 + human sign-off automation)

**Decision:** Ship v1.3.17 with backlog P2–P8 complete, automated human sign-offs (`run-human-signoffs`), and CI hardening fixes discovered during `/ship`.

**Changes:**
- Human sign-offs job: removed Dependabot API step (GITHUB_TOKEN unreliable on Windows runners; dotnet `--vulnerable` + Security Scan cover release gate)
- MSI validation: fixed `msiexec /a` argument quoting and Process-based libvips smoke exit code
- libvips publish smoke: removed `--no-build` to restore win-x64 RID assets

**Release:** [v1.3.17](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.17) — portable EXE, ZIP, MSI uploaded via `workflow_dispatch`.

**Validation:** CI + Security Scan + CodeQL green on `5aa8a14`; 109 tests; zero open Critical/High Dependabot alerts.

**Deferred:** CycloneDX SBOM attachment to releases (documented in INITIALIZATION_PROMPT, not yet in `build.yml`).

---

## 2026-06-21 — Release v1.3.18 (import progress + ETA)

**Decision:** Ship F-002 — byte-weighted import progress and ETA with parallel copies unchanged.

**Changes:**
- `ImportByteProgressTracker` + per-provider copy progress callbacks
- Shared `MainViewModel.ImportProgress.partial.cs` for bar, ETA, and status across import paths

**Release:** [v1.3.18](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.18) — portable EXE, ZIP, MSI via `workflow_dispatch`.

**Validation:** CI + Security Scan + CodeQL green on `0221d2c`; 113 tests; zero open Critical/High Dependabot alerts.

---

## 2026-07-12 — Audit Sprint R2 (security + docs hygiene)

**Decision:** Address High/Medium audit findings that are agent-fixable without a Core/WPF refactor; leave Dependabot merges and Scorecard triage to `[HUMAN]`.

**Changes:**
- Purge legacy `FtpPass` from `config.json` immediately after Credential Manager migrate (`LoadConfig` → `SaveConfig`)
- Crash dumps read `config.json` and redact `FtpPass`
- FTP path normalizers collapse `.` / `..` without climbing above root
- Document Win32 `CredentialPersistence.LocalMachine` as per-user persistent
- Docs: AGENT_MEMORY package versions, MODULE checklist, README version callouts, COMPLETED_TASKS DialogOverlays row

**Validation:** `watch-agent-gates` 9 stages OK; **127** Release tests; Dependabot/CodeQL open alerts = 0.

**Deferred (superseded):** See R2-D1 / R2-D2 / R2-D3 entries below — completed 2026-07-12.

---

## 2026-07-12 — Dependabot merges + Scorecard workflow fix

**Decision:** Squash-merge Dependabot PRs #10 (NuGet) and #7 (Actions). Fix Scorecard publish failure by moving write permissions to job scope (`permissions: read-all` at workflow level), matching [ossf/scorecard-action workflow restrictions](https://github.com/ossf/scorecard-action#workflow-restrictions). Also upload SARIF to code scanning.

**Root cause:** `publish_results: true` rejected the workflow because top-level `security-events: write` / `id-token: write` violate Scorecard API integrity checks.

**Validation:** Scorecard [run 29203183074](https://github.com/edwardlthompson/QuickMediaIngest/actions/runs/29203183074) success; `check-scorecard-sarif.sh` OK (no error/warning findings).

---

## 2026-07-12 — Release v1.3.19

**Decision:** Ship Audit R2 security fixes + backlog D1–D3 (DecodedThumbnail, LogPathSanitizer, Core coverage tests) as v1.3.19.

**Validation:** feature-gate 9 stages; 144 Release tests; CI/Security/CodeQL green on prior HEAD; Dependabot open Critical/High = 0 (API); Scorecard green after permission fix.

---

## 2026-07-12 — R2-D1: DecodedThumbnail replaces BitmapSource in Core

**Decision:** Core thumbnail public APIs and decode/cache paths use `DecodedThumbnail` (JPEG bytes + WxH). WPF shell/STA/BitmapSource orchestration lives under `QuickMediaIngest/Thumbnails/Wpf/`. ViewModels convert via `WpfThumbnailBridge.ToBitmapSource`.

**Rationale:** Keeps `Core/` free of WPF types (CODE_REVIEW F-002) while preserving shell/COM decode on an STA thread outside Core.

**Validation:** `dotnet test QuickMediaIngest-1.sln -c Release` — **144** passed; all `Core/**/*.cs` ≤ 200 lines; no `BitmapSource` / `System.Windows` usings under Core (doc cref only in `IShootFilterService`).

---

## 2026-07-12 — R2-D2/D3: log path sanitization + Core coverage tests

**Decision:** Add `LogPathSanitizer` for Information/Error logs (local / AppData / FTP). Expand unit tests for MetadataKeywordWriter, IngestItemProcessor, UpdateService (mock HttpClient), and the sanitizer.

**Validation:** **144** Release tests green.

---

## 2026-07-12 — AUTO-SBOM: CycloneDX on Build and Release

**Decision:** Generate CycloneDX JSON via Syft (`anchore/sbom-action@v0.24.0`) against `./publish/portable` after publish; attach to workflow artifacts and to GitHub Releases on `workflow_dispatch`. Local optional path: `scripts/generate-sbom.sh`.

**Rationale:** Closes regress gap from v1.3.19 (INITIALIZATION_PROMPT `[AUTO]` SBOM requirement).

---

## 2026-07-17 — Release v1.3.20 (SD/USB I/O stall)

**Decision:** Cap removable-drive preview/import parallelism (`RemovableDriveIo`), cancel preview CTS when import starts, run Shell/WPF thumbnail fallback on `StaRunner` (not UI dispatcher), and rethrow `OperationCanceledException` from `IngestItemProcessor`. Also fix Dependabot alert counting (`--paginate`, no `page=`) and normalize `scripts/*.sh` to LF.

**Rationale:** Concurrent sync preview workers + UI-marshaled Shell decode + parallel copies thrash SD/USB and freeze the UI until force-close.

**Validation:** `pre-release-gate.sh` passed; feature-gate 9 stages; **151** Release tests; zero open Critical/High Dependabot alerts.

---

## 2026-07-17 — Regress v1.3.20

**Checks:** Post-tag `pre-release-gate.sh` green on `ae84019`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.20.cyclonedx.json` on [v1.3.20](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.20).

**N/A:** GitHub Pages (WPF app); `simulate-template-upgrade.sh` not present in child repo.

**Follow-up [HUMAN]:** Confirm SD/USB preview+import on a real card with v1.3.20 portable build.

---

## 2026-07-21 — Release v1.3.21 (import Dispatcher freeze)

**Decision:** Replace sync `Dispatcher.Invoke` on import byte/`ItemProcessed` progress with `BeginInvoke` + coalesced byte snapshots.

**Rationale:** `ImportByteProgressTracker.ReportBytes` publishes every 1MB; sync UI marshaling blocked copy threads and froze the app mid-SD import (no log/dest growth; CPU still active). Observed after 42/132 files on a Canon exFAT card.

**Validation:** Local recovery copied remaining 90 with size checks; feature-gate green; **151** Release tests; `pre-release-gate.sh` green before ship.

---

## 2026-07-21 — Regress v1.3.21

**Checks:** Post-tag `pre-release-gate.sh` green on `ecb9d04`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.21.cyclonedx.json` on [v1.3.21](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.21).

**N/A:** GitHub Pages; template upgrade simulation script.

