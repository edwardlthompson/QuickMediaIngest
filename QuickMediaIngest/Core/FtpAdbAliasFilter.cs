#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core
{
    /// <summary>Drops OP13-style FTP LIST aliases that are not real files when ADB can validate.</summary>
    public static class FtpAdbAliasFilter
    {
        private static readonly ConcurrentDictionary<string, bool> ExistsCache =
            new(StringComparer.OrdinalIgnoreCase);

        public static void ClearSessionCache() => ExistsCache.Clear();

        public static async Task<List<ImportItem>> FilterAsync(
            IReadOnlyList<ImportItem> items,
            AdbTransferSession session,
            IAdbPathProbe pathProbe,
            CancellationToken cancellationToken = default)
        {
            if (items.Count == 0)
            {
                return items.ToList();
            }

            var byStem = items
                .GroupBy(i => Path.GetFileNameWithoutExtension(i.FileName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var candidates = new List<ImportItem>();
            foreach (ImportItem item in items)
            {
                string ext = Path.GetExtension(item.FileName).ToLowerInvariant();
                string stem = Path.GetFileNameWithoutExtension(item.FileName);
                if (!byStem.TryGetValue(stem, out var group))
                {
                    continue;
                }

                bool hasHeic = group.Any(i =>
                    Path.GetExtension(i.FileName).Equals(".heic", StringComparison.OrdinalIgnoreCase));
                bool hasRawOrHeic = group.Any(i =>
                {
                    string e = Path.GetExtension(i.FileName);
                    return MediaExtensions.IsRawExtension(e) ||
                           e.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                           e.Equals(".heif", StringComparison.OrdinalIgnoreCase);
                });

                if (ext == ".heif" && hasHeic)
                {
                    candidates.Add(item);
                }
                else if ((ext is ".jpg" or ".jpeg") && hasRawOrHeic)
                {
                    candidates.Add(item);
                }
            }

            if (candidates.Count == 0)
            {
                return items.ToList();
            }

            using var gate = new SemaphoreSlim(4, 4);
            var missing = new ConcurrentBag<string>();

            await Parallel.ForEachAsync(
                candidates,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = cancellationToken
                },
                async (item, ct) =>
                {
                    await gate.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        string devicePath = AdbAndroidPath.ToDevicePath(session.MediaRootPrefix, item.SourcePath);
                        string cacheKey = $"{session.DeviceSerial}|{devicePath}";
                        if (!ExistsCache.TryGetValue(cacheKey, out bool exists))
                        {
                            exists = await Task.Run(() => pathProbe.FileExists(session.DeviceSerial, devicePath), ct)
                                .ConfigureAwait(false);
                            ExistsCache[cacheKey] = exists;
                        }

                        if (!exists)
                        {
                            missing.Add(item.SourcePath);
                        }
                    }
                    finally
                    {
                        gate.Release();
                    }
                }).ConfigureAwait(false);

            if (missing.IsEmpty)
            {
                return items.ToList();
            }

            return items.Where(i => !missing.Contains(i.SourcePath)).ToList();
        }
    }
}
