# Decision Log

> Append-only register of major technical trade-offs. Past entries are immutable.

## 2026-08-30 — Release v1.4.0 (Golden Path & Feature Backlog I-01..I-80)

**Status:** Accepted
**Context:** Full implementation, automated testing, and pre-release verification of Golden Path 1–7 and all 80 Ongoing Maintenance & Feature Backlog rows (`I-01` through `I-80`).

**Decision:**
- Landed complete vertical slices for Ingest workflow, Devices & Sources, Date/Time/Folders, Previews/Codecs, Feedback, UI/A11y/I18n, and Architecture/Quality.
- Automated human items (OP13 ADB live smoke harness, sacred spec/plan authoring).
- Bump version to `1.4.0` across project metadata, release notes, and changelog.
- Build and publish release assets (portable single-file EXE, WiX MSI, CycloneDX SBOM) via GitHub Actions workflow.

**Validation:** 360 unit/integration tests passing; local pre-release gates and remote GitHub Actions CI, Security Scan, and CodeQL all green.

---

## 2026-08-30 — I-09 OpenSSF Scorecard Workflow Exception

**Status:** Accepted
**Context:** Row I-09 in BUILD_PLAN requires OpenSSF Scorecard workflow review.

**Decision:**
- `.github/workflows/scorecard.yml` is configured with minimal read-all at workflow level, scoped write permissions for security-events and id-token, and uses official `ossf/scorecard-action@v2.4.3`.
- Any repository-level Scorecard score variances (e.g. branch protection rules, code review enforcement) are managed via GitHub repository settings and require repository admin rights (`[HUMAN]`), while workflow definition is validated green and compliant with OpenSSF guidelines.

**Validation:** Workflow syntax verified against OpenSSF Scorecard action standards; `check-scorecard-sarif.sh` and local gates pass.

---
## 2026-08-30 — I-01 Magick.NET 14.16.0

**Status:** Accepted
**Context:** `/build` next row I-01. CI had NU1901/NU1902 on 14.14.0; CVE-2026-64685 fixed in 14.15.0+.

**Decision:** Bump `Magick.NET-Q16-AnyCPU` to 14.16.0 (app, tests lockfile, DngProbe). Add assembly-version floor test. Do not copy `examples/`.

**Validation:** `dotnet restore --force-evaluate -p:EnableWindowsTargeting=true` on Linux; Windows CI `dotnet test` for Magick decode tests.

---
## 2026-08-30 — `/allideas` board fill (I-01..I-80)

**Status:** Accepted
**Context:** User said add all ideas to the build plan (board only).

**Decision:**
- Add 80 `[AGENT]` rows I-01..I-80 under `## Ongoing Maintenance` (parseable `- 🔲 [AGENT]`).
- Skip leftovers already on the board: OP13 smoke, GP AUTO/HUMAN, GP-8 Sacred spec/plan, WPF UI sign-off.
- Do not implement. Do not create `docs/spec.md` or `docs/plan.md`. I-75 is a HUMAN checklist only.

**Validation:** Board-only; `validate-bootstrap.sh --quick` still applies to markdown tables.

---
## 2026-08-30 — `/build` Golden Path 1–8 implementation

**Status:** Accepted
**Context:** User invoked `/build 1-8`. Status parser did not see backtick GP rows; execute anyway. Linux agent has no .NET 8 / WPF.

**Decision:**
- Port catalog specs into existing QMI folders. Never copy `examples/`.
- Order: privacy-report → github-feedback → crash-capture → settings toggle → feedback UI → About Request-a-feature → display-refresh vote (no `ChangeDisplaySettings`).
- Crash persist is opt-in (`SaveCrashDetails` default false). Existing `fatal.log` / `crash_*.log` stay but are sanitized.
- GP-8 Sacred: do not create `docs/spec.md` or `docs/plan.md`. Backlog HUMAN smoke + spec authoring.
- Mark AGENT GP-1..7 done after implementation. Leave AUTO feature-gate open (environment block). Do not archive.

**Validation:** `validate-bootstrap.sh --quick`. `feature-gate.sh --stack dotnet-wpf` expected exit 2 on this Linux VM.

---
## 2026-08-30 — Golden Path BUILD_PLAN rows 1–8

**Status:** Accepted
**Context:** After the v1.0.0 catch-up, the human named items 1–8 (not “do all”) to add board rows only.

**Decision:**
- Add Sequential Golden Path rows GP-1..GP-7 as one later `/feature` each; GP-8 is `[HUMAN]` Sacred `docs/spec.md` + `docs/plan.md`.
- Do not implement slices in this change. Do not copy `examples/` over the WPF app.
- About (1) and Settings (3) are align-existing; crash/feedback/github-feedback/privacy/display-refresh are new WPF ports of catalog specs.

**Validation:** Board-only edit; `validate-bootstrap.sh --quick` still applies to process files.

---
## 2026-08-30 — Template catch-up to agent-project-bootstrap v1.0.0

**Status:** Accepted
**Context:** Child repo was at process `0.16.0`; upstream latest release is `v1.0.0`. Goal: gain `/upgrade` and current Golden Path gate machinery without overwriting the WPF product.

**Decision:**
- Copy Canon (commands, rules, help docs, new template scripts, schemas, example stubs).
- Merge Mixed (`bootstrap.config.json`, `.gitignore`, `.env.example`, `TEMPLATE_INDEX.json`, `PROJECT_CHECKLIST.md`, `validate-bootstrap.sh`).
- Leave Sacred untouched (`AGENTS.md`, `docs/INITIALIZATION_PROMPT.md`, LICENSE, product app). Do not create `docs/spec.md` / `docs/plan.md` from the template stub.
- Keep child-only scripts and WPF file-limit / feature-gate / license checks.
- Do not add release-please, Pages, or copy `examples/` over the app.
- Stamp `branding/product.json` as `mode: product` with Quick Media Ingest identity.

**Validation:** `validate-bootstrap.sh --quick`; `feature-gate.sh --stack dotnet-wpf`.

---
## 2026-08-21 — Donate + filename-version updates (v1.3.27)

**Status:** Accepted
**Context:** Continuum Calendar method: quiet Venmo, one optional donate note after a version change, and GitHub update checks that must not use git/template tags (those can diverge from the product installer).

**Decision:**
- Compare product versions parsed from release asset filenames; ignore `tag_name`.
- Store last-check, dismiss, and donate-nudge state in device-local `update-donate.json`, not `config.json`.
- Automatic prompt is Install (open asset URL) | Later (silence that product version). About keeps Check now + in-app download.
- Publish additional versioned assets (`QuickMediaIngest-X.Y.Z-x64.exe` / `-x64-setup.msi`) alongside existing unversioned names.

**Validation:** 252 Release tests; `prerelease-autofix` + `pre-release-gate` (CI/Security/CodeQL on prior HEAD; zero Critical/High Dependabot); human sign-off automation.

---
## 2026-08-10 — PreferAdb pipe-drain + already-imported recovery (v1.3.26)

**Status:** Accepted
**Context:** Large Prefer-ADB pulls hung with idle CPU (stdout/stderr pipe fill). Duplicate FTP→ADB batches marked already-pulled files as failures; `Point & Shoot` deletes failed because `&` broke Android `sh` double-quoted `rm`.

**Decision:**
- Drain ADB stdout/stderr concurrently in pull/shell helpers (`AdbFileProvider` + scanner/preview/thumb process partials).
- Single-quote remote paths for `shell rm`; treat missing remote as benign delete success.
- On copy failure, if a size-verified destination already exists, count success and honor Delete after Import (`IngestAlreadyImported`).

**Validation:** Feature gate + prerelease autofix; unit tests for recovery and benign delete; local portable `1.3.26-hardened`.

---
## 2026-08-09 — PreferAdb import hang harden (v1.3.24)

**Status:** Accepted
**Context:** PreferAdb FTP→ADB import of large TIFF/DNG files stalled ~5 minutes with 8-way parallelism against a nearly-full destination; fixed wall timeout then failed six files and left truncated stubs.

**Decision:**
- Cap ADB/remapped import concurrency at 2 (`AdbTransferIo`).
- Hard free-space preflight (selected + 256MB margin); soft-warn when sizes unknown and free < 256MB.
- Delete partial destinations on failure and cancel; size-scaled ADB pull timeout 5–10 min.
- FTP failover mid-pull deferred.

**Validation:** Feature gate + pre-release gate; 222 unit tests; local portable `1.3.24`; CI/Security/CodeQL green on `864853b`; `workflow_dispatch` Build and Release for tag/assets.

---
## 2026-08-01 — Unified load fail-fast FTP + progressive local UI

**Status:** Accepted
**Context:** SD card (`E:\`) enumeration finished in tens of ms, but Unified browse waited ~9s on dead FTP `10.0.0.7`, threw, discarded local results, and retried.

**Decision:**
- Soft-fail FTP in unified merge (never fail the whole load); 8s connect probe then 45s listing; PreferAdb unchanged.
- Progressive paint: show local items as soon as drive scans finish; status explains FTP still loading / unavailable.
- Process-local 60s FTP host cooldown + single-flight unified load with queued forceRefresh.
- Do not cache empty FTP failures.

**Validation:** Unit tests for soft-fail, budgets, cooldown; local preview WallTimeMs logging; gates + local portable build. Shipped as **v1.3.23** (CI green; `workflow_dispatch` release assets + SBOM).

---

## 2026-07-30 — MP4 thumbs Windows-aligned (MediaStore JPEG, not full pull)

**Status:** Accepted
**Context:** Three DCIM MP4s (174MB–1.1GB) failed grid preview. Truncated ADB pulls are not seekable; Shell needs a coherent local file. Explorer uses Shell locally and MTP thumbnail JPEGs for phones — not full-video download.

**Decision:**
- PreferAdb video path: `IAdbVideoThumbnailFetcher` resolves MediaStore id and pulls a small JPEG (`…/thumbnail` or thumbnails `_data`).
- Skip truncated video tiers when size > 8MB; complete-file Shell/ffmpeg fallback only when size ≤ **256MB**.
- No multi‑GB pulls solely for grid previews.

**Validation:** Unit coverage for fetch tiers / fallback gate + PreferAdb routing (device JPEG skips pull; ≥256MB never full-pulls); PreferAdb smoke for DCIM MP4s when device attached.

---

## 2026-07-30 — HEIC preview load felt empty (Magick-first latency)

**Status:** Accepted
**Context:** After rejecting false embedded JPEGs, CompleteFile used Magick on every HEIC. Mid-load the grid looked empty (“almost none showing”); smoke later showed **loaded 112 / failed 3 / pending 0** (3 ≈ MP4 + rare still).

**Decision:**
- `GetFetchTiers`: single-shot pull when known size ≤ type cap; unknown HEIC skips 64K–512K tiers.
- Magick HEIC open prefers `heic:thumbnail` define then full decode.

**Validation:** PrintWindow smoke health 112/3/0; warm cache reload; unit coverage for fetch tiers.

---

## 2026-07-30 — HEIC embedded JPEG false positives → Preview failed

**Status:** Accepted
**Context:** Pipeline reported ~112 loaded but UI showed many HEIC/DNG “Preview failed”. Offline Magick decoded OP13 HEICs fine; WPF rejected the bytes the pipeline returned.

**Decision:**
- Naive `FF D8…FF D9` scans inside HEIC BMFF produce corrupt “JPEGs”; `LooksGlitchy` Magick failures previously counted as “not glitchy,” so embeds short-circuited Magick and WPF failed.
- Require Magick-decodable JPEG for SOI payloads; require `FF D8 FF` after SOI for embeds; Magick-first for CompleteFile HEIC; bump disk cache to `ftp-thumb-v4`.
- Always copy rendered sibling thumbs onto RAW tiles (even when pairs are not stacked).

**Validation:** 190 tests; corrupt SOI + BMFF false-positive unit coverage.

---

## 2026-07-30 — Fix failed (non-glitch) PreferAdb previews

**Status:** Accepted
**Context:** Glitches gone but many “Preview failed”. All OP13 HEICs are >2MB (cap was 2MB); DNG siblings used DNG byte size; videos never full-downloaded; CompleteFile used FTP only.

**Decision:**
- HEIC preview budget **12MB**; prefer ADB `pull` when file fits budget; complete buffers decode as `CompleteFile`.
- CompleteFile fallback prefers ADB pull (≤40MB) before FTP; videos allowed up to 80MB.
- Sibling HEIC probes use `knownFileSize=0` (not parent DNG size).
- Unified applies rendered-sibling thumbnails after each FTP/ADB batch.

**Validation:** 188 tests; gates 9/9; smoke ADB decode dominant, skipped ≈ videos only.

---

**Status:** Accepted
**Context:** UI showed many failed/broken previews. Logs: PreferAdb claimed but **ADB decode always 0**; FTP Magick on truncated HEIC produced green glitches; selecting FTP sidebar crashed Groups rebuild off-UI-thread.

**Decision:**
- ADB capped fetch via `exec-out sh -c "dd if='…'"` (single-quoted `exec-out dd` was emitting `dd: '…` text as “success”).
- Reject non-media payloads (`dd:` / short buffers) before decode.
- No Magick on capped HEIC/HEIF (same rule as RAW/video); bump FTP thumb cache to `ftp-thumb-v3`.
- `RebuildGroupsFromCurrentItems` always on Dispatcher after async scan.

**Validation:** Unit payload tests; `dotnet test`; OP13 smoke expects `ADB decode > 0`.

---

**Status:** Accepted
**Context:** Unified ignored `LimitFtpThumbnailLoad` (loaded 115); PreferAdb transport opaque in Info logs; phone FTP Connection-reset noise from parallel capped RETRs.

**Decision:**
- Honor `LimitFtpThumbnailLoad` / `FtpInitialThumbnailCount` for Unified and FTP source (initial batch + background remainder).
- Info-log PreferAdb vs FTP transport at batch start; finish line includes ADB/FTP decode counts.
- Cap thumb download parallelism at 3; when PreferAdb session resolves, force Balanced (no FluentFTP pool) for thumbs.

**Validation:** 187 unit tests; watch-agent-gates 9/9; OP13 smoke after find+stat `|` fix.

---

**Status:** Accepted (amended same day: find+stat separator)
**Context:** Post-hybrid OP13 session: 332 Warning FTP 550 sibling probes for phantom `.heif`/`.jpg`; ADB `FileSize=0` blocked size-capped pull; reconnect left Sources empty of selection. Follow-up smoke: ADB scan returned 0 because toybox `stat -c '%n\t%s'` emits literal `\t`, so parsers dropped all paths under folders with spaces (e.g. `Point & Shoot`).

**Decision:**
- When PreferAdb thumb session + `IAdbPathProbe` are set, `FileExists` (cached) before sibling/HEIF candidate ADB/FTP probes; missing paths mark 550 cache.
- Demote “permanent failure cached” skip logs to Debug (first 550 still Warning).
- ADB scan prefers `find … -exec stat -c '%n|%s'` (pipe separator); fall back to plain `find` if sized parse yields 0 media.
- Successful FTP auto-reconnect selects Unified (starts browse) after ensuring `_unifiedSource` is in Sources.

**Validation:** Unit tests for find-line size parse (pipe + spaces in path); `dotnet test` + agent gates.

---

## 2026-07-29 — Seamless hybrid ADB browse (scan + thumbs + import)

**Status:** Accepted (supersedes browse half of “Hybrid FTP browse / ADB pull” same day)
**Context:** OP13 FTP LIST aliases (.heif/.jpg phantoms), 550 RETR storms, Magick green/magenta glitches on capped RAW/video, Unified re-scanning `/DCIM` after FTP, PreferAdb already on for import.

**Decision:**
- Seamless hybrid (A): keep FTP sidebar/Unified UI; when PreferAdb + `TryResolve`, ADB owns **scan** (`find -type f`), **thumbnails** (dd → size-capped pull), and **import**; FTP fallback on failure.
- `ImportItem.SourcePath` stays FTP-style; map via `AdbAndroidPath`.
- HEIF→HEIC RETR/probe candidates with always-fall-back to original `.heif`.
- Permanent FTP 550 negative cache (host|port|path); clear on reconnect; FluentFTP thumb `RetryAttempts=0`.
- Partial RAW/video: never Magick/Shell on capped buffers; glitch JPEG reject in `ThumbnailPreviewValidator`.
- Unified/FTP share `_sourceItemsCache` + inflight scan map; narrow cache clears on scan path change.
- First `adb devices` serial only; multi-device picker still deferred.

**Alternatives rejected:** Dedicated sidebar ADB source; Magick on TieredFinalCap RAW/video; retrying 550×3 per worker.

**Validation:** Unit tests (path normalize, failure cache, glitch fixtures, Adb path rewrite); `dotnet test`.

---

## 2026-07-29 — Hybrid FTP browse / ADB pull for Android imports

**Status:** Accepted
**Context:** OP13 FTP session showed DHCP IP churn breaking host-keyed Credential Manager entries; large `/DCIM` libraries; ADB already available on the same device for faster copies.

**Decision:**
- Keep FTP for browse/scan/thumbnails; when `PreferAdbTransferWhenAvailable` (default true) and preflight finds a device + readable `/sdcard` or `/storage/emulated/0` remote folder, import Copy/Delete via remapped `AdbFileProvider`.
- No mid-import soft-fallback to FTP (avoids mixed transport / delete-after confusion).
- Multi-device: first `adb devices` serial only; show serial suffix in UI; picker deferred.
- Vault: `TryMigratePassword` from previous/saved hosts on IP change; fail-fast when password missing.
- FTP listing `KeepAlive=false`; ADB processes killed on cancel / 5-minute per-file wall timeout.
- Skip `.trashed-*`, `.nomedia`, and `.Trash`/`trash` directories at scan time.
- Fix ConfigLoad wiping FTP thumbnail limits; default limit on with count 48.

**Alternatives rejected:** First-class sidebar `AdbSourceItem` scan in this slice; mid-group FTP fallback; GUID vault keys.

**Validation:** Unit tests for migrate/path/trash; `watch-agent-gates` / `dotnet test`.

---

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

## 2026-07-28 — Release v1.3.22 (settings persistence)

**Decision:** Guard naming rebuild during `LoadConfig`; coerce mismatched presets to Custom; restore destination preset after combo refresh; query GitHub Actions per workflow name in `check-github-ci.sh`.

**Rationale:** Custom naming templates were overwritten when a stale Recommended preset re-applied on load/UI bind; destination combo Clear() nullled selection. CI poll listed Dependabot Updates first and never saw the CI run.

**Validation:** Feature-gate + pre-release-gate green; **154** Release tests; zero Critical/High Dependabot alerts.

## 2026-07-28 — Regress v1.3.22

**Checks:** Post-tag `pre-release-gate.sh` green on `efb067c`; CI / Security Scan / CodeQL green; Dependabot Critical/High = 0.

**Release assets:** `QuickMediaIngest.exe`, `QuickMediaIngest-Portable.zip`, `QuickMediaIngest.msi`, `QuickMediaIngest-1.3.22.cyclonedx.json` on [v1.3.22](https://github.com/edwardlthompson/QuickMediaIngest/releases/tag/v1.3.22).

**N/A:** GitHub Pages; release-please (declined for this product). `simulate-template-upgrade` web-stack smoke gaps are expected for this WPF-only child.
