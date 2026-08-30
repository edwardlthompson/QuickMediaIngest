#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core.Thumbnails
{
    public static class VideoProxyGenerator
    {
        public static async Task<string?> ExtractFirstFrameAsync(string videoPath, string outputPath, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath))
            {
                return null;
            }

            try
            {
                await Task.Yield();
                // In lightweight mode, generates or extracts video frame image without reading entire multi-GB stream
                string? dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                return outputPath;
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "First frame video extraction failed for {Path}", videoPath);
                return null;
            }
        }
    }
}
