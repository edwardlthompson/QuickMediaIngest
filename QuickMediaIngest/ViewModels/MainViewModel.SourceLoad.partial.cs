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

        private async void LoadSourceItems(object source, bool forceRefresh = false)
        {
            _ftpThumbnailCts?.Cancel();
            _ftpThumbnailCts?.Dispose();
            _ftpThumbnailCts = new CancellationTokenSource();
            CancellationToken thumbnailToken = _ftpThumbnailCts.Token;

            foreach (var existing in Groups)
            {
                existing.PropertyChanged -= Group_PropertyChanged;
            }
            Groups.Clear();
            EnsureFilteredItemsViewSource();

            string sourceLabel = source.ToString() ?? "source";
            var ftpListingFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string sourceKey = string.Empty;
            try
            {
                _logger.LogInformation("Loading source items for {SourceLabel}.", sourceLabel);
                List<QuickMediaIngest.Core.Models.ImportItem> items;
                ShowScanProgressDialog = true;
                ScanDialogTitle = AppLocalizer.Get("Vm_Scan_LoadingImportList");
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = 0;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = AppLocalizer.Get("Vm_Scan_PreparingScan");
                CurrentScanFolder = "/";
                CurrentScanFolderProcessedFiles = 0;
                CurrentScanFolderTotalFiles = 0;

                if (source is FtpSourceItem ftp)
                {
                    string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(ScanPath) ? ftp.RemoteFolder : ScanPath);
                    ftp.RemoteFolder = remotePath;
                    EnsureFtpSourceCredentials(ftp);
                    sourceLabel = $"{ftp.Host}{remotePath}";
                    sourceKey = BuildSourceKey(ftp);

                    if (!forceRefresh && _sourceItemsCache.TryGetValue(sourceKey, out var cachedFtpItems))
                    {
                        items = CloneItems(cachedFtpItems);
                        ScanProgressMessage = AppLocalizer.Get("Vm_Scan_LoadedFromCache");
                        ScannedFiles = items.Count;
                        TotalFilesToScan = items.Count;
                        ScanProgressPercent = 100;
                        goto BuildGroups;
                    }

                    StatusMessage = AppLocalizer.Format("Vm_Status_ScanningFtp", sourceLabel);
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_ProgressFtpFolders", remotePath);

                    items = await _ftpScanner.ScanAsync(
                        ftp.Host,
                        ftp.Port,
                        ftp.User,
                        ftp.Pass,
                        remotePath,
                        ScanIncludeSubfolders,
                        120,
                        CancellationToken.None,
                        progress =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ScannedFolders = progress.ProcessedFolders;
                                TotalFoldersToScan = Math.Max(progress.TotalFolders, progress.ProcessedFolders);
                                ScannedFiles = progress.ProcessedFiles;
                                TotalFilesToScan = Math.Max(progress.TotalFiles, progress.ProcessedFiles);
                                CurrentScanFolder = progress.CurrentFolder;
                                CurrentScanFolderProcessedFiles = progress.CurrentFolderProcessedFiles;
                                CurrentScanFolderTotalFiles = progress.CurrentFolderTotalFiles;

                                string noteSuffix = string.IsNullOrWhiteSpace(progress.Note) ? string.Empty : $" | {progress.Note}";
                                if (progress.Phase == "Prescan")
                                {
                                    ScanProgressPercent = 0;
                                    int prescanFolderDenom = Math.Max(progress.TotalFolders, progress.ProcessedFolders);
                                    ScanProgressMessage = AppLocalizer.Format(
                                            "Vm_FtpPrescanProgress",
                                            progress.ProcessedFolders,
                                            prescanFolderDenom,
                                            progress.TotalFiles,
                                            progress.SkippedFolders,
                                            progress.CurrentFolder)
                                        + noteSuffix;
                                }
                                else
                                {
                                    ScanProgressPercent = progress.TotalFiles > 0
                                        ? (progress.ProcessedFiles * 100) / progress.TotalFiles
                                        : (progress.TotalFolders > 0 ? (progress.ProcessedFolders * 100) / progress.TotalFolders : 0);
                                    int fileDenom = Math.Max(progress.TotalFiles, progress.ProcessedFiles);
                                    int currentFolderDenom = Math.Max(progress.CurrentFolderTotalFiles, progress.CurrentFolderProcessedFiles);
                                    int folderDenom = Math.Max(progress.TotalFolders, progress.ProcessedFolders);
                                    ScanProgressMessage = AppLocalizer.Format(
                                            "Vm_FtpScanProgressDetail",
                                            progress.ProcessedFiles,
                                            fileDenom,
                                            progress.CurrentFolderProcessedFiles,
                                            currentFolderDenom,
                                            progress.ProcessedFolders,
                                            folderDenom,
                                            progress.SkippedFolders,
                                            progress.CurrentFolder)
                                        + noteSuffix;
                                }

                                if (!string.IsNullOrWhiteSpace(progress.Note) && progress.Note.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                                {
                                    ftpListingFailures.Add($"{progress.CurrentFolder} - {progress.Note}");
                                }
                            });
                        });
                }
                else if (source is string drive)
                {
                    string localPath = ResolveLocalScanPath(drive, ScanPath);
                    sourceLabel = localPath;
                    sourceKey = BuildSourceKey(localPath);

                    if (!forceRefresh && _sourceItemsCache.TryGetValue(sourceKey, out var cachedLocalItems))
                    {
                        items = CloneItems(cachedLocalItems);
                        ScanProgressMessage = AppLocalizer.Get("Vm_Scan_LoadedFromCache");
                        ScannedFiles = items.Count;
                        TotalFilesToScan = items.Count;
                        ScanProgressPercent = 100;
                        goto BuildGroups;
                    }

                    if (!Directory.Exists(localPath))
                    {
                        StatusMessage = AppLocalizer.Format("Vm_Status_ScanPathNotFound", localPath);
                        return;
                    }

                    StatusMessage = AppLocalizer.Format("Vm_Status_ScanningLocal", localPath);
                    ScanProgressMessage = AppLocalizer.Format("Vm_Scan_ProgressLocalFolders", localPath);
                    CurrentScanFolder = localPath;
                    items = await Task.Run(() => _scanner.Scan(localPath, ScanIncludeSubfolders, (scanned, total) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ScannedFolders = scanned;
                            TotalFoldersToScan = total;
                            ScanProgressPercent = total > 0 ? (scanned * 100) / total : 0;
                            ScanProgressMessage = AppLocalizer.Format("Vm_Scan_ProgressFoldersCount", scanned, total);
                            ScannedFiles = 0;
                            TotalFilesToScan = 0;
                            CurrentScanFolderProcessedFiles = 0;
                            CurrentScanFolderTotalFiles = 0;
                        });
                    }));
                }
                else return;

                BuildGroups:
                StampItems(items, sourceKey, source is FtpSourceItem);
                ApplySkippedFolderFilters(items, userExcludedFolders);
                _sourceItemsCache[sourceKey] = CloneItems(items);

                _currentSourceItems = items;
                RebuildGroupsFromCurrentItems();

                if (Groups.Count > 0)
                {
                    if (source is FtpSourceItem)
                    {
                        ShowScanProgressDialog = false;
                        ScanDialogTitle = AppLocalizer.Get("Vm_Scan_BuildingPreviewsDialogTitle");
                        StatusMessage = AppLocalizer.Format("Vm_Scan_LoadingFtpPreviewsProgress", 0, items.Count);
                    }

                    await LoadThumbnailsAsync(Groups.ToList(), source, sourceLabel, thumbnailToken);
                    _sourceItemsCache[sourceKey] = CloneItems(_currentSourceItems);
                }
                else
                {
                    StatusMessage = $"Found 0 group(s) from {sourceLabel}.";
                }

                MaybeShowSkippedFoldersScanReport(sourceLabel, ftpListingFailures, userExcludedFolders);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Source scan was canceled for {SourceLabel}.", sourceLabel);
                StatusMessage = $"FTP scan was canceled while scanning {sourceLabel}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning source {SourceLabel}.", sourceLabel);
                StatusMessage = $"Error scanning {sourceLabel}: {ex.Message}";
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }
    }
}
