#nullable enable
using System;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Fills shoot preview paths (local files or ADB pull + Magick).</summary>
    internal static class IngestBenchThumbFill
    {
        public static ThumbFillResult Fill(IngestBenchHost host, int perGroup)
        {
            ItemGroup[] groups = host.Groups.ToArray();
            ImportItem[] items = groups.SelectMany(g => g.Items.ToArray()).ToArray();
            int total = Math.Max(1, items.Length);
            int done = 0;
            int ok = 0;
            int fail = 0;
            int lastPct = -1;

            void Apply(ImportItem item, string? cached)
            {
                string path = cached ?? string.Empty;
                host.Post(() => item.PreviewCachePath = path);
                if (cached is null)
                {
                    Interlocked.Increment(ref fail);
                }
                else
                {
                    Interlocked.Increment(ref ok);
                }

                Tick();
            }

            void Skip()
            {
                Tick();
            }

            void Tick()
            {
                int n = Interlocked.Increment(ref done);
                int pct = n * 100 / total;
                if (pct == lastPct)
                {
                    return;
                }

                lastPct = pct;
                IngestBenchActivity.Report(host, pct, "Thumbnails " + n + "/" + total);
            }

            Task local = Task.Run(() =>
            {
                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 2, 6),
                };
                Parallel.ForEach(items.Where(i => !IsPhone(i)).ToArray(), options, item =>
                    Apply(item, Decode(host, item)));
            });
            Task phones = Task.Run(() => FillPhones(host, groups, perGroup, Apply, Skip));
            Task.WaitAll(local, phones);
            IngestBenchThumbs.EnforceCap(host.Paths);
            return new ThumbFillResult(ok, fail);
        }

        public static string? Decode(IngestBenchHost host, ImportItem item, int maxEdge = 256)
        {
            string identity = IngestBenchThumbCache.Identity(item);
            string? cached = IngestBenchThumbCache.TryExisting(host.Paths, identity, maxEdge);
            if (cached is not null)
            {
                return cached;
            }

            if (!string.IsNullOrWhiteSpace(item.PreviewCachePath) && File.Exists(item.PreviewCachePath))
            {
                return item.PreviewCachePath;
            }

            if (File.Exists(item.SourcePath))
            {
                return IngestBenchThumbs.DecodeToCache(host.Paths, item.SourcePath, identity, maxEdge);
            }

            string? pulled = AdbPreviewPull.TryMaterialize(host.Paths, item);
            if (pulled is null)
            {
                return null;
            }

            try
            {
                return IngestBenchThumbs.DecodeToCache(host.Paths, pulled, identity, maxEdge);
            }
            finally
            {
                AdbPreviewPull.TryDelete(pulled);
            }
        }

        private static void FillPhones(
            IngestBenchHost host,
            ItemGroup[] groups,
            int perGroup,
            Action<ImportItem, string?> apply,
            Action skip)
        {
            Parallel.ForEach(
                groups,
                new ParallelOptions { MaxDegreeOfParallelism = 2 },
                group =>
                {
                    int adb = 0;
                    foreach (ImportItem item in group.Items.ToArray())
                    {
                        if (!IsPhone(item))
                        {
                            continue;
                        }

                        if (adb >= perGroup)
                        {
                            host.Post(() => item.PreviewCachePath = string.Empty);
                            skip();
                            continue;
                        }

                        string? cached = Decode(host, item);
                        apply(item, cached);
                        if (cached is not null)
                        {
                            adb++;
                        }
                    }
                });
        }

        private static bool IsPhone(ImportItem item) =>
            item.SourceId.StartsWith("adb:", StringComparison.OrdinalIgnoreCase);
    }
}
