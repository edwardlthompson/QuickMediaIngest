# Quick Media Ingest — Product Specification

## 1. Executive Summary

Quick Media Ingest is a high-throughput, cross-transport desktop media ingestion application designed for professional photographers and videographers. It automates media discovery, grouping, metadata attachment, preview generation, and safe verified file transfers across local flash cards (SD/CFexpress), network camera Wi-Fi/FTP endpoints, and Android/iOS mobile devices over USB (ADB and Windows Portable Devices / WPD).

## 2. Target Workflows & Personas

- **Event & Studio Photographers:** Offloading multi-gigabyte RAW (.ARW, .CR3, .NEF) and JPEG/HEIF assets from UHS-II SD cards or tethered cameras into shoot-based folders with automated date/time grouping, creator copyright stamps, and XMP sidecar generation.
- **Mobile Journalists & Creators:** Ingesting mixed photos and 4K/8K video footage from smartphones via direct USB ADB pull or camera Wi-Fi FTP without requiring cloud uploads or slow manual file manager copying.

## 3. Architecture & Tech Stack

- **Target Framework:** .NET 8.0 Windows (WPF MVVM Architecture with `CommunityToolkit.Mvvm`).
- **Data & Core Logic:** `QuickMediaIngest/Core/` containing pure logic with zero UI/WPF dependencies.
- **Image & RAW Decoding:** Prioritized decode pipeline: embedded JPEG/preview extraction -> `NetVips` / `libvips` shrink-on-load -> Windows Imaging Component (WIC) -> `Magick.NET` (fallback).
- **Metadata Management:** IPTC-IIM, EXIF tag preservation/stripping, and XMP sidecar synthesis (`MetadataKeywordWriter.cs`).
- **Transport Layers:**
  - Local filesystem I/O (`LocalScanner`, `RemovableDriveIo` with removable flash throttle).
  - FTP/FTPS Client (`FtpScanner`, `FtpBandwidthThrottler`, `FtpTieredPreviewDecoder`).
  - Android Debug Bridge (`AdbMediaScanner`, `AdbTransferSession`, `AdbFileProvider`).
  - Portable Devices (`PortableDeviceScanner`, `PtpTetherScanner`).

## 4. Key Functional Capabilities

1. **Intelligent Shoot Grouping:** Automatically clusters media into distinct shoots based on adjustable time gap thresholds (`GroupBuilder.cs`), folder naming patterns (`GroupFolderNaming.cs`), and customizable template tokens (`[Date]`, `[ShootTitle]`, `[Job]`, `[Client]`, `[Camera]`).
2. **Review & Pre-Cull:** Grid-based visual culling with Star Ratings (1-5), Color Labels, Pick/Reject flags, and side-by-side comparison state (`SideBySideComparisonState.cs`).
3. **Safe Ingestion & Verification:**
   - Pre-flight dry-run simulation (`IsDryRun`).
   - Free space forecasting (`ImportDestinationEstimator.cs`).
   - Pre-copy collision analysis (`IngestCollisionAnalyzer.cs`).
   - Strict checksum / SHA-256 manifest generation (`ShootChecksumManifestWriter.cs`).
   - Dual-copy 3-2-1 secondary backup destination support.
   - Non-destructive safety interlocks on Delete-After-Import and automated volume ejection (`RemovableDriveIo.TryEjectVolume`).
4. **Privacy & Crash Resilience:**
   - Client-side PII redaction and path sanitization (`PrivacyReportSanitize.cs`).
   - Opt-in local crash review queue (`CrashCaptureService.cs`) with persistent discard fingerprint filtering.
   - Offline-capable GitHub issue and feedback composition.
