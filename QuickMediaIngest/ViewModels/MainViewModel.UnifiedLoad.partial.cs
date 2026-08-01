#nullable enable
using System;
using System.Collections.Concurrent;
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

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly object _unifiedLoadGate = new();
        private Task? _unifiedLoadTask;
        private bool _unifiedForceRefreshQueued;

        private async Task LoadUnifiedSourceItemsAsync(bool forceRefresh = false)
        {
            Task run;
            lock (_unifiedLoadGate)
            {
                if (_unifiedLoadTask != null)
                {
                    if (forceRefresh)
                    {
                        _unifiedForceRefreshQueued = true;
                    }

                    run = _unifiedLoadTask;
                }
                else
                {
                    run = RunUnifiedLoadLoopAsync(forceRefresh);
                    _unifiedLoadTask = run;
                }
            }

            await run.ConfigureAwait(false);
        }

        private async Task RunUnifiedLoadLoopAsync(bool forceRefresh)
        {
            try
            {
                await LoadUnifiedSourceItemsCoreAsync(forceRefresh).ConfigureAwait(false);
                while (true)
                {
                    bool again;
                    lock (_unifiedLoadGate)
                    {
                        again = _unifiedForceRefreshQueued;
                        _unifiedForceRefreshQueued = false;
                    }

                    if (!again)
                    {
                        break;
                    }

                    await LoadUnifiedSourceItemsCoreAsync(forceRefresh: true).ConfigureAwait(false);
                }
            }
            finally
            {
                lock (_unifiedLoadGate)
                {
                    _unifiedLoadTask = null;
                }
            }
        }

        private async Task LoadUnifiedSourceItemsCoreAsync(bool forceRefresh)
        {
            var userExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var concreteSources = Sources
                .Where(s => s is string || s is FtpSourceItem)
                .ToList();

            if (concreteSources.Count == 0)
            {
                _logger.LogInformation("Unified load skipped: no drive or FTP sources in the sidebar.");
                _currentSourceItems = new List<ImportItem>();
                StatusMessage = AppLocalizer.Get("Vm_Status_NoSourcesForUnified");
                return;
            }

            _logger.LogInformation(
                "Unified load starting: {SourceCount} sources: {SourceSummary}.",
                concreteSources.Count,
                string.Join(", ", concreteSources.Select(s => s.ToString() ?? "")));

            foreach (var ftp in concreteSources.OfType<FtpSourceItem>())
            {
                EnsureFtpSourceCredentials(ftp);
            }

            _ftpThumbnailCts?.Cancel();
            _ftpThumbnailCts?.Dispose();
            _ftpThumbnailCts = new CancellationTokenSource();
            CancellationToken thumbToken = _ftpThumbnailCts.Token;

            var itemsBySource = new ConcurrentDictionary<string, List<ImportItem>>(StringComparer.OrdinalIgnoreCase);
            var ftpPending = concreteSources.OfType<FtpSourceItem>().Any();
            int localPainted = 0;

            try
            {
                HasUnifiedFtpListingFailures = false;
                RefreshUxEmptyStateHints();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
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
                });

                var mergeProgress = new Progress<(int Completed, int Total)>(tuple =>
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        ScannedFolders = Math.Max(ScannedFolders, tuple.Completed);
                        ScanProgressPercent = tuple.Total > 0 ? (tuple.Completed * 100) / tuple.Total : 0;
                        ScanProgressMessage = AppLocalizer.Format("Vm_Scan_MergedSourcesProgress", tuple.Completed, tuple.Total);
                    });
                });

                var sourceCompleted = new Progress<UnifiedScanSourceCompleted>(update =>
                {
                    itemsBySource[update.SourceKey] = update.Items.ToList();
                    if (!update.IsFtp)
                    {
                        _ = ApplyUnifiedPartialLocalAsync(
                            itemsBySource,
                            userExcludedFolders,
                            ftpPending,
                            thumbToken,
                            () => Interlocked.Exchange(ref localPainted, 1) == 0);
                    }
                    else if (!string.IsNullOrWhiteSpace(update.FailureNote))
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = AppLocalizer.Format("Vm_Status_UnifiedFtpUnavailable", update.FailureNote!);
                        });
                    }
                    else
                    {
                        Application.Current?.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = AppLocalizer.Get("Vm_Status_UnifiedFtpStillLoading");
                        });
                    }
                });

                UnifiedScanMergeResult merge = await _unifiedConcreteSourceScanService
                    .MergeAllAsync(
                        concreteSources,
                        forceRefresh,
                        ScanIncludeSubfolders,
                        _sourceItemsCache,
                        mergeProgress,
                        CancellationToken.None,
                        PreferAdbTransferWhenAvailable,
                        sourceCompleted)
                    .ConfigureAwait(false);

                ftpPending = false;
                HasUnifiedFtpListingFailures = merge.FtpListingFailures.Count > 0;
                RefreshUxEmptyStateHints();

                List<ImportItem> unifiedItems = merge.UnifiedItems;
                ApplySkippedFolderFilters(unifiedItems, userExcludedFolders);
                _currentSourceItems = unifiedItems;

                List<ItemGroup> groupsForThumbnails = await Application.Current.Dispatcher
                    .InvokeAsync(() =>
                    {
                        ScannedFiles = unifiedItems.Count;
                        TotalFilesToScan = unifiedItems.Count;
                        RebuildGroupsFromCurrentItems();
                        ShowScanProgressDialog = false;
                        if (merge.FtpListingFailures.Count > 0)
                        {
                            StatusMessage = AppLocalizer.Format(
                                "Vm_Status_UnifiedFtpUnavailable",
                                string.Join("; ", merge.FtpListingFailures.Take(2)));
                        }

                        return Groups.ToList();
                    })
                    .Task
                    .ConfigureAwait(false);

                if (groupsForThumbnails.Count > 0)
                {
                    // Unified thumbs reuse _thumbnailByItemKey — local CR2s already painted are skipped.
                    await LoadThumbnailsAsync(groupsForThumbnails, _unifiedSource, "Unified", thumbToken)
                        .ConfigureAwait(false);
                }
                else if (Volatile.Read(ref localPainted) == 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Get("Vm_Status_UnifiedNoMedia");
                    });
                }

                if (merge.FtpListingFailures.Count > 0 || userExcludedFolders.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        MaybeShowSkippedFoldersScanReport("Unified", merge.FtpListingFailures, userExcludedFolders));
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

        private async Task ApplyUnifiedPartialLocalAsync(
            ConcurrentDictionary<string, List<ImportItem>> itemsBySource,
            HashSet<string> userExcludedFolders,
            bool ftpStillPending,
            CancellationToken thumbToken,
            Func<bool> tryClaimFirstPaint)
        {
            List<ImportItem> partial = itemsBySource.Values.SelectMany(v => v).ToList();
            ApplySkippedFolderFilters(partial, userExcludedFolders);

            List<ItemGroup> groups = await Application.Current.Dispatcher
                .InvokeAsync(() =>
                {
                    _currentSourceItems = partial;
                    RebuildGroupsFromCurrentItems();
                    ShowScanProgressDialog = false;
                    ScannedFiles = partial.Count;
                    TotalFilesToScan = partial.Count;
                    StatusMessage = ftpStillPending
                        ? AppLocalizer.Get("Vm_Status_UnifiedFtpStillLoading")
                        : AppLocalizer.Format("Vm_Status_ScanComplete_LoadedPreviewsAuto", "Unified");
                    return Groups.ToList();
                })
                .Task
                .ConfigureAwait(false);

            if (tryClaimFirstPaint() && groups.Count > 0)
            {
                await LoadThumbnailsAsync(groups, _unifiedSource, "Unified", thumbToken).ConfigureAwait(false);
            }
        }
    }
}
