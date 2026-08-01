#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Portable <c>adb shell find -type f</c> media listing.</summary>
    public sealed partial class AdbMediaScanner : IAdbMediaScanner
    {
        private static readonly TimeSpan WallTimeout = TimeSpan.FromSeconds(60);
        private readonly ILogger<AdbMediaScanner> _logger;

        public AdbMediaScanner(ILogger<AdbMediaScanner> logger)
        {
            _logger = logger;
        }

        public async Task<List<ImportItem>?> ScanAsync(
            AdbTransferSession session,
            string ftpRemoteFolder,
            bool includeSubfolders,
            CancellationToken cancellationToken = default)
        {
            if (!AdbAndroidPath.TryNormalizeRemote(ftpRemoteFolder, out string folder))
            {
                return null;
            }

            string deviceRoot = AdbAndroidPath.ToDevicePath(session.MediaRootPrefix, folder);
            string escaped = deviceRoot.Replace("'", "'\\''", StringComparison.Ordinal);

            try
            {
                // Prefer find+stat so ImportItem.FileSize is set (enables size-capped ADB pull).
                // Use '|' — Android toybox stat -c does not expand \t escapes.
                string? output = await RunAdbCaptureAsync(
                        session.DeviceSerial,
                        $"shell find '{escaped}' -type f -exec stat -c '%n|%s' {{}} +",
                        cancellationToken)
                    .ConfigureAwait(false);

                List<ImportItem>? items = null;
                if (output != null)
                {
                    items = ParseFindOutput(session.MediaRootPrefix, folder, includeSubfolders, output, cancellationToken);
                }

                if (items == null || items.Count == 0)
                {
                    output = await RunAdbCaptureAsync(
                            session.DeviceSerial,
                            $"shell find '{escaped}' -type f",
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (output == null)
                    {
                        _logger.LogWarning("ADB scan failed for {Root} (find error).", deviceRoot);
                        return null;
                    }

                    items = ParseFindOutput(session.MediaRootPrefix, folder, includeSubfolders, output, cancellationToken);
                }

                if (items.Count == 0)
                {
                    _logger.LogWarning(
                        "ADB scan returned 0 media files under {Root}; falling back to FTP.",
                        deviceRoot);
                    return null;
                }

                int sized = 0;
                foreach (ImportItem item in items)
                {
                    if (item.FileSize > 0)
                    {
                        sized++;
                    }
                }

                _logger.LogInformation(
                    "ADB scan listed {Count} media files under {Root} ({Sized} with sizes).",
                    items.Count,
                    deviceRoot,
                    sized);
                return items;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ADB scan exception for {Root}: {Message}", deviceRoot, ex.Message);
                _logger.LogDebug(ex, "ADB scan detail for {Root}.", deviceRoot);
                return null;
            }
        }
    }
}
