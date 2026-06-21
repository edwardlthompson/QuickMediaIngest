using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest.Localization;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        [RelayCommand]
        private void ToggleShootStackExpand(ItemGroup? group)
        {
            if (group == null || !GroupRawAndRenderedPairs)
            {
                return;
            }

            group.ExpandStackedPairsInShoot = !group.ExpandStackedPairsInShoot;
            ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
            ApplyFiltersToCurrentGroups();
        }

        [RelayCommand]
        private void ToggleGroupExpanded(ItemGroup? group)
        {
            if (group == null)
            {
                return;
            }

            group.IsExpanded = !group.IsExpanded;
            _shootGroupExpandedMemory[BuildShootExpansionKey(group)] = group.IsExpanded;
        }

        [RelayCommand]
        private void ExpandAllGroups()
        {
            foreach (var group in Groups)
            {
                group.IsExpanded = true;
            }
        }

        [RelayCommand]
        private void CollapseAllGroups()
        {
            foreach (var group in Groups)
            {
                group.IsExpanded = false;
            }
        }

        private async Task LoadThumbnailsAsync(List<ItemGroup> groups, object source, string sourceLabel, CancellationToken cancellationToken = default)
        {
            if (source is UnifiedSourceItem)
            {
                await LoadUnifiedThumbnailsAsync(groups, sourceLabel);
                return;
            }

            if (source is FtpSourceItem ftp)
            {
                await LoadFtpThumbnailsAsync(groups, ftp, sourceLabel, preferBackgroundBatch: false, cancellationToken);
                return;
            }

            await Task.Run(() =>
            {
                var allItems = ThumbnailPreviewOrdering.OrderItemsForLocalPreviews(groups);
                int total = allItems.Count;

                if (total == 0)
                {
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalFilesToScan = total;
                    ScanProgressPercent = 0;
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingPreviewsProgress", 0, total);
                });

                int current = 0;
                int workers = GetThumbnailWorkerCount();
                Parallel.ForEach(allItems, new ParallelOptions { MaxDegreeOfParallelism = workers }, item =>
                {
                    string itemKey = BuildItemKey(item);
                    if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                    {
                        int cCached = Interlocked.Increment(ref current);
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.Thumbnail = cachedThumb;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            ScannedFiles = cCached;
                            ScanProgressPercent = total > 0 ? (cCached * 100) / total : 0;
                            ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingPreviewsProgress", cCached, total);
                        });
                        return;
                    }

                    object? thumb = null;
                    try
                    {
                        thumb = _thumbnailService.GetThumbnail(item.SourcePath, BuildThumbnailHints());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Thumbnail generation failed for local item {SourcePath}.", item.SourcePath);
                    }
                    int c = Interlocked.Increment(ref current);

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
                        ScannedFiles = c;
                        ScanProgressPercent = total > 0 ? (c * 100) / total : 0;
                        ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingPreviewsProgress", c, total);
                    });
                });

                Application.Current.Dispatcher.Invoke(RefreshPreviewHealthSummary);
            });

            StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_LoadedPreviewsAuto", sourceLabel);
            FtpThumbnailPhaseDetail = string.Empty;
        }

        private async Task LoadFtpThumbnailsAsync(List<ItemGroup> groups, FtpSourceItem ftp, string sourceLabel, bool preferBackgroundBatch = true, CancellationToken cancellationToken = default)
        {
            var allItems = OrderItemsForViewportPriority(groups);

            if (allItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_NoFtpImages", sourceLabel);
                return;
            }

            int total = allItems.Count;
            int initialCount = preferBackgroundBatch && LimitFtpThumbnailLoad ? Math.Min(FtpInitialThumbnailCount, total) : total;

            if (initialCount <= 0)
            {
                initialCount = total;
            }

            var initialItems = allItems.Take(initialCount).ToList();
            var remainingItems = allItems.Skip(initialCount).ToList();

            int loadedInitial = await LoadFtpThumbnailBatchAsync(initialItems, ftp, total, 0, true, cancellationToken);

            if (remainingItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_FtpPreviewsLoaded", sourceLabel, loadedInitial, total);
                return;
            }

            StatusMessage = AppLocalizer.Format("Vm_Status_FtpPreviewsPartialBackground", initialCount, total);

            _ = Task.Run(async () =>
            {
                try
                {
                    int loadedRemaining = await LoadFtpThumbnailBatchAsync(remainingItems, ftp, total, initialCount, false, cancellationToken);
                    int loadedTotal = loadedInitial + loadedRemaining;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Status_FtpBackgroundPreviewComplete", loadedTotal, total);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FTP background thumbnail batch failed for {SourceLabel}.", sourceLabel);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Status_PreviewBuildFailed", ex.Message);
                    });
                }
            }, cancellationToken);
        }
    }
}
