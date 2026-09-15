#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Local vs per-serial ADB providers so mixed shoots still pull from the right phone.</summary>
    internal static class IngestBenchImportCopy
    {
        public static string SourceKey(ImportItem item)
        {
            string id = item.SourceId ?? string.Empty;
            if (id.StartsWith("adb:", StringComparison.OrdinalIgnoreCase) && id.Length > 4)
            {
                return "adb:" + id[4..];
            }

            return "local";
        }

        public static IFileProvider Create(string sourceKey)
        {
            if (sourceKey.StartsWith("adb:", StringComparison.OrdinalIgnoreCase) && sourceKey.Length > 4)
            {
                return new AdbFileProvider(sourceKey[4..], NullLogger<AdbFileProvider>.Instance);
            }

            return new TrashingFileProvider(
                new LocalFileProvider(NullLogger<LocalFileProvider>.Instance),
                new GioTrashService());
        }

        public static List<ItemGroup> SplitBySource(ItemGroup group)
        {
            var map = new Dictionary<string, ItemGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (ImportItem item in group.Items.Where(i => i.IsSelected))
            {
                string key = SourceKey(item);
                if (!map.TryGetValue(key, out ItemGroup? slice))
                {
                    slice = new ItemGroup
                    {
                        Title = group.Title,
                        StartDate = group.StartDate,
                        EndDate = group.EndDate,
                        KeywordsText = group.KeywordsText,
                        FolderPath = group.FolderPath,
                    };
                    map[key] = slice;
                }

                slice.Items.Add(item);
            }

            return map.Values.ToList();
        }

        public static async Task RunGroupsAsync(
            IngestBenchHost host,
            IReadOnlyList<ItemGroup> groups,
            IngestOptions options,
            CancellationToken token,
            IngestBenchImportVerify? verify = null)
        {
            int total = Math.Max(1, groups.Sum(g => g.Items.Count(i => i.IsSelected)));
            int done = 0;
            var stamp = new long[1];
            var clock = Stopwatch.StartNew();
            if (verify is null)
            {
                Publish(host, 0, total, clock, "Importing", stamp, force: true);
            }

            foreach (ItemGroup group in groups)
            {
                foreach (ItemGroup slice in SplitBySource(group))
                {
                    IFileProvider provider = Create(SourceKey(slice.Items[0]));
                    var engine = new IngestEngine(provider, NullLogger<IngestEngine>.Instance);
                    engine.ItemProcessed += info =>
                    {
                        if (info.IsStarted)
                        {
                            return;
                        }

                        if (verify != null)
                        {
                            verify.NoteCopy(info);
                            return;
                        }

                        if (!info.Success)
                        {
                            IngestBenchQueue.NoteFailed(host, new[] { info.SourcePath });
                        }

                        int n = Interlocked.Increment(ref done);
                        Publish(host, n, total, clock, "Importing", stamp, force: n >= total);
                    };
                    try
                    {
                        await engine.IngestGroupAsync(
                            slice,
                            host.Model.DestinationRoot ?? string.Empty,
                            host.Model.NamingTemplate,
                            token,
                            options,
                            deleteAfterImport: !options.IsDryRun && host.Model.DeleteAfterImport);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        IngestBenchQueue.NoteFailedGroup(host, slice);
                    }
                }
            }
        }

        internal static void Publish(
            IngestBenchHost host,
            int done,
            int total,
            Stopwatch clock,
            string phase,
            long[] stamp,
            bool force)
        {
            long now = Environment.TickCount64;
            if (!force && now - stamp[0] < 120)
            {
                return;
            }

            stamp[0] = now;
            int snap = done;
            TimeSpan elapsed = clock.Elapsed;
            host.Post(() => IngestBenchActivity.Report(
                host.Model,
                IngestBenchImportProgress.Percent(snap, total),
                IngestBenchImportProgress.Line(phase, snap, total, elapsed)));
        }
    }
}
