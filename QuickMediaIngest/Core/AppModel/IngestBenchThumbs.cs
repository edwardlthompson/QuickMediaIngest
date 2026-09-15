#nullable enable
using System;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Core.Thumbnails;

namespace QuickMediaIngest.Core.AppModel
{
    public readonly record struct ThumbFillResult(int Succeeded, int Failed);

    /// <summary>Magick/Vips/HEIC/ffmpeg preview cache with a byte cap (no WIC).</summary>
    public static class IngestBenchThumbs
    {
        public const long DefaultCapBytes = 256L * 1024 * 1024;

        public static string CacheDir(IAppPaths paths) => Path.Combine(paths.AppDataRoot, "thumbs");

        public static void NoteIcc(IngestBenchHost host) =>
            host.Model.IccProfilePath = new ColorManagementProfileService().GetSystemDefaultIccProfilePath() ?? string.Empty;

        public static void Refresh(IngestBenchHost host)
        {
            ImportItem? item = host.Model.SelectedCullItem
                ?? host.Groups.SelectMany(g => g.Items)
                    .FirstOrDefault(i => MediaExtensions.IsMediaExtension(Path.GetExtension(i.FileName)));
            host.Model.PreviewPath = item is null
                ? string.Empty
                : FullJpeg(host, item) ?? string.Empty;
        }

        public static string? FullJpeg(IngestBenchHost host, ImportItem item) =>
            IngestBenchPreviewJpeg.Resolve(host, item);

        public static ThumbFillResult FillShootPreviews(IngestBenchHost host, int perGroup = 24) =>
            IngestBenchThumbFill.Fill(host, perGroup);

        public static string? DecodeItem(IngestBenchHost host, ImportItem item, int maxEdge = 256) =>
            IngestBenchThumbFill.Decode(host, item, maxEdge);

        public static void CompareSelected(IngestBenchHost host)
        {
            ImportItem[] items = host.Groups.SelectMany(g => g.Items).ToArray();
            ImportItem? left = host.Model.SelectedCullItem ?? items.FirstOrDefault();
            ImportItem? right = items.SkipWhile(i => i != left).Skip(1).FirstOrDefault() ?? items.FirstOrDefault();
            host.Model.CompareLeftPath = left is null ? string.Empty : DecodeItem(host, left) ?? string.Empty;
            host.Model.CompareRightPath = right is null ? string.Empty : DecodeItem(host, right) ?? string.Empty;
            host.Model.ShowCompare = items.Length > 0;
        }

        public static void Purge(IngestBenchHost host)
        {
            try
            {
                if (Directory.Exists(CacheDir(host.Paths)))
                {
                    Directory.Delete(CacheDir(host.Paths), recursive: true);
                }
            }
            catch
            {
                // best effort
            }

            host.Model.PreviewPath = string.Empty;
            host.Model.CompareLeftPath = string.Empty;
            host.Model.CompareRightPath = string.Empty;
            host.Model.ShowCompare = false;
        }

        public static void ForgetImported(IngestBenchHost host, IEnumerable<ImportItem> items)
        {
            var failed = new HashSet<string>(host.FailedPaths, StringComparer.OrdinalIgnoreCase);
            IngestBenchThumbCache.Forget(host.Paths, items.Where(i => !failed.Contains(i.SourcePath)));
            if (!string.IsNullOrWhiteSpace(host.Model.PreviewPath) && !File.Exists(host.Model.PreviewPath))
            {
                host.Model.PreviewPath = string.Empty;
            }
        }

        public static string? DecodeToCache(IAppPaths paths, string sourcePath, int maxEdge = 256) =>
            DecodeToCache(paths, sourcePath, IngestBenchThumbCache.FileIdentity(sourcePath), maxEdge);

        public static string? DecodeToCache(IAppPaths paths, string sourcePath, string identity, int maxEdge)
        {
            string? existing = IngestBenchThumbCache.TryExisting(paths, identity, maxEdge);
            if (existing is not null)
            {
                return existing;
            }

            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                return null;
            }

            try
            {
                if (new FileInfo(sourcePath).Length < 64)
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }

            string ext = Path.GetExtension(sourcePath);
            DecodedThumbnail? decoded = null;
            if (MediaExtensions.IsRawExtension(ext))
            {
                decoded = RawEmbeddedPreviewReader.TryExtract(sourcePath);
            }

            decoded ??= MagickThumbnailDecoder.TryGetThumbnail(sourcePath, maxEdge);
            if (decoded is null && !MediaExtensions.IsVideoExtension(ext))
            {
                decoded = VipsThumbnailDecoder.TryGetThumbnail(sourcePath, maxEdge);
            }

            if (decoded is null && MediaExtensions.IsHeifFamily(ext))
            {
                decoded = HeifPreviewDecoder.TryDecode(sourcePath, maxEdge);
            }

            if (decoded is null && MediaExtensions.IsVideoExtension(ext))
            {
                decoded = FfmpegVideoThumbnailDecoder.TryGetThumbnail(sourcePath);
            }

            if (decoded is null || decoded.JpegBytes.Length == 0)
            {
                return null;
            }

            decoded = MagickThumbnailDecoder.FitMaxEdge(decoded, maxEdge);

            Directory.CreateDirectory(CacheDir(paths));
            string dest = IngestBenchThumbCache.JpegPath(paths, identity, maxEdge);
            File.WriteAllBytes(dest, decoded.JpegBytes);
            return dest;
        }

        private static readonly object CapLock = new();

        public static void EnforceCap(IAppPaths paths, long capBytes = DefaultCapBytes)
        {
            lock (CapLock)
            {
                TrimCache(paths, capBytes);
            }
        }

        private static void TrimCache(IAppPaths paths, long capBytes)
        {
            string dir = CacheDir(paths);
            if (!Directory.Exists(dir))
            {
                return;
            }

            FileInfo[] files = new DirectoryInfo(dir).GetFiles()
                .Where(f => !f.Name.Equals("full-preview.jpg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.LastWriteTimeUtc)
                .ToArray();
            long total = files.Sum(f => f.Length);
            foreach (FileInfo file in files)
            {
                if (total <= capBytes)
                {
                    return;
                }

                total -= file.Length;
                try
                {
                    file.Delete();
                }
                catch
                {
                    // best effort
                }
            }
        }
    }
}
