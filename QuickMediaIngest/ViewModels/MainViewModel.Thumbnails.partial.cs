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


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        private void ImportItem_SelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportItem.IsSelected))
            {
                RefreshImportReadinessSummary();
            }
        }

        private void DetachImportItemSelectionHandlers()
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.PropertyChanged -= ImportItem_SelectionChanged;
                }
            }
        }

        private void AttachImportItemSelectionHandlers(ItemGroup group)
        {
            foreach (var item in group.Items)
            {
                item.PropertyChanged -= ImportItem_SelectionChanged;
                item.PropertyChanged += ImportItem_SelectionChanged;
            }
        }

        private string BuildImportConfirmationMessage(List<ItemGroup> selectedGroups, int totalFiles)
        {
            long bytes = selectedGroups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => Math.Max(0, i.FileSize));
            string mb = (bytes / (1024d * 1024d)).ToString("0.00", CultureInfo.CurrentCulture);

            var sb = new StringBuilder();
            sb.AppendLine(AppLocalizer.Format("Vm_ConfirmImport_Line1", totalFiles, mb));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_Destination"));
            sb.AppendLine(DestinationRoot);
            sb.AppendLine();
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DupPolicy")).Append(' ').AppendLine(DuplicatePolicy);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_Verify")).Append(' ').AppendLine(VerificationMode);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DeleteAfter")).Append(' ')
                .AppendLine(DeleteAfterImport ? AppLocalizer.Get("Vm_Yes") : AppLocalizer.Get("Vm_No"));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_KeywordsHeader"));
            if (!EmbedKeywordsOnImport)
            {
                sb.AppendLine(AppLocalizer.Get("Vm_Readiness_KwOff"));
            }
            else
            {
                bool any = false;
                foreach (ItemGroup g in selectedGroups.OrderBy(x => x.Title))
                {
                    List<string> list = KeywordInputParser.Parse(g.KeywordsText);
                    if (list.Count == 0)
                    {
                        continue;
                    }

                    any = true;
                    sb.AppendLine(AppLocalizer.Format("Vm_Confirm_ShootKeywords", g.Title, string.Join(", ", list)));
                }

                if (!any)
                {
                    sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_NoKeywords"));
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void RepopulateLanguageOptions()
        {
            UiLanguageOptions.Clear();
            UiLanguageOptions.Add(new LanguageOption("", AppLocalizer.Get("Lang_UseSystem")));
            UiLanguageOptions.Add(new LanguageOption("en", AppLocalizer.Get("Lang_English")));
            UiLanguageOptions.Add(new LanguageOption("fr", AppLocalizer.Get("Lang_French")));
            UiLanguageOptions.Add(new LanguageOption("es", AppLocalizer.Get("Lang_Spanish")));
            InitializeIntervalOptions();
            InitializeSidebarSections();
            ApplyLocalizedShellStrings();
        }

        [RelayCommand]
        private async Task RetryFailedPreviewLoadsAsync()
        {
            if (Groups.Count == 0 || SelectedSource == null)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NothingToRetry");
                return;
            }

            var failedItems = Groups.SelectMany(g => g.Items).Where(i => i.ThumbnailPreviewStatus == ThumbnailPreviewStatus.Failed).ToList();
            if (failedItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NoFailedPreviews");
                return;
            }

            StatusMessage = $"Retrying {failedItems.Count} preview(s)...";

            var failedLocal = failedItems.Where(i => !i.IsFtpSource).ToList();
            var failedFtp = failedItems.Where(i => i.IsFtpSource).ToList();

            if (failedLocal.Count > 0)
            {
                await Task.Run(() =>
                {
                    Parallel.ForEach(failedLocal, new ParallelOptions { MaxDegreeOfParallelism = GetThumbnailWorkerCount() }, item =>
                    {
                        string key = BuildItemKey(item);
                        object? thumb = null;
                        try
                        {
                            thumb = _thumbnailService.GetThumbnail(item.SourcePath, BuildThumbnailHints());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Retry thumbnail failed for {Path}.", item.SourcePath);
                        }

                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (thumb != null)
                            {
                                item.Thumbnail = thumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                _thumbnailByItemKey[key] = thumb;
                            }
                            else
                            {
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                            }
                        });
                    });
                });
            }

            if (failedFtp.Count > 0 && SelectedSource is FtpSourceItem ftpSource)
            {
                await LoadFtpThumbnailBatchAsync(failedFtp, ftpSource, failedFtp.Count, 0, false);
            }
            else if (failedFtp.Count > 0 && SelectedSource is UnifiedSourceItem)
            {
                var ftpSourcesByKey = Sources
                    .OfType<FtpSourceItem>()
                    .ToDictionary(BuildSourceKey, f => f, StringComparer.OrdinalIgnoreCase);

                foreach (var group in failedFtp.GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase))
                {
                    if (!ftpSourcesByKey.TryGetValue(group.Key, out var ftp))
                    {
                        continue;
                    }

                    await LoadFtpThumbnailBatchAsync(group.ToList(), ftp, group.Count(), 0, false);
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(RefreshPreviewHealthSummary);
            StatusMessage = AppLocalizer.Get("Vm_Status_PreviewRetryFinished");
        }

        [RelayCommand]
        private async Task ClearThumbnailCacheAndReloadPreviewsAsync()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_LoadSourceBeforeClearPreviewCache");
                return;
            }

            try
            {
                _thumbnailByItemKey.Clear();
                ClearThumbnailDiskCache();
                foreach (var group in Groups)
                {
                    foreach (var item in group.Items)
                    {
                        item.Thumbnail = null;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Unknown;
                    }
                }

                string label = GetThumbnailSourceLabel();
                StatusMessage = AppLocalizer.Get("Vm_Status_ThumbnailCacheClearedReloading");
                await LoadThumbnailsAsync(Groups.ToList(), SelectedSource, label);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to reload previews: {ex.Message}";
            }
        }

        private string GetThumbnailSourceLabel()
        {
            return SelectedSource switch
            {
                FtpSourceItem ftp => $"{ftp.Host}{NormalizeFtpPath(ftp.RemoteFolder)}",
                UnifiedSourceItem => "Unified",
                _ => SelectedSource?.ToString() ?? "source"
            };
        }

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
                // Use CPU-aware parallelism for local thumbnail decode.
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
