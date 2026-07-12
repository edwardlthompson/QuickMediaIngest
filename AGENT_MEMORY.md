# Agent Memory

> Centralized index of tech stack, threat models, persistent context, and retrospectives.
> Update only at session startups, milestone boundaries, or major architectural pivots.

## Tech Stack

| Layer | Technology | Version | Notes |
|-------|-----------|---------|-------|
| Runtime | .NET | 8.0 | `net8.0-windows` |
| UI | WPF + MaterialDesignThemes | 5.3.2 | Dark/light theme |
| MVVM | CommunityToolkit.Mvvm | 8.4.2 | Partial ViewModels |
| DI | Microsoft.Extensions.DependencyInjection | 10.0.9 | Registered in `App.xaml.cs` |
| FTP | FluentFTP | 54.2.0 | Thread-safe connections |
| Images | Magick.NET-Q16-AnyCPU | 14.14.0 | Thumbnails, RAW sidecars |
| Metadata | MetadataExtractor | 2.9.3 | EXIF read/write |
| Storage | System.Data.SQLite.Core | 1.0.119 | VACUUM maintenance; single-file extract required |
| Testing | xUnit + Moq | 2.5.0 / 4.20.70 | `QuickMediaIngest.Tests/` |
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
| 2026-06-13 | Bootstrap parity | Template adoption in Reference mode | Split MainWindow/MainViewModel (Sprint 1) |
| 2026-06-20 | Template v0.11.0 migration | Phased bootstrap sync; slash commands + gate loop; WPF feature-gate | Confirm `/` menu in Cursor UI; push + CI green |
| 2026-06-21 | Release v1.3.17 | Human sign-off automation; P2–P8 backlog; MSI/libvips CI fixes | SBOM in build.yml; local `gh auth refresh -s security_events` for strict Dependabot gate |
| 2026-06-21 | Release v1.3.18 | Byte-weighted import progress + ETA (F-002) | LAN FTP smoke tests flaky offline |
| 2026-07-12 | Audit Sprint R2 | FtpPass purge + path collapse + crash redact; 127 tests; gates green | HUMAN: Dependabot #10/#7 merge; Scorecard failure |
| 2026-07-12 | R2 backlog D1–D3 | DecodedThumbnail Core; LogPathSanitizer; Update/Ingest/Keyword tests (144) | Separate Core csproj; FtpScanner/DeviceWatcher live tests |
| 2026-07-12 | Release v1.3.19 | Audit R2 + backlog shipped; CI green; build workflow_dispatch | SBOM in build.yml (AUTO-SBOM) |

## Template Provenance

- **Source template:** `edwardlthompson/agent-project-bootstrap`
- **Template version:** `0.11.0` (see `.template-version`)
- **Last update check:** See `.template-update.json`
