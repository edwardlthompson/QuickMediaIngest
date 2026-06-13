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

        private async Task<int> LoadFtpThumbnailBatchAsync(
            List<ImportItem> items,
            FtpSourceItem ftp,
            int totalItemCount,
            int startIndex,
            bool updateScanProgressMessage)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

            int loadedCount = 0;
            int skippedCount = 0;
            int processedCount = 0;
            int workerCount = GetFtpThumbnailWorkerCount();

            var indexedItems = items.Select((item, index) => (item, index)).ToList();
            await Parallel.ForEachAsync(
                indexedItems,
                new ParallelOptions { MaxDegreeOfParallelism = workerCount },
                async (entry, _) =>
                {
                    var item = entry.item;
                    int overallIndex = startIndex + entry.index + 1;
                    string itemKey = BuildItemKey(item);

                    string ext = Path.GetExtension(item.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".jpg";
                    }

                    string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                    try
                    {
                        if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                        {
                            Interlocked.Increment(ref loadedCount);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                item.Thumbnail = cachedThumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            });
                        }
                        else
                        {
                            int timeoutSeconds = IsLikelyVideoPath(item.FileName) ? 120 : 30;
                            bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, timeoutSeconds);
                            if (!downloaded)
                            {
                                Interlocked.Increment(ref skippedCount);
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                                });
                            }
                            else
                            {
                                var thumb = await Task.Run(() => _thumbnailService.GetThumbnail(tempPath, BuildThumbnailHints()));
                                if (thumb != null)
                                {
                                    Interlocked.Increment(ref loadedCount);
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        item.Thumbnail = thumb;
                                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                        _thumbnailByItemKey[itemKey] = thumb;
                                    });
                                }
                                else
                                {
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref skippedCount);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                        });
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

                    int processed = Interlocked.Increment(ref processedCount);
                    if (processed == 1 || processed % 10 == 0 || processed == items.Count)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ScannedFiles = startIndex + processed;
                            TotalFilesToScan = totalItemCount;
                            ScanProgressPercent = totalItemCount > 0 ? ((startIndex + processed) * 100) / totalItemCount : 0;
                            CurrentScanFolder = item.SourcePath;
                            CurrentScanFolderProcessedFiles = processed;
                            CurrentScanFolderTotalFiles = items.Count;
                            if (updateScanProgressMessage)
                            {
                                ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingFtpPreviewsProgress", Math.Min(totalItemCount, startIndex + processed), totalItemCount);
                            }
                        });
                    }
                });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (skippedCount > 0)
                {
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_LoadingFtpPreviewsWithSkips", Math.Min(totalItemCount, startIndex + items.Count), totalItemCount, skippedCount);
                }

                FtpThumbnailPhaseDetail = $"FTP previews: loaded {loadedCount}/{items.Count} in batch · skipped {skippedCount}";
                RefreshPreviewHealthSummary();
            });

            return loadedCount;
        }

        private static async Task<bool> DownloadFtpFileWithTimeoutAsync(
            FtpSourceItem ftp,
            string remotePath,
            string localPath,
            int timeoutSeconds)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));
            try
            {
                await Task.Run(() => DownloadFtpFileSync(ftp, remotePath, localPath, timeout.Token), timeout.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLikelyVideoPath(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mov", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".avi", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".wmv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".3gp", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mts", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".m2ts", StringComparison.OrdinalIgnoreCase);
        }

        private static void DownloadFtpFileSync(FtpSourceItem ftp, string remotePath, string localPath, CancellationToken cancellationToken)
        {
            Uri uri = BuildFtpFileUri(ftp.Host, ftp.Port, remotePath);

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(ftp.User, ftp.Pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;

            using var response = (FtpWebResponse)request.GetResponse();
            using var source = response.GetResponseStream();
            using var dest = File.Create(localPath);

            if (source == null)
            {
                return;
            }

            byte[] buffer = new byte[65536];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dest.Write(buffer, 0, read);
            }
        }

        private static Uri BuildFtpFileUri(string host, int port, string remotePath)
        {
            string normalized = NormalizeFtpPath(remotePath);
            string encodedPath = string.Join("/", normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

            string uriText = string.IsNullOrEmpty(encodedPath)
                ? $"ftp://{host}:{port}/"
                : $"ftp://{host}:{port}/{encodedPath}";

            return new Uri(uriText);
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
