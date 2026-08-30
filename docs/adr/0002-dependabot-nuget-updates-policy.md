# ADR-0002: Dependabot and Magick.NET/NuGet Weekly Updates Policy

**Status:** Accepted (2026-08-30)

## Context

Quick Media Ingest relies on native and managed dependencies such as `Magick.NET-Q16-AnyCPU`, `FluentFTP`, `CommunityToolkit.Mvvm`, `MaterialDesignThemes`, and `MetadataExtractor`.

Automated dependency tools (such as GitHub Dependabot) regularly propose version bumps. Blindly applying major or native dependency bumps can introduce breaking ABI changes, performance regressions in RAW thumbnail decoding, or bundled native library incompatibility across Windows architectures (x64/x86/ARM64).

## Decision

1. **Patch and Minor NuGet Updates:** Allowed for automated or weekly evaluation provided all local gate suites and unit tests pass (`dotnet test`).
2. **Magick.NET Version Pinning & Testing:**
   - Magick.NET bumps must be tested with `VipsRawThumbnailDecoderTests`, `MetadataKeywordWriterGpsTests`, and `MagickThumbnailDecoder` tests.
   - Any version with known security advisories (e.g. GHSA / CVE-2026-64685) must be updated to the verified safe release (>= 14.16.0).
   - Magick.NET major upgrades require manual smoke verification on Windows x64.
3. **Never Blind-Bump:**
   - Major releases of UI frameworks (`MaterialDesignThemes`, `CommunityToolkit.Mvvm`).
   - Native wrappers where runtime dynamic library loading (`Magick.Native-*.dll`) could break standalone portable single-file extraction.
4. **Local-First Dependency Auditing:**
   - Run `python3 scripts/agent-run.py update-deps` (dry-run first) to check outdated packages before applying patch/minor updates.

## Consequences

- Dependency vulnerabilities are promptly addressed without breaking portable desktop stability.
- Magick.NET remains safe and stable across all supported camera RAW formats.
