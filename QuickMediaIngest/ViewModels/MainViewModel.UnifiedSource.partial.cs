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

        private async Task LoadUnifiedThumbnailsAsync(List<ItemGroup> groups, string sourceLabel)
        {
            var allItems = groups
                .SelectMany(g => g.Items)
                .ToList();

            if (allItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_NoUnifiedImages", sourceLabel);
                return;
            }

            int total = allItems.Count;
            int processedAtomic = 0;

            await Application.Current.Dispatcher
                .InvokeAsync(() =>
                {
                    ScannedFiles = 0;
                    TotalFilesToScan = total;
                    ScanProgressPercent = 0;
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingUnifiedPreviewsProgress", 0, total);
                })
                .Task
                .ConfigureAwait(false);

            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            foreach (var ftp in ftpSourcesByKey.Values)
            {
                EnsureFtpSourceCredentials(ftp);
            }

            var needLocal = new List<ImportItem>();
            var needFtp = new List<ImportItem>();

            foreach (var item in allItems)
            {
                string itemKey = BuildItemKey(item);
                if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        item.Thumbnail = cachedThumb;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                    });
                    BumpProgress();
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

            int localWorkers = GetThumbnailWorkerCount();

            void BumpProgress()
            {
                int c = Interlocked.Increment(ref processedAtomic);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ScannedFiles = Math.Max(ScannedFiles, c);
                    int shown = ScannedFiles;
                    TotalFilesToScan = total;
                    ScanProgressPercent = total > 0 ? (shown * 100) / total : 0;
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingUnifiedPreviewsProgress", shown, total);
                });
            }

            Task localTask = Task.Run(() =>
            {
                Parallel.ForEach(
                    needLocal,
                    new ParallelOptions { MaxDegreeOfParallelism = localWorkers },
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

                        Application.Current.Dispatcher.Invoke(() =>
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
                        BumpProgress();
                    });
            });

            Task ftpTask = Task.Run(async () =>
            {
                var ftpGroups = needFtp.GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase);
                foreach (var group in ftpGroups)
                {
                    if (!ftpSourcesByKey.TryGetValue(group.Key, out var ftp))
                    {
                        foreach (var item in group)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                            BumpProgress();
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
                            BumpProgress();
                            return Task.CompletedTask;
                        },
                        async result =>
                        {
                            await ApplyFtpThumbnailResultAsync(result, itemByKey);
                        },
                        CancellationToken.None);

                    await ApplyFtpThumbnailBatchResultsAsync(batch.Items, itemByKey);
                }
            });

            await Task.WhenAll(localTask, ftpTask).ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(RefreshPreviewHealthSummary);
            StatusMessage = AppLocalizer.Format("Vm_Status_ScanComplete_UnifiedPreviews", sourceLabel);
        }

        private static void StampItems(List<ImportItem> items, string sourceId, bool isFtp)
        {
            foreach (var item in items)
            {
                item.SourceId = sourceId;
                item.IsFtpSource = isFtp;
            }
        }

        private static List<ImportItem> CloneItems(List<ImportItem> items)
        {
            return items.Select(i => new ImportItem
            {
                SourcePath = i.SourcePath,
                SourceId = i.SourceId,
                IsFtpSource = i.IsFtpSource,
                FileName = i.FileName,
                FileSize = i.FileSize,
                DateTaken = i.DateTaken,
                IsVideo = i.IsVideo,
                FileType = i.FileType,
                IsSelected = i.IsSelected,
                Thumbnail = i.Thumbnail,
                IsPreviewVisible = i.IsPreviewVisible,
                PreviewLabel = i.PreviewLabel,
                StackKey = i.StackKey,
                IsStackRepresentative = i.IsStackRepresentative,
                ThumbnailPreviewStatus = i.ThumbnailPreviewStatus
            }).ToList();
        }

        private static string BuildItemKey(ImportItem item)
        {
            string sourceId = string.IsNullOrWhiteSpace(item.SourceId) ? "unknown" : item.SourceId;
            return $"{sourceId}|{item.SourcePath}";
        }

        private int GetThumbnailWorkerCount()
        {
            int cpu = Math.Max(2, Environment.ProcessorCount);
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => Math.Clamp(cpu, 6, 16),
                "Ultra" => Math.Clamp(cpu * 2, 12, 32),
                _ => Math.Clamp(Math.Max(3, cpu / 2), 3, 12)
            };
        }

        private int GetFtpThumbnailWorkerCount()
        {
            int cpu = Math.Max(2, Environment.ProcessorCount);
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => Math.Clamp(cpu, 4, 10),
                "Ultra" => Math.Clamp(cpu * 2, 8, 16),
                _ => Math.Clamp(Math.Max(3, cpu / 2), 3, 6)
            };
        }

        private ThumbnailHints? BuildThumbnailHints()
        {
            int deferMs = ThumbnailPerformanceMode switch
            {
                "Low" => 48,
                "Max" => 0,
                "Ultra" => 0,
                _ => 18
            };

            return deferMs > 0 ? new ThumbnailHints { DeferRawShellMilliseconds = deferMs } : null;
        }

        private FtpThumbnailLoadOptions BuildFtpThumbnailLoadOptions()
        {
            return new FtpThumbnailLoadOptions
            {
                DownloadParallelism = Math.Min(GetFtpThumbnailWorkerCount(), 6),
                DecodeParallelism = GetThumbnailWorkerCount(),
                PerformanceMode = ThumbnailPerformanceMode
            };
        }

        private static List<ImportItem> OrderItemsForViewportPriority(List<ItemGroup> groups)
        {
            return groups
                .Select((group, index) => (group, index))
                .OrderByDescending(x => x.group.IsExpanded)
                .ThenBy(x => x.index)
                .SelectMany(x => x.group.Items)
                .ToList();
        }

        private static string BuildSourceKey(FtpSourceItem ftp)
        {
            return FtpPathNormalizer.BuildFtpSourceKey(ftp.Host, ftp.Port, ftp.RemoteFolder);
        }

        private static string BuildSourceKey(string localPath)
        {
            return $"local|{localPath}";
        }

        private async void ExecuteBuildSelectedPreviews()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_ScanSourceFirst");
                return;
            }

            var selectedGroups = Groups.Where(g => g.IsSelected).ToList();
            if (selectedGroups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SelectAtLeastOneGroup");
                return;
            }

            try
            {
                ShowScanProgressDialog = true;
                ScanDialogTitle = AppLocalizer.Get("Vm_Scan_BuildingPreviewsDialogTitle");
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = selectedGroups.Count;
                ScannedFiles = 0;
                TotalFilesToScan = selectedGroups.SelectMany(g => g.Items).Count();
                ScanProgressMessage = AppLocalizer.Format("Vm_Scan_BuildingPreviewsForFolders", selectedGroups.Count);
                StatusMessage = AppLocalizer.Get("Vm_Status_BuildingPreviews");

                string sourceLabel = SelectedSource is FtpSourceItem ftpSource
                    ? $"{ftpSource.Host}{NormalizeFtpPath(ftpSource.RemoteFolder)}"
                    : SelectedSource.ToString() ?? "source";

                await LoadThumbnailsAsync(selectedGroups, SelectedSource, sourceLabel);
                ScannedFolders = TotalFoldersToScan;
            }
            catch (Exception ex)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_PreviewBuildFailed", ex.Message);
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }
    }
}
