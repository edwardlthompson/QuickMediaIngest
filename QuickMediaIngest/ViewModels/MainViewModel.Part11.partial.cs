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

            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

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
            int ftpWorkers = GetFtpThumbnailWorkerCount();

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
                            thumb = _thumbnailService.GetThumbnail(item.SourcePath, BuildThumbnailHints());
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

            Task ftpTask = Parallel.ForEachAsync(
                needFtp,
                new ParallelOptions { MaxDegreeOfParallelism = ftpWorkers },
                async (item, ct) =>
                {
                    string itemKey = BuildItemKey(item);
                    if (!ftpSourcesByKey.TryGetValue(item.SourceId, out var ftp))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                        BumpProgress();
                        return;
                    }

                    string ext = Path.GetExtension(item.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".jpg";
                    }

                    string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                    try
                    {
                        int timeoutSeconds = IsLikelyVideoPath(item.FileName) ? 120 : 30;
                        bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, timeoutSeconds).ConfigureAwait(false);
                        if (downloaded)
                        {
                            object? thumb = await Task.Run(() => _thumbnailService.GetThumbnail(tempPath, BuildThumbnailHints()), ct).ConfigureAwait(false);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
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
                        }
                        else
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                        }
                    }
                    catch
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                        }
                        catch
                        {
                            // Ignore temp cleanup failures.
                        }
                    }

                    BumpProgress();
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
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => 8,
                "Ultra" => 16,
                _ => 4
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
