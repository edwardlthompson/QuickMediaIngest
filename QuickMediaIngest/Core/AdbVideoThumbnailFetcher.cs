#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    /// <summary>PreferAdb video grid thumbs via MediaStore JPEG (Explorer/MTP economics).</summary>
    public sealed partial class AdbVideoThumbnailFetcher : IAdbVideoThumbnailFetcher
    {
        private static readonly TimeSpan WallTimeout = TimeSpan.FromSeconds(30);
        private static readonly Regex IdRowRegex = new(@"_id=(\d+)", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static readonly Regex DataPathRegex = new(@"_data=([^\r\n,]+)", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private readonly ILogger<AdbVideoThumbnailFetcher> _logger;

        public AdbVideoThumbnailFetcher(ILogger<AdbVideoThumbnailFetcher> logger)
        {
            _logger = logger;
        }

        public async Task<bool> TryFetchVideoThumbJpegAsync(
            AdbTransferSession session,
            string ftpRemotePath,
            string localJpegPath,
            CancellationToken cancellationToken)
        {
            try
            {
                TryDelete(localJpegPath);
                string devicePath = AdbAndroidPath.ToDevicePath(session.MediaRootPrefix, ftpRemotePath);
                string fileName = Path.GetFileName(devicePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return false;
                }

                long? videoId = await ResolveVideoIdAsync(session.DeviceSerial, fileName, devicePath, cancellationToken)
                    .ConfigureAwait(false);
                if (videoId is not long id)
                {
                    return false;
                }

                if (await TryReadContentThumbnailAsync(session.DeviceSerial, id, localJpegPath, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return true;
                }

                string? thumbPath = await ResolveThumbnailFilePathAsync(session.DeviceSerial, id, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(thumbPath) &&
                    await PullFileAsync(session.DeviceSerial, thumbPath!, localJpegPath, cancellationToken)
                        .ConfigureAwait(false) &&
                    LooksLikeJpeg(localJpegPath))
                {
                    return true;
                }

                TryDelete(localJpegPath);
                return false;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "ADB video thumbnail fetch failed for {Path}.", ftpRemotePath);
                TryDelete(localJpegPath);
                return false;
            }
        }

        private async Task<long?> ResolveVideoIdAsync(
            string serial,
            string fileName,
            string devicePath,
            CancellationToken cancellationToken)
        {
            string escapedName = fileName.Replace("'", "''", StringComparison.Ordinal);
            string output = await RunAdbShellAsync(
                    serial,
                    "content query --uri content://media/external/video/media " +
                    $"--projection _id:_display_name:_data --where \"_display_name='{escapedName}'\"",
                    cancellationToken)
                .ConfigureAwait(false);

            Match idMatch = IdRowRegex.Match(output);
            if (idMatch.Success && long.TryParse(idMatch.Groups[1].Value, out long id))
            {
                return id;
            }

            string escapedPath = devicePath.Replace("'", "''", StringComparison.Ordinal);
            output = await RunAdbShellAsync(
                    serial,
                    "content query --uri content://media/external/video/media " +
                    $"--projection _id:_data --where \"_data='{escapedPath}'\"",
                    cancellationToken)
                .ConfigureAwait(false);

            idMatch = IdRowRegex.Match(output);
            return idMatch.Success && long.TryParse(idMatch.Groups[1].Value, out id) ? id : null;
        }

        private async Task<bool> TryReadContentThumbnailAsync(
            string serial,
            long videoId,
            string localJpegPath,
            CancellationToken cancellationToken)
        {
            string[] uris =
            {
                $"content://media/external/video/media/{videoId}/thumbnail",
                $"content://media/external_primary/video/media/{videoId}/thumbnail",
            };

            foreach (string uri in uris)
            {
                TryDelete(localJpegPath);
                bool ok = await RunAdbExecOutToFileAsync(
                        serial,
                        $"content read --uri {uri}",
                        localJpegPath,
                        maxBytes: 2 * 1024 * 1024,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (ok && LooksLikeJpeg(localJpegPath))
                {
                    return true;
                }

                TryDelete(localJpegPath);
            }

            return false;
        }

        private async Task<string?> ResolveThumbnailFilePathAsync(
            string serial,
            long videoId,
            CancellationToken cancellationToken)
        {
            string output = await RunAdbShellAsync(
                    serial,
                    "content query --uri content://media/external/video/thumbnails " +
                    $"--projection video_id:_data:kind --where \"video_id={videoId}\"",
                    cancellationToken)
                .ConfigureAwait(false);

            Match data = DataPathRegex.Match(output);
            if (!data.Success)
            {
                return null;
            }

            string path = data.Groups[1].Value.Trim();
            return path.StartsWith('/') && path.Length > 1 ? path : null;
        }
    }
}
