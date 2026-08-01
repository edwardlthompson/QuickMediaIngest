#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>Capped ADB preview fetch: exec-out dd via <c>sh -c</c>, then size-capped pull.</summary>
    public sealed partial class AdbPreviewFetcher : IAdbPreviewFetcher
    {
        private static readonly TimeSpan WallTimeout = TimeSpan.FromSeconds(45);
        private readonly ILogger<AdbPreviewFetcher> _logger;

        public AdbPreviewFetcher(ILogger<AdbPreviewFetcher> logger)
        {
            _logger = logger;
        }

        public async Task<bool> TryFetchCappedAsync(
            AdbTransferSession session,
            string ftpRemotePath,
            string localPath,
            long maxBytes,
            long knownFileSize,
            CancellationToken cancellationToken)
        {
            string devicePath = AdbAndroidPath.ToDevicePath(session.MediaRootPrefix, ftpRemotePath);
            TryDelete(localPath);

            long blocks = Math.Max(1, (maxBytes + 65535) / 65536);
            string escaped = devicePath.Replace("'", "'\\''", StringComparison.Ordinal);
            string ddArgs =
                $"exec-out sh -c \"dd if='{escaped}' bs=65536 count={blocks} 2>/dev/null\"";

            try
            {
                // Prefer full pull when the remote file fits the budget (complete HEIC/JPEG).
                if (knownFileSize > 0 && knownFileSize <= maxBytes)
                {
                    bool pulled = await RunAdbAsync(
                            session.DeviceSerial,
                            $"pull \"{devicePath}\" \"{localPath}\"",
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (pulled && LooksLikeMediaPayload(localPath))
                    {
                        return true;
                    }

                    TryDelete(localPath);
                }

                if (await TryExecOutToFileAsync(session.DeviceSerial, ddArgs, localPath, maxBytes, cancellationToken)
                        .ConfigureAwait(false) &&
                    LooksLikeMediaPayload(localPath))
                {
                    return true;
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ADB preview fetch failed for {Path}.", devicePath);
                return false;
            }
        }

        /// <summary>Rejects dd/stderr text that adb sometimes writes to stdout.</summary>
        internal static bool LooksLikeMediaPayload(string localPath)
        {
            try
            {
                if (!File.Exists(localPath))
                {
                    return false;
                }

                var info = new FileInfo(localPath);
                if (info.Length < 12)
                {
                    return false;
                }

                Span<byte> header = stackalloc byte[12];
                using var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                int read = fs.Read(header);
                if (read < 12)
                {
                    return false;
                }

                // JPEG
                if (header[0] == 0xFF && header[1] == 0xD8)
                {
                    return true;
                }

                // PNG
                if (header[0] == 0x89 && header[1] == (byte)'P' && header[2] == (byte)'N' && header[3] == (byte)'G')
                {
                    return true;
                }

                // ISO BMFF (HEIC/HEIF/AVIF/MP4) — ftyp at offset 4
                if (header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p')
                {
                    return true;
                }

                // TIFF / DNG
                if ((header[0] == 0x49 && header[1] == 0x49) || (header[0] == 0x4D && header[1] == 0x4D))
                {
                    return true;
                }

                // dd error text often starts with "dd:"
                if (header[0] == (byte)'d' && header[1] == (byte)'d' && header[2] == (byte)':')
                {
                    return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
