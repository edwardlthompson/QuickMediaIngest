#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core
{
    internal sealed partial class FtpThumbnailPipeline
    {
        private async Task<DecodedThumbnail?> TryLoadSiblingPreviewAsync(
            FtpEndpoint endpoint,
            FtpThumbnailWorkItem workItem,
            string tempPath,
            ThumbnailHints? hints,
            bool useFluentFtp,
            SemaphoreSlim decodeGate,
            CancellationToken cancellationToken)
        {
            foreach (string siblingPath in GetRenderedSiblingRemotePaths(workItem.RemotePath, workItem.FileName))
            {
                DecodedThumbnail? thumb = await TryTieredDownloadAndDecodeAsync(
                    endpoint,
                    siblingPath,
                    Path.GetFileName(siblingPath),
                    tempPath,
                    hints,
                    useFluentFtp,
                    decodeGate,
                    cancellationToken);
                if (thumb != null)
                {
                    return thumb;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetRenderedSiblingRemotePaths(string remotePath, string fileName)
        {
            int slash = remotePath.LastIndexOf('/');
            string directory = slash >= 0 ? remotePath[..slash] : string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (string ext in new[] { ".heic", ".heif", ".jpg", ".jpeg" })
            {
                yield return string.IsNullOrEmpty(directory) ? "/" + baseName + ext : directory + "/" + baseName + ext;
            }
        }

        private static bool ShouldTryFullDownload(FtpThumbnailWorkItem workItem)
        {
            string ext = Path.GetExtension(workItem.FileName);
            if (MediaExtensions.IsVideoExtension(ext))
            {
                return false;
            }

            return workItem.FileSize <= 0 || workItem.FileSize <= 25 * 1024 * 1024;
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
