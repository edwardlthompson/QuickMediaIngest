# Quick Media Ingest — Architectural Plan & Roadmap

## 1. System Architecture

```
                                  +---------------------------------------+
                                  |         WPF UI Layer (MVVM)           |
                                  |  - MainWindow / Overlays / Controls   |
                                  |  - MainViewModel & Partials           |
                                  +---------------------------------------+
                                                     |
                                                     v
+---------------------------------------------------------------------------------------------------------+
|                                    QuickMediaIngest.Core (Business Logic)                               |
+---------------------------------------------------------------------------------------------------------+
|  +--------------------+   +---------------------+   +---------------------+   +----------------------+  |
|  |     Transports     |   |   Group & Naming    |   |  Previews & Codecs  |   |   Ingest Pipeline    |  |
|  | - LocalScanner     |   | - GroupBuilder      |   | - VipsDecoder       |   | - IngestProcessor    |  |
|  | - FtpScanner       |   | - GroupFolderNaming |   | - HeifPreviewReader |   | - ManifestWriter     |  |
|  | - AdbMediaScanner  |   | - TimeZoneAdjuster  |   | - VideoProxyGen     |   | - SpaceEstimator     |  |
|  | - PortableDevices  |   | - CullPersistence   |   | - ThumbnailCache    |   | - CollisionAnalyzer  |  |
|  +--------------------+   +---------------------+   +---------------------+   +----------------------+  |
|  +--------------------+   +---------------------+   +---------------------+   +----------------------+  |
|  |  Security & Privacy|   |  Localization/Theme |   |     Crash Capture   |   |   Catalog & Storage  |  |
|  | - EncryptionDetect |   | - AppLocalizer      |   | - CrashCaptureSvc   |   | - ImportHashCatalog  |  |
|  | - PII Sanitizer    |   | - SystemThemeDetect |   | - PendingCrashStore |   | - WatchFolderService |  |
|  +--------------------+   +---------------------+   +---------------------+   +----------------------+  |
+---------------------------------------------------------------------------------------------------------+
```

## 2. Ingest Lifecycle Pipeline

1. **Discovery & Probe:** Unified scan across selected physical drives, FTP shares, or connected ADB devices.
2. **De-duplication & Alias Filtering:** `FtpAdbAliasFilter` strips virtual duplicates across simultaneous mounts; `ImportHashCatalog` flags previously imported items.
3. **Chronological Clustering:** `GroupBuilder` segments items into shoots with configurable time-gap limits and token-driven naming.
4. **Pre-flight & Verification:** Free space forecast calculates required bytes on destination roots; collision analysis generates conflict reports.
5. **Execution & Dual-Copy:** Single-worker or throttled copy to primary target directory; concurrent write to secondary 3-2-1 backup root.
6. **Post-Processing & Manifest:** SHA-256 shoot checksum manifest (`manifest.sha256`) generation; safe media eject if all files imported.

## 3. Engineering Invariants & Quality Standards

- **Zero-Unsafe Core:** All business logic lives in `QuickMediaIngest.Core` with `#nullable enable` and 100% automated test coverage.
- **Strict FOSS Compliance:** Pure MIT licensing with no proprietary cloud analytics, trackers, or non-free SDKs in the production path.
- **Continuous Gate Validation:** Full multi-platform test pass (`dotnet test -c Release`), repo hygiene check, and UTF-8 encoding verification on every release candidate.
