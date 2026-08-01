#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Localization;
using QuickMediaIngest.Thumbnails;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private async Task LoadUnifiedThumbnailsAsync(
            List<ItemGroup> groups,
            string sourceLabel,
            CancellationToken cancellationToken = default)
        {
            var allItems = OrderItemsForViewportPriority(groups);
            if (allItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_NoUnifiedImages", sourceLabel);
                return;
            }

            int libraryTotal = allItems.Count;
            int processedAtomic = 0;

            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            foreach (FtpSourceItem ftp in ftpSourcesByKey.Values)
            {
                EnsureFtpSourceCredentials(ftp);
            }

            var needLocal = new List<ImportItem>();
            var needFtp = new List<ImportItem>();

            foreach (ImportItem item in allItems)
            {
                string itemKey = BuildItemKey(item);
                if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        item.Thumbnail = cachedThumb;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                    });
                    Interlocked.Increment(ref processedAtomic);
                }
                else if (item.IsFtpSource)
                {
                    needFtp.Add(item);
                }
                else
                {
                    needLocal.Add(item);
                }
            }

            int ftpInitialCount = needFtp.Count;
            List<ImportItem> ftpRemaining = [];
            if (LimitFtpThumbnailLoad && needFtp.Count > FtpInitialThumbnailCount)
            {
                ftpInitialCount = FtpInitialThumbnailCount;
                ftpRemaining = needFtp.Skip(ftpInitialCount).ToList();
                needFtp = needFtp.Take(ftpInitialCount).ToList();
            }

            int progressTotal = needLocal.Count + needFtp.Count + processedAtomic;
            await Application.Current.Dispatcher
                .InvokeAsync(() =>
                {
                    ScannedFiles = processedAtomic;
                    TotalFilesToScan = Math.Max(progressTotal, libraryTotal);
                    ScanProgressPercent = progressTotal > 0 ? (processedAtomic * 100) / progressTotal : 0;
                    ScanProgressMessage = AppLocalizer.Format(
                        "Vm_Scan_LoadingUnifiedPreviewsProgress",
                        processedAtomic,
                        progressTotal);
                })
                .Task
                .ConfigureAwait(false);

            void BumpProgress()
            {
                int c = Interlocked.Increment(ref processedAtomic);
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ScannedFiles = Math.Max(ScannedFiles, c);
                    int shown = ScannedFiles;
                    int total = Math.Max(progressTotal, libraryTotal);
                    TotalFilesToScan = total;
                    ScanProgressPercent = total > 0 ? (shown * 100) / total : 0;
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingUnifiedPreviewsProgress", shown, total);
                });
            }

            try
            {
                await LoadUnifiedPreviewPassAsync(
                        needLocal,
                        needFtp,
                        ftpSourcesByKey,
                        BumpProgress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Unified preview load canceled for {SourceLabel}.", sourceLabel);
                StatusMessage = AppLocalizer.Get("Vm_Status_PreviewLoadCanceled");
                return;
            }

            Application.Current.Dispatcher.Invoke(RefreshPreviewHealthSummary);

            if (ftpRemaining.Count == 0)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_UnifiedPreviews", sourceLabel);
                return;
            }

            StatusMessage = AppLocalizer.Format(
                "Vm_Status_FtpPreviewsPartialBackground",
                ftpInitialCount,
                libraryTotal);

            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadUnifiedFtpPreviewGroupsAsync(
                            ftpRemaining,
                            ftpSourcesByKey,
                            () => { },
                            cancellationToken)
                        .ConfigureAwait(false);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        RefreshPreviewHealthSummary();
                        StatusMessage = AppLocalizer.Format(
                            "Vm_Status_FtpBackgroundPreviewComplete",
                            ftpInitialCount + ftpRemaining.Count,
                            libraryTotal);
                    });
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unified background thumbnail batch failed for {SourceLabel}.", sourceLabel);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Status_PreviewBuildFailed", ex.Message);
                    });
                }
            }, cancellationToken);
        }

        private async Task LoadUnifiedPreviewPassAsync(
            List<ImportItem> needLocal,
            List<ImportItem> needFtp,
            Dictionary<string, FtpSourceItem> ftpSourcesByKey,
            Action bumpProgress,
            CancellationToken cancellationToken)
        {
            string? localSamplePath = needLocal.Count > 0 ? needLocal[0].SourcePath : null;
            int localWorkers = GetThumbnailWorkerCount(localSamplePath);

            Task localTask = Task.Run(() =>
            {
                Parallel.ForEach(
                    needLocal,
                    new ParallelOptions { MaxDegreeOfParallelism = localWorkers, CancellationToken = cancellationToken },
                    item =>
                    {
                        string itemKey = BuildItemKey(item);
                        object? thumb = null;
                        try
                        {
                            thumb = WpfThumbnailBridge.ToBitmapSource(
                                _thumbnailService.GetThumbnail(item.SourcePath, BuildThumbnailHints()));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Unified thumbnail failed for local {Path}.", item.SourcePath);
                        }

                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (thumb != null)
                            {
                                item.Thumbnail = thumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                _thumbnailByItemKey[itemKey] = thumb;
                            }
                            else
                            {
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                            }
                        });
                        bumpProgress();
                    });
            }, cancellationToken);

            Task ftpTask = LoadUnifiedFtpPreviewGroupsAsync(needFtp, ftpSourcesByKey, bumpProgress, cancellationToken);
            await Task.WhenAll(localTask, ftpTask).ConfigureAwait(false);
        }

        private async Task LoadUnifiedFtpPreviewGroupsAsync(
            List<ImportItem> needFtp,
            Dictionary<string, FtpSourceItem> ftpSourcesByKey,
            Action bumpProgress,
            CancellationToken cancellationToken)
        {
            foreach (var group in needFtp.GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ftpSourcesByKey.TryGetValue(group.Key, out FtpSourceItem? ftp))
                {
                    foreach (ImportItem item in group)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                        bumpProgress();
                    }

                    continue;
                }

                var groupItems = group.ToList();
                var itemByKey = groupItems.ToDictionary(BuildItemKey, i => i, StringComparer.OrdinalIgnoreCase);
                var workItems = groupItems
                    .Where(item => !ShouldSkipFtpThumbnailWorkItem(item, groupItems))
                    .Select(item => new FtpThumbnailWorkItem
                    {
                        ItemKey = BuildItemKey(item),
                        RemotePath = item.SourcePath,
                        FileName = item.FileName,
                        FileSize = item.FileSize
                    })
                    .ToList();

                FtpThumbnailBatchResult batch = await _ftpThumbnailService.LoadBatchAsync(
                    ToFtpEndpoint(ftp),
                    workItems,
                    BuildThumbnailHints(),
                    BuildFtpThumbnailLoadOptions(),
                    _ =>
                    {
                        bumpProgress();
                        return Task.CompletedTask;
                    },
                    async result =>
                    {
                        await ApplyFtpThumbnailResultAsync(result, itemByKey);
                    },
                    cancellationToken);

                await ApplyFtpThumbnailBatchResultsAsync(batch.Items, itemByKey);
                await ApplyRenderedSiblingThumbnailsAsync(groupItems);
            }
        }
    }
}
