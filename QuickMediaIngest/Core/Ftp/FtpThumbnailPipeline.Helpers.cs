#nullable enable
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private IAdbPathProbe? _adbPathProbe;
        private readonly ConcurrentDictionary<string, bool> _adbExistsCache = new();

        private void LogThumbnailTransport(int itemCount)
        {
            if (_adbSession is { } preferAdb)
            {
                _logger.LogInformation(
                    "Thumbnail batch transport: PreferAdb ADB ({Serial}) with FTP fallback for {Count} item(s).",
                    preferAdb.DeviceSerial,
                    itemCount);
            }
            else
            {
                _logger.LogInformation(
                    "Thumbnail batch transport: FTP for {Count} item(s).",
                    itemCount);
            }
        }

        private async Task<(DecodedThumbnail? Thumb, bool ViaAdb)> TryLoadSiblingPreviewAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            foreach (string siblingPath in FtpMediaPathNormalizer.GetRenderedSiblingRemotePaths(
                         workItem.RemotePath, workItem.FileName))
            {
                if (await ShouldSkipMissingRemoteAsync(endpoint, siblingPath, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                (DecodedThumbnail? thumb, bool viaAdb) = await TryTieredDownloadAndDecodeAsync(
                    endpoint,
                    siblingPath,
                    Path.GetFileName(siblingPath),
                    knownFileSize: 0,
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (thumb != null)
                {
                    return (thumb, viaAdb);
                }
            }

            return (null, false);
        }

        /// <summary>
        /// When PreferAdb session + probe are set, skip paths that do not exist on the device
        /// (avoids FTP 550 storms for phantom .heif/.jpg siblings).
        /// </summary>
        private async Task<bool> ShouldSkipMissingRemoteAsync(
            FtpEndpoint endpoint,
            string remotePath,
            CancellationToken cancellationToken)
        {
            if (_adbSession is not { } adb || _adbPathProbe is null)
            {
                return false;
            }

            string devicePath = AdbAndroidPath.ToDevicePath(adb.MediaRootPrefix, remotePath);
            string cacheKey = $"{adb.DeviceSerial}|{devicePath}";
            if (_adbExistsCache.TryGetValue(cacheKey, out bool cached))
            {
                return !cached;
            }

            bool exists = await Task.Run(
                    () => _adbPathProbe.FileExists(adb.DeviceSerial, devicePath),
                    cancellationToken)
                .ConfigureAwait(false);
            _adbExistsCache[cacheKey] = exists;
            if (!exists)
            {
                FtpPermanentFailureCache.MarkFailed(endpoint.Host, endpoint.Port, remotePath);
            }

            return !exists;
        }

        private static bool ShouldTryFullDownload(FtpThumbnailWorkItem workItem)
        {
            string ext = Path.GetExtension(workItem.FileName);
            if (MediaExtensions.IsVideoExtension(ext))
            {
                return FtpPreviewDownloadLimits.ShouldTryVideoCompleteFallback(workItem.FileSize);
            }

            // Stills up to 40MB.
            const long stillLimit = 40L * 1024 * 1024;
            return workItem.FileSize <= 0 || workItem.FileSize <= stillLimit;
        }

        private static int GetThumbnailPriority(FtpThumbnailWorkItem item)
        {
            string ext = Path.GetExtension(item.FileName).ToLowerInvariant();
            if (ext is ".heic" or ".heif" or ".jpg" or ".jpeg" or ".png")
            {
                return 0;
            }

            if (MediaExtensions.IsVideoExtension(ext))
            {
                return 2;
            }

            if (MediaExtensions.IsRawExtension(ext))
            {
                return 3;
            }

            return 1;
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore temp cleanup failures.
            }
        }
    }
}
