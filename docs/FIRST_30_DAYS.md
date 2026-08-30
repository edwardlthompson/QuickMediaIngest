# Quick Media Ingest — First 30 Days Playbook

## Week 1: Environment & Baseline Verification

1. **Local Build & Portable EXE Test:**
   - Execute `build_local_test.bat` or `dotnet build -c Release` to verify local compiler toolchains.
   - Run `publish\local-test\QuickMediaIngest.exe` to verify cold-start configuration generation in `%AppData%\QuickMediaIngest\config.json`.
2. **Automated Smoke Pass:**
   - Run `powershell -ExecutionPolicy Bypass -File .\scripts\run-human-signoffs.ps1 -PublishedExe`.
   - Verify all 300+ unit and integration tests pass cleanly with `dotnet test -c Release --no-build`.
3. **Hardware Storage Media Validation:**
   - Test with real UHS-I/II SD card readers and Android/iOS USB MTP connections.
   - Verify non-destructive pre-flight check, free space forecasting, and single-worker flash drive copy throttle.

## Week 2: Workflow Hardening & Golden Path Verification

1. **Wi-Fi Camera Profile Ingest:**
   - Configure Sony/Canon/Nikon FTP profiles and run simulated or live transfers.
   - Verify background thumbnail cache generation and XMP creator/copyright sidecar attachment.
2. **Crash & Feedback Loop:**
   - Verify opt-in feedback dialog (F9 / Help menu) opens sanitized bug/feature URL templates on GitHub without leaking PII.
   - Discarded crash fingerprints are permanently ignored in `discarded-crashes.json`.

## Weeks 3 & 4: Production Packaging & Release

1. **Pre-Release Gate Checks:**
   - Run `bash scripts/pre-release-gate.sh` to confirm FOSS licensing, Gitleaks clean status, and artifact hygiene.
2. **WiX MSI & Portable Distribution:**
   - Generate release tags via GitHub Actions workflow (`.github/workflows/build.yml`) and verify SHA-256 asset checksums.
