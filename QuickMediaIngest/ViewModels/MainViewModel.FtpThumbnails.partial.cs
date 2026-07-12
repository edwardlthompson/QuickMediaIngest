using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Media;
using System.Net;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Localization;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Data;
using QuickMediaIngest;
using QuickMediaIngest.Thumbnails;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private async Task<int> LoadFtpThumbnailBatchAsync(
            List<ImportItem> items,
            FtpSourceItem ftp,
            int totalItemCount,
            int startIndex,
            bool updateScanProgressMessage,
            CancellationToken cancellationToken = default)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            EnsureFtpSourceCredentials(ftp);
            var endpoint = ToFtpEndpoint(ftp);
            var itemByKey = items.ToDictionary(BuildItemKey, i => i, StringComparer.OrdinalIgnoreCase);

            int loadedFromCache = 0;
            var workItems = new List<FtpThumbnailWorkItem>();

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string itemKey = BuildItemKey(item);

                if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb))
                {
                    if (cachedThumb is System.Windows.Media.Imaging.BitmapSource cachedBitmap
                        && cachedBitmap.PixelWidth >= ThumbnailPreviewValidator.MinPixelEdge
                        && cachedBitmap.PixelHeight >= ThumbnailPreviewValidator.MinPixelEdge)
                    {
                        loadedFromCache++;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.Thumbnail = cachedBitmap;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                        }, System.Windows.Threading.DispatcherPriority.Background, cancellationToken);
                        continue;
                    }

                    _thumbnailByItemKey.TryRemove(itemKey, out _);
                }

                if (ShouldSkipFtpThumbnailWorkItem(item, items))
                {
                    continue;
                }

                var diskThumb = FtpThumbnailCache.TryLoad(ftp.Host, ftp.Port, item.SourcePath, item.FileSize);
                if (diskThumb != null)
                {
                    var bitmap = WpfThumbnailBridge.ToBitmapSource(diskThumb);
                    if (bitmap != null)
                    {
                        loadedFromCache++;
                        _thumbnailByItemKey[itemKey] = bitmap;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.Thumbnail = bitmap;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                        }, System.Windows.Threading.DispatcherPriority.Background, cancellationToken);
                        continue;
                    }
                }

                workItems.Add(new FtpThumbnailWorkItem
                {
                    ItemKey = itemKey,
                    RemotePath = item.SourcePath,
                    FileName = item.FileName,
                    FileSize = item.FileSize
                });
            }

            int loadedCount = loadedFromCache;
            int skippedCount = 0;

            if (workItems.Count > 0)
            {
                FtpThumbnailBatchResult batch = await _ftpThumbnailService.LoadBatchAsync(
                    endpoint,
                    workItems,
                    BuildThumbnailHints(),
                    BuildFtpThumbnailLoadOptions(),
                    async progress =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ScannedFiles = startIndex + progress.Processed;
                            TotalFilesToScan = totalItemCount;
                            ScanProgressPercent = totalItemCount > 0 ? ((startIndex + progress.Processed) * 100) / totalItemCount : 0;
                            CurrentScanFolder = progress.CurrentRemotePath ?? string.Empty;
                            CurrentScanFolderProcessedFiles = progress.Processed;
                            CurrentScanFolderTotalFiles = workItems.Count;
                            if (updateScanProgressMessage)
                            {
                                ScanProgressMessage = AppLocalizer.Format(
                                    "Vm_Scan_LoadingFtpPreviewsProgress",
                                    Math.Min(totalItemCount, startIndex + progress.Processed),
                                    totalItemCount);
                            }
                        }, System.Windows.Threading.DispatcherPriority.Background);
                    },
                    async result =>
                    {
                        await ApplyFtpThumbnailResultAsync(result, itemByKey);
                        await Application.Current.Dispatcher.InvokeAsync(RefreshPreviewHealthSummary);
                    },
                    cancellationToken);

                loadedCount += batch.LoadedCount;
                skippedCount = batch.SkippedCount;
            }

            await ApplyRenderedSiblingThumbnailsAsync(items);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (skippedCount > 0)
                {
                    ScanProgressMessage = AppLocalizer.Format(
                        "Vm_Scan_LoadingFtpPreviewsWithSkips",
                        Math.Min(totalItemCount, startIndex + items.Count),
                        totalItemCount,
                        skippedCount);
                }

                FtpThumbnailPhaseDetail = $"FTP previews: loaded {loadedCount}/{items.Count} in batch · skipped {skippedCount}";
                RefreshPreviewHealthSummary();
            }, System.Windows.Threading.DispatcherPriority.Background);

            return loadedCount;
        }

        [RelayCommand]
        private async Task RefreshUnified()
        {
            await LoadUnifiedSourceItemsAsync(forceRefresh: true);
        }

        private async Task LoadUnifiedSourceItemsAsync(bool forceRefresh = false)
        {
            var userExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var concreteSources = Sources
                .Where(s => s is string || s is FtpSourceItem)
                .ToList();

            if (concreteSources.Count == 0)
            {
                _logger.LogInformation("Unified load skipped: no drive or FTP sources in the sidebar. Add sources or enable fixed drives in drive selection.");
                _currentSourceItems = new List<ImportItem>();
                StatusMessage = AppLocalizer.Get("Vm_Status_NoSourcesForUnified");
                return;
            }

            _logger.LogInformation(
                "Unified load starting: {SourceCount} sources: {SourceSummary}. Sidebar uses removable drives by default; enable fixed drives in drive selection to merge them here.",
                concreteSources.Count,
                string.Join(", ", concreteSources.Select(s => s.ToString() ?? "")));

            foreach (var ftp in concreteSources.OfType<FtpSourceItem>())
            {
                EnsureFtpSourceCredentials(ftp);
            }

            try
            {
                HasUnifiedFtpListingFailures = false;
                RefreshUxEmptyStateHints();

                ShowScanProgressDialog = true;
                ScanDialogTitle = forceRefresh
                    ? AppLocalizer.Get("Vm_Scan_UnifiedRefreshing")
                    : AppLocalizer.Get("Vm_Scan_UnifiedLoading");
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = concreteSources.Count;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = AppLocalizer.Get("Vm_Scan_MergingSources");
                CurrentScanFolder = "/";
                CurrentScanFolderProcessedFiles = 0;
                CurrentScanFolderTotalFiles = 0;

                var mergeProgress = new Progress<(int Completed, int Total)>(tuple =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ScannedFolders = Math.Max(ScannedFolders, tuple.Completed);
                        ScanProgressPercent = tuple.Total > 0 ? (tuple.Completed * 100) / tuple.Total : 0;
                        ScanProgressMessage = AppLocalizer.Format("Vm_Scan_MergedSourcesProgress", tuple.Completed, tuple.Total);
                    });
                });

                UnifiedScanMergeResult merge = await _unifiedConcreteSourceScanService
                    .MergeAllAsync(concreteSources, forceRefresh, ScanIncludeSubfolders, _sourceItemsCache, mergeProgress, CancellationToken.None)
                    .ConfigureAwait(false);

                HasUnifiedFtpListingFailures = merge.FtpListingFailures.Count > 0;
                RefreshUxEmptyStateHints();

                HashSet<string> ftpListingFailures = merge.FtpListingFailures;
                List<ImportItem> unifiedItems = merge.UnifiedItems;

                ApplySkippedFolderFilters(unifiedItems, userExcludedFolders);

                _currentSourceItems = unifiedItems;

                List<ItemGroup> groupsForThumbnails = await Application.Current.Dispatcher
                    .InvokeAsync(() =>
                    {
                        ScannedFiles = unifiedItems.Count;
                        TotalFilesToScan = unifiedItems.Count;
                        CurrentScanFolderProcessedFiles = unifiedItems.Count;
                        CurrentScanFolderTotalFiles = unifiedItems.Count;
                        RebuildGroupsFromCurrentItems();
                        ShowScanProgressDialog = false;
                        return Groups.ToList();
                    })
                    .Task
                    .ConfigureAwait(false);

                if (groupsForThumbnails.Count > 0)
                {
                    await LoadThumbnailsAsync(groupsForThumbnails, _unifiedSource, "Unified");
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Get("Vm_Status_UnifiedNoMedia");
                    });
                }

                if (ftpListingFailures.Count > 0 || userExcludedFolders.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() => MaybeShowSkippedFoldersScanReport("Unified", ftpListingFailures, userExcludedFolders));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unified source load failed.");
                StatusMessage = AppLocalizer.Format("Vm_Status_UnifiedLoadError", ex.Message);
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }
    }
}
