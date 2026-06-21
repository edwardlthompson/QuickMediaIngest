# Build Plan

> Prioritized task board with owner labels. Completed milestones live in `COMPLETED_TASKS.md`.

## Status legend

| Icon | Meaning |
|------|---------|
| ✅ | Done |
| 🔲 | Open — blank checkbox |
| ❌ | Blocked / failed / sign-off pending |

**Task format:** `- 🔲 [OWNER] Description`

## Owner Label Legend

| Label | Owner | When to use |
|-------|-------|-------------|
| `AGENT` | Cursor Agent | Code, docs, scaffolding, tests, CI config |
| `HUMAN` | Human developer | Approvals, credentials, GitHub settings, product decisions |
| `ADB` | Human (Android) | Android SDK, emulator/device testing (ADB provider testing) |
| `AUTO` | CI/scripts/bots | GitHub Actions, Dependabot, pre-commit, update checker |

**Current release:** v1.3.16

**Active work:** Template Migration Sprint ✅ complete — pick up **Backlog** Parallel lane items

---

## Template Migration Sprint

> Align with upstream template **v0.11.0** (pinned **v0.2.0**). Critique mitigations in `DECISION_LOG.md` (2026-06-20).

### Phases 1–2 ✅ Complete

| Phase | Status |
|-------|--------|
| Phase 1 — Foundation & stack selection | ✅ |
| Phase 2 — Slash commands & rules | ✅ (HUMAN `/` menu check moved to Phase 5) |
| Phase 3 — Gate scripts & CI | ✅ |

### Remaining (Sequential)

#### Phase 3 — Gate scripts & CI

- ✅ [AGENT] Extend `scripts/validate-local.ps1` — optional `-WatchGates` / `-PreRelease` / `-QuickBootstrap` flags; Git Bash path fix
- ✅ [AGENT] Update `.github/workflows/ci.yml` — add `check-batch-commands.sh`, `check-repo-hygiene.sh`
- ✅ [AGENT] Gate smoke: bootstrap + hygiene + batch commands pass via Git Bash; `dotnet build` OK; 88 unit tests pass (1 test session timeout — see Phase 5)

#### Phase 4 — Index, docs & memory ✅

- ✅ [AGENT] Merge `TEMPLATE_INDEX.json` for new rules, commands, gate scripts; run `validate-template-index.sh`
- ✅ [AGENT] Update `AGENTS.md`, `docs/FOR_AGENTS.md`, `docs/START_HERE.md`, `PROMPT_LIBRARY.md`, `README.md`
- ✅ [AGENT] Update `AGENT_MEMORY.md`; bump `.template-version` to `0.11.0`

#### Phase 5 — Validation & sign-off ✅

- ✅ [AGENT] Run gate suite: `validate-bootstrap.sh`, `feature-gate.sh --stack dotnet-wpf`, `pre-release-gate.sh`, `check-batch-commands.sh` — pre-release partial: Dependabot strict needs `gh` security_events scope
- ✅ [AGENT] Run `dotnet test -c Release` — 101 passed (incl. human verification smoke)
- ✅ [AGENT] Document super-command smoke results in `DECISION_LOG.md`
- ✅ [HUMAN] Confirm `/bootstrap`, `/build`, `/verify`, `/ship`, `/gates`, `/audit` in Cursor `/` menu
- ✅ [HUMAN] Sign off template version bump (v0.11.0)
- ✅ [AUTO] Push branch; CI green on all new gates — `e59ec6f` + CI fix `7720986` (`.gitattributes` CRLF)

---

## HUMAN Verification — v1.3.12 / v1.3.13

Source: `10.0.0.23:2221/DCIM` · Thumbnail Performance: **Ultra**

**Automated smoke:** `.\scripts\smoke-human-verification.ps1` (or `-RequireFtp` to fail when LAN FTP offline). Skips FTP tests gracefully on CI; runs full checks when LAN server is reachable.

| BUILD_PLAN check | Automation | Remaining HUMAN |
|------------------|------------|-----------------|
| Delete After Import persists | ✅ `ConfigFilePersistenceTests` — config.json round-trip | Visual confirm dialog skipped on restart |
| Thumbnail zoom persists | ✅ `ConfigFilePersistenceTests` — `ThumbnailSize: 200` | Slider UI spot-check optional |
| Cold load tier caps | ✅ `HumanVerificationSmokeTests.FtpColdLoad_*` | Log review in app optional |
| Reconnect cache hit | ✅ `HumanVerificationSmokeTests.FtpReconnect_*` | App log phrase optional |
| Ultra / FluentFTP | ✅ `HumanVerificationSmokeTests.FtpUltraMode_*` + DCIM batch smokes | libvips in **published** exe only |
| Weekly CVE triage | ✅ `check-security-triage.sh` via smoke script | Sign-off when gh lacks `security_events` |

### Settings (v1.3.12)

- ✅ [AUTO] Delete After Import + prompt dismissed persist in `config.json` — `ConfigFilePersistenceTests`
- 🔲 [HUMAN] Visual: enable → confirm → restart → toggle stays on; no startup dialog when previously confirmed

### FTP thumbnails (v1.3.12 + v1.3.13)

- ✅ [AUTO] Cold load tier caps (not full 8 MB) — `HumanVerificationSmokeTests` + `FtpThumbnailIntegrationTests`
- ✅ [AUTO] Reconnect disk cache hit — `HumanVerificationSmokeTests.FtpReconnect_SecondLoadUsesDiskCache`
- ✅ [AUTO] Ultra mode JPG+HEIC load — `HumanVerificationSmokeTests` + `FtpThumbnailPipelineDcimSmokeTests`
- 🔲 [HUMAN] libvips decode in **published** portable exe (native DLL bundle smoke)

### Thumbnail zoom (v1.3.13)

- ✅ [AUTO] `ThumbnailSize: 200` in config.json — `ConfigFilePersistenceTests`
- 🔲 [HUMAN] Visual: slider at 200 after restart

### CI

- ✅ [AUTO] `dotnet test` + CI green after push — CI `27889916358` green on `7720986`

**Acceptance reference:** see Milestones 8–9 in `COMPLETED_TASKS.md` for shipped features and key files.

---

## Ongoing Maintenance

### Weekly (recurring)

- ✅ [AGENT] CVE triage pass — 2026-06-13: no open critical Dependabot alerts
- 🔲 [HUMAN] Run weekly CVE triage per `docs/SECURITY_TRIAGE.md` (recommended: Monday; sign-off) — or run `.\scripts\smoke-human-verification.ps1`
- ✅ [AUTO] Trivy + CodeQL + CI green after next push — `7720986` on `main`

### Backlog (not blocking)

> Deferred from Sprint 1 / Milestone 9, `COMPLETED_TASKS.md`, and post–template-migration follow-ups. **Parallel lane** — no ordering required unless noted.

#### Parallel lane — priority order (when picking up backlog)

| P | Task | Owner |
|---|------|-------|
| 1 | Published portable exe libvips + native DLL smoke | AGENT |
| 2 | `DialogOverlaysView` + remaining MainWindow handlers | AGENT |
| 3 | Grandfather file-limit cleanup (MainViewModel + Core/Ftp) | AGENT |
| 4 | Headless WPF smoke in CI | AGENT |
| 5 | Core architecture (WPF types out of Core, ShootFilterService, FTP scan unification) | AGENT |
| 6 | UI UserControl extractions (optional polish) | AGENT |
| 7 | MSI install/uninstall CI step | AGENT |
| 8 | Template delta sync + Scorecard SARIF triage | AGENT |

#### UI / MVVM polish

- 🔲 [AGENT] `DialogOverlaysView` + remaining MainWindow event handlers — Sprint 4 carry-over from `COMPLETED_TASKS.md`
- 🔲 [AGENT] Extract Sidebar UserControl — optional; `MainWindow.xaml` under limit
- 🔲 [AGENT] Extract Import panel UserControl — optional; `MainWindow.xaml` under limit
- 🔲 [AGENT] Extract ShootGroups UserControl — optional polish

#### Human verification — automation follow-ups

- 🔲 [AGENT] Published portable exe smoke — `scripts/smoke-published-exe.ps1` (libvips DLL load + FTP thumbnail from `publish/local-test/`)
- 🔲 [AGENT] `DeleteAfterImportConfirmHelper` headless test — prompt skipped when `DeleteAfterImportPromptDismissed` (WPF host)
- 🔲 [AGENT] `MainViewModel` config reload test — `ThumbnailSize` + `DeleteAfterImport` after simulated `LoadConfig`
- 🔲 [HUMAN] Visual spot-check only: delete-after-import dialog on restart; thumbnail slider at 200

#### Core architecture

- 🔲 [AGENT] Remove WPF types from Core models (`ImportItem.Thumbnail`, `BitmapSource` in contracts)
- 🔲 [AGENT] Decouple `ShootFilterService` from ViewModels
- 🔲 [AGENT] Unify FTP scan listing with thumbnail transport stack — thumbnail path done in M9; scan listing remains

#### File limits (grandfather cleanup)

- 🔲 [AGENT] Split `MainViewModel.Config.partial.cs` below 400 lines
- 🔲 [AGENT] Split `MainViewModel.Ftp.partial.cs` below 400 lines
- 🔲 [AGENT] Split `MainViewModel.Thumbnails.partial.cs` below 400 lines
- 🔲 [AGENT] Split `Core/Ftp/FtpFileDownloader.cs` below 200 lines
- 🔲 [AGENT] Split `Core/Ftp/FtpThumbnailPipeline.cs` below 200 lines
- 🔲 [AGENT] Split `Core/Ftp/FtpTieredPreviewLoader.cs` below 200 lines
- 🔲 [AGENT] Split `Core/ThumbnailService.cs` below 200 lines

#### CI / release infrastructure

- 🔲 [AGENT] Wire `smoke-human-verification.sh` into CI as optional job (skip when no LAN FTP secret)
- 🔲 [AGENT] Headless WPF smoke test in CI — Sprint 6 follow-up
- 🔲 [AGENT] MSI install/uninstall CI step — WiX validation
- ✅ [AUTO] Human verification smoke — `scripts/smoke-human-verification.ps1` (config + optional LAN FTP)
- 🔲 [HUMAN] `gh auth refresh -s security_events` — local `pre-release-gate.sh` Dependabot strict check
- ✅ [AGENT] Add `TestResults/` to `.gitignore` — keep test output out of working tree

#### Template / tooling

- 🔲 [AGENT] Sync remaining upstream template deltas after v0.11.0 pin (quarterly via `check-template-updates.ps1`)
- 🔲 [AGENT] Scorecard workflow green — triage OpenSSF SARIF into BUILD_PLAN rows if not green

---

## Open Milestone Gates

| Gate | Owner | Status |
|------|-------|--------|
| Core `/**/*.cs` ≤ 200 lines (no grandfather) | AUTO | ✅ |
| Unit test baseline (101 tests, incl. smoke when LAN FTP up) | AUTO | ✅ |
| ADB provider device smoke test | ADB | ✅ (device `b5214fc6`) |
| Overlay MVVM decouple | AUTO | ✅ |
| Weekly CVE triage within last 7 days | HUMAN | ⚠️ Scripted via `check-security-triage.sh`; human sign-off when gh scope missing |
| FTP thumbnails on LAN test source | HUMAN | ⚠️ Auto smoke when LAN FTP up; published-exe libvips pending |
| Delete After Import persists across restart | HUMAN | ⚠️ Config auto-tested; UI dialog spot-check pending |
| Thumbnail zoom (`ThumbnailSize`) persists | HUMAN | ⚠️ Config auto-tested; slider UI spot-check pending |
