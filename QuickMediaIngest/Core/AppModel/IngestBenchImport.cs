#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Local Import / Dry run for the ingest-bench (Avalonia + tests).</summary>
    public static class IngestBenchImport
    {
        public static List<ImportItem> CollectItems(IEnumerable<string> roots, IEnumerable<string>? excludeFolders = null)
        {
            var items = new List<ImportItem>();
            foreach (string path in VolumeScan.ListMedia(roots, excludeFolders: excludeFolders))
            {
                var info = new FileInfo(path);
                if (!info.Exists)
                {
                    continue;
                }

                string ext = info.Extension;
                items.Add(new ImportItem
                {
                    SourcePath = info.FullName,
                    FileName = info.Name,
                    FileSize = info.Length,
                    DateTaken = info.LastWriteTime,
                    IsSelected = true,
                    FileType = ext.TrimStart('.'),
                    IsVideo = MediaExtensions.IsVideoExtension(ext),
                });
            }

            return items;
        }

        public static async Task<bool> RunAsync(
            IngestBenchHost host,
            bool dryRun,
            IngestBenchCopy copy,
            CancellationToken cancellationToken = default,
            bool resetFailures = true)
        {
            IngestBenchAppModel model = host.Model;
            if (string.IsNullOrWhiteSpace(model.DestinationRoot))
            {
                await host.Prompt.NotifyAsync(copy.Get("Dlg_SelectDestinationFolder"), copy.Get("Empty_NoFilesTitle"));
                return false;
            }

            List<ItemGroup> groups = host.Groups.Count > 0
                ? host.Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList()
                : new GroupBuilder().BuildGroups(CollectItems(host.ScanRoots, host.Model.ExcludedFolders), IngestBenchFilters.Gap(model));
            if (groups.Count == 0 || groups.All(g => g.Items.Count == 0))
            {
                await host.Prompt.NotifyAsync(copy.Get("Empty_NoFilesTitle"), copy.Get("Empty_NoSourcesBody"));
                return false;
            }
            var forecast = ImportDestinationEstimator.ForecastSpace(groups, model.DestinationRoot);
            if (!forecast.isSufficientSpace)
            {
                long needMb = Math.Max(1, forecast.selectedBytes / (1024 * 1024));
                long haveMb = Math.Max(0, (forecast.availableFreeBytes ?? 0) / (1024 * 1024));
                await host.Prompt.NotifyAsync(
                    copy.Get("Msg_ImportFreeSpace_AbortTitle"),
                    copy.Format("Msg_ImportFreeSpace_AbortBody", new object[] { needMb, haveMb }));
                return false;
            }

            model.CollisionCount = IngestCollisionAnalyzer.Analyze(groups, model.DestinationRoot, model.NamingTemplate, IngestBenchPost.Duplicates(model.Prefs.DuplicatePolicy)).CollisionsCount;
            if (!dryRun && model.Prefs.ConfirmBeforeImport && !await host.Prompt.ConfirmAsync(copy.Get("Settings_ConfirmImport"), copy.Get("Onboarding_Body")))
            {
                return false;
            }

            if (!dryRun && model.DeleteAfterImport)
            {
                bool ok = await host.Prompt.ConfirmAsync(
                    copy.Get("Msg_DeleteAfterImport_ConfirmTitle"),
                    copy.Get("Msg_DeleteAfterImport_ConfirmBody"));
                if (!ok)
                {
                    return false;
                }
            }

            var options = IngestBenchPost.CreateOptions(model, dryRun);
            if (!dryRun)
            {
                await IngestBenchPost.SkipAlreadyImportedAsync(host, groups, cancellationToken);
            }

            Directory.CreateDirectory(model.DestinationRoot);
            int imported = groups.Sum(g => g.Items.Count(i => i.IsSelected));
            int selected = imported;
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            host.ImportCts = linked;
            IngestBenchScene.Begin(model);
            var clock = Stopwatch.StartNew();
            if (resetFailures)
            {
                host.FailedPaths.Clear();
            }

            if (!dryRun)
            {
                IngestBenchQueue.SavePlan(host, IngestBenchQueue.Snapshot(host));
            }

            bool cancelled = false;
            string? postError = null;
            try
            {
                if (dryRun)
                {
                    await IngestBenchImportCopy.RunGroupsAsync(host, groups, options, linked.Token);
                }
                else
                {
                    var manifests = await IngestBenchImportVerify.CopyAndHashAsync(
                        host, groups, options, clock, linked.Token);
                    try
                    {
                        await IngestBenchPost.AfterImportAsync(host, groups, copy, manifests, linked.Token);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        postError = ex.Message;
                    }

                    IngestBenchShoots.DropImported(host, groups, host.FailedPaths);
                }
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }
            finally
            {
                clock.Stop();
                host.ImportCts = null;
                linked.Dispose();
                if (cancelled)
                {
                    IngestBenchScene.End(model, copy.Get("Btn_CancelImport"));
                }
                else if (dryRun)
                {
                    IngestBenchScene.End(model, model.ImportStatus);
                }
                else
                {
                    if (host.FailedPaths.Count == 0)
                    {
                        IngestBenchQueue.ClearPlan(host);
                    }

                    IngestBenchScene.Complete(
                        model, copy, Math.Max(0, selected - host.FailedPaths.Count), host.FailedPaths.Count);
                    if (!string.IsNullOrWhiteSpace(postError))
                    {
                        model.ImportStatus = "Import finished, catalog failed: " + postError;
                    }

                    IngestBenchHistory.Record(host, selected, imported, host.FailedPaths.Count, clock.Elapsed);
                    IngestBenchQueue.Refresh(host);
                }
            }

            if (cancelled)
            {
                return false;
            }

            if (dryRun)
            {
                await host.Prompt.NotifyAsync(
                    copy.Get("Toolbar_Preflight"),
                    copy.Format("Msg_ImportSummary_Body", new object[]
                    {
                        Math.Max(1, forecast.selectedBytes / (1024 * 1024)),
                        Math.Max(0, (forecast.availableFreeBytes ?? 0) / (1024L * 1024L * 1024L)),
                    }));
            }
            else
            {
                await IngestBenchQueue.TryStartNextAsync(host, copy);
            }

            return true;
        }
    }
}
