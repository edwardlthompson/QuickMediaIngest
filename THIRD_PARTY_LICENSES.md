# Third-Party Licenses

> Attribution for NuGet dependencies bundled in Quick Media Ingest distributions.
> Review before each release. See `QuickMediaIngest/QuickMediaIngest.csproj` for pinned versions.

| Package | License | Notes |
|---------|---------|-------|
| CommunityToolkit.Mvvm | MIT | MVVM helpers |
| Magick.NET-Q16-AnyCPU | Apache-2.0 | Image processing; verify ImageMagick attribution |
| Meziantou.Framework.Win32.CredentialManager | MIT | Windows Credential Manager wrapper |
| MaterialDesignThemes | MIT | UI theme |
| Microsoft.Extensions.DependencyInjection | MIT | DI container |
| Microsoft.Extensions.Logging | MIT | Logging abstractions |
| FluentFTP | MIT | FTP client |
| MetadataExtractor | Apache-2.0 | EXIF/metadata |
| System.Data.SQLite.Core | Public Domain (SQLite) | Local database |
| System.Management | MIT | WMI for device detection |

## Test Dependencies

| Package | License |
|---------|---------|
| Microsoft.NET.Test.Sdk | MIT |
| xunit | Apache-2.0 |
| xunit.runner.visualstudio | Apache-2.0 |
| Moq | BSD-3-Clause |

## License Compliance

Allowed licenses for production dependencies: MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC, Public Domain.

Run `scripts/check-license-compliance.sh dotnet` before release.

## ImageMagick Notice

Magick.NET bundles ImageMagick. Distribution artifacts may require additional notices per ImageMagick license. Verify before MSI/portable release.
