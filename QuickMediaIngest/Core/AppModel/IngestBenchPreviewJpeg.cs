#nullable enable
using System;
using System.IO;
using ImageMagick;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Thumbnails;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Selected-item preview JPEG at camera/source resolution (no grid FitMaxEdge).</summary>
    internal static class IngestBenchPreviewJpeg
    {
        public static string? Resolve(IngestBenchHost host, ImportItem item)
        {
            string? pulled = null;
            string path = item.SourcePath;
            if (!File.Exists(path))
            {
                pulled = AdbPreviewPull.TryMaterialize(host.Paths, item);
                path = pulled ?? string.Empty;
            }

            try
            {
                string? result = FromLocal(host.Paths, path);
                if (pulled is not null
                    && result is not null
                    && string.Equals(result, pulled, StringComparison.Ordinal))
                {
                    string dest = Path.Combine(IngestBenchThumbs.CacheDir(host.Paths), "full-preview.jpg");
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(pulled, dest, overwrite: true);
                    return dest;
                }

                return result;
            }
            finally
            {
                if (pulled is not null)
                {
                    AdbPreviewPull.TryDelete(pulled);
                }
            }
        }

        internal static string? FromLocal(IAppPaths paths, string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            string ext = Path.GetExtension(sourcePath);
            if (MediaExtensions.IsJpegFamily(ext))
            {
                return sourcePath;
            }

            string dest = Path.Combine(IngestBenchThumbs.CacheDir(paths), "full-preview.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            if (MediaExtensions.IsRawExtension(ext))
            {
                DecodedThumbnail? embed = RawEmbeddedPreviewReader.TryExtract(sourcePath);
                if (embed is not null && embed.JpegBytes.Length > 0)
                {
                    File.WriteAllBytes(dest, embed.JpegBytes);
                    return dest;
                }
            }

            if (MediaExtensions.IsVideoExtension(ext))
            {
                DecodedThumbnail? frame = FfmpegVideoThumbnailDecoder.TryGetThumbnail(sourcePath);
                if (frame is null || frame.JpegBytes.Length == 0)
                {
                    return null;
                }

                File.WriteAllBytes(dest, frame.JpegBytes);
                return dest;
            }

            try
            {
                using var image = new MagickImage(sourcePath);
                image.AutoOrient();
                image.Write(dest, MagickFormat.Jpeg);
                return dest;
            }
            catch
            {
                return null;
            }
        }
    }
}
