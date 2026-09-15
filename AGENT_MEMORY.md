# Agent Memory

> Centralized index of tech stack, threat models, persistent context, and retrospectives.
> Update only at session startups, milestone boundaries, or major architectural pivots.

## Tech Stack

| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Runtime | .NET | 8.0 | `net8.0-windows` |
| UI | WPF + MaterialDesignThemes | 5.3.2 | Dark/light theme (Windows) |
| UI (Linux) | Avalonia Fluent | 11.3.22 | Second view over Core/AppModel |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 | Partial ViewModels |
| DI | Microsoft.Extensions.DependencyInjection | 10.0.9 | Registered in `App.xaml.cs` |
| FTP | FluentFTP | 54.2.1 | Thread-safe connections |
| Images | Magick.NET-Q16-AnyCPU | 14.17.1 | Thumbnails, RAW sidecars |
| Metadata | MetadataExtractor | 2.9.3 | EXIF read/write |
| Storage | System.Data.SQLite.Core | 1.0.119 | VACUUM maintenance; single-file extract required |
| Testing | xUnit + Moq | 2.5.0 / 4.20.70 | `QuickMediaIngest.Tests/` + `QuickMediaIngest.Core.Tests/` |
| Installer | WiX Toolset | 4.0.4 | MSI via GitHub Actions |
| License | MIT | — | Pure FOSS |
| Distribution | GitHub Releases | — | Portable EXE + MSI |
## Active Modules

- [x] .NET / WPF (`modules/dotnet-wpf/MODULE.md`)
- [ ] Android / F-Droid — not applicable
- [ ] Web / PWA — not applicable
- [ ] Python — not applicable
- [ ] Lightroom Classic — not applicable

## Threat Model Checklist

- [x] `docs/THREAT_MODEL.md` drafted (STRIDE, trust boundaries, abuse cases)
- [x] No proprietary closed-source SDKs in production path
- [x] Opt-in only telemetry (none shipped); see `docs/PRIVACY.md`
- [x] Secrets excluded from VCS (Gitleaks pre-commit)
- [x] Dependency vulnerability scanning enabled (CodeQL + Trivy + Dependabot)
- [x] Input validation at FTP and file path boundaries
- [x] `SECURITY.md` and private vulnerability reporting enabled (human setup)

## Persistent Context

### Project Purpose

Quick Media Ingest imports photos and videos from SD cards, local drives, and FTP (including phone/camera Wi-Fi shares) into dated, shoot-based destination folders with configurable naming, metadata options, and safety checks.

### Key Constraints

- WPF adapted file limits: `.xaml` 800, ViewModels/`*.xaml.cs` 400, `Core/` 200 lines
- Trunk-based development with Conventional Commits
- Nullable reference types enabled project-wide
- SQLite requires `IncludeAllContentForSelfExtract` for single-file publish (see csproj comment)

## Session Retrospectives

| Date | Milestone | What worked | What to improve |
|------|-----------|-------------|-----------------|
| 2026-09-15 | Release v1.5.0 | Linux Avalonia ingest-bench + Core extract; local pre-release gate 23 stages | Windows CI still owns WPF STA; optional Mint glance |
| 2026-09-15 | Window + preview pane | prefs.json bounds; GridSplitter 1600-edge preview | Off-screen clamp on multi-monitor Mint |
| 2026-09-15 | Overall import meter | Done/total + elapsed/ETA; single SHA-256 pass with catalog meter; DropImported after catalog | Large dest-disk SHA-256 of 1500 RAW files still takes minutes — meter must stay visible |
| 2026-09-15 | Wide media + thumbs | AVIF/JXL/HIF ingest; Magick→Vips→HEIF→ffmpeg decode; Core.Tests 239; .deb reinstall | Real phone AVIF/JXL thumbs still depend on libheif/libjxl + ADB pull time |
| 2026-09-15 | Linux UX polish UX-1–40 | Fluent first-paint, Import gated on SelectedShoot, ingest sheet, Core motion on Desktop | HUMAN Mint glance UX-41; WatchFolderService_StartStop inotify flake |
| 2026-06-13 | Bootstrap parity | Template adoption in Reference mode | Split MainWindow/MainViewModel (Sprint 1) |
| 2026-06-20 | Template v0.11.0 migration | Phased bootstrap sync; slash commands + gate loop; WPF feature-gate | Confirm `/` menu in Cursor UI; push + CI green |
| 2026-06-21 | Release v1.3.17 | Human sign-off automation; P2–P8 backlog; MSI/libvips CI fixes | SBOM in build.yml; local `gh auth refresh -s security_events` for strict Dependabot gate |
| 2026-06-21 | Release v1.3.18 | Byte-weighted import progress + ETA (F-002) | LAN FTP smoke tests flaky offline |
| 2026-07-12 | Audit Sprint R2 | FtpPass purge + path collapse + crash redact; 127 tests; gates green | HUMAN: Dependabot #10/#7 merge; Scorecard failure |
| 2026-07-12 | R2 backlog D1–D3 | DecodedThumbnail Core; LogPathSanitizer; Update/Ingest/Keyword tests (144) | Separate Core csproj; FtpScanner/DeviceWatcher live tests |
| 2026-07-12 | Release v1.3.19 | Audit R2 + backlog shipped; CI green; build workflow_dispatch | SBOM in build.yml (AUTO-SBOM) |
| 2026-07-17 | Release v1.3.20 | SD/USB preview+import stall fix; RemovableDriveIo; Dependabot count script | Confirm removable throttle on real SD card; keep `scripts/*.sh` as LF |
| 2026-07-21 | Release v1.3.21 | Import progress `BeginInvoke` coalesce (mid-card freeze) | Keep Delete-after-import off until full pass; watch large MP4 imports |
| 2026-07-28 | Release v1.3.22 | Settings persistence (naming preset + destination combo) | Confirm prefs survive cold start on portable EXE |
| 2026-08-01 | Release v1.3.23 | Unified fail-fast FTP + PreferAdb/MP4 thumbs + HEIC fixes; local build + GH release | Warm PreferAdb smoke with OP13; confirm Unified paints E: while FTP down |
| 2026-08-09 | Release v1.3.24 | PreferAdb import hang harden (ADB concurrency cap, free-space gate, stub cleanup) | Watch large PreferAdb imports when destination free space is tight |
| 2026-08-21 | Release v1.3.27 | Quiet Venmo donate + filename-version GitHub update prompt; device-local prefs | Next `workflow_dispatch` must upload versioned EXE/MSI names |
| 2026-08-10 | Release v1.3.26 | ADB pipe-drain + already-imported recovery + `Point & Shoot` delete quoting | Watch PreferAdb dual-FTP alias duplicate batches |
| 2026-08-10 | Release v1.3.25 | Phone DateTaken: ADB mtime + EXIF MetadataReader rewired | PreferAdb phone shoot folders should match capture, not scan-time UTC |
| 2026-08-30 | Template align → 1.0.0 | Canon/Mixed catch-up; `/upgrade` + Golden Path gates; kept WPF app | HUMAN: spec.md/plan.md still unset (Sacred) |
| 2026-08-30 | `/build` GP 1–8 | Privacy, crash queue, settings toggle, feedback dialogs, GitHub composer, display-refresh vote | HUMAN smoke + `docs/spec.md`/`docs/plan.md`; AUTO feature-gate needs Windows/.NET 8 |
| 2026-09-14 | /cleanup + UX M7–B1 | Archived T/UNB/GP/Q1–M6; IUserPrompt, prefs search, afterglow, dest chip | Next: UX-B2 full-bleed import |
| 2026-09-14 | Two heads LX-L1–L5 | Core+Localization net8.0; AppModel; Linux adapters; Avalonia Desktop; .deb pack+smoke | Next: LX-trim after Magick/Vips smoke; Dependabot PRs stay human |
| 2026-09-14 | LX-trim + Dependabot | Trimmed .deb `--smoke-native` Magick+Vips (SQLite WPF-only); close stale Magick PR | Native AOT still out of v1; Avalonia compiled bindings for overlays |
| 2026-09-15 | Linux ingest-bench parity LP 43–71 | AppModel + Avalonia Fluent ingest-bench (scan through eject); trimmed `.deb` smoke-native; Core.Tests 175 | Uncommitted LP tree; optional live Mint glance still useful; Windows CI still owns WPF STA |
| 2026-09-15 | Linux depth (LD) after Mint glance | Naming is raw template text; Windows still has Settings builder + shoot-card/thumb/filter/prefs wall | Next: LD-naming token chips/presets; do not compile MainViewModel into Desktop |
| 2026-09-15 | /build Linux ingest-bench depth LD | Token chips/presets/preview; shoot cards; Magick wrap-grid+zoom; hours/type chips; dest/dup/verify/RAW/confirm prefs | Restart the Linux app to pick up the new chrome; leftover Windows prefs still later |
| 2026-09-15 | Linux UX audit → BUILD_PLAN UX-1–41 | Linux first paint is a control inventory vs Windows Settings/View disclosure | Next: UX-onboard bind Onboarding_Body; do not compile MainViewModel |
## Template Provenance

- **Source template:** `edwardlthompson/agent-project-bootstrap`
- **Template version:** `1.5.0` (see `.template-version`)
- **Alignment:** `docs/BOOTSTRAP_ALIGNMENT.md`
- **Last update check:** See `.template-update.json`
