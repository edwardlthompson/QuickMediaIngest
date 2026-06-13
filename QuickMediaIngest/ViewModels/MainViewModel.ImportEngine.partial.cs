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

        private async void ExecuteImport()
        {
            if (Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_StatusNothingToImport");
                return;
            }

            SyncStackSelections(Groups);
            var selectedGroups = Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList();
            int totalFiles = selectedGroups.Sum(g => g.Items.Count(i => i.IsSelected));
            if (totalFiles == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_StatusNoFilesSelected");
                return;
            }

            ShowPostDeleteRecoveryBanner = false;

            if (ConfirmBeforeImport)
            {
                string confirmBody = BuildImportConfirmationMessage(selectedGroups, totalFiles);
                MessageBoxResult confirmResult = MessageBox.Show(
                    confirmBody,
                    AppLocalizer.Get("Vm_ConfirmImportTitle"),
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.OK)
                {
                    StatusMessage = AppLocalizer.Get("Vm_StatusImportCanceled");
                    return;
                }
            }

            if (ShowCompactImportSummaryModal)
            {
                long estBytes = ImportDestinationEstimator.SumSelectedBytes(selectedGroups);
                long? free = ImportDestinationEstimator.TryGetFreeBytes(DestinationRoot);
                string estMb = (estBytes / (1024d * 1024d)).ToString("0.##", CultureInfo.CurrentCulture);
                string freeGb = free.HasValue
                    ? (free.Value / (1024d * 1024d * 1024d)).ToString("0.##", CultureInfo.CurrentCulture)
                    : "?";
                MessageBox.Show(
                    AppLocalizer.Format("Msg_ImportSummary_Body", estMb, freeGb),
                    AppLocalizer.Get("Msg_ImportSummary_Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            IsImporting = true;
            SavePendingImportPlan(selectedGroups);
            _logger.LogInformation("Import started. SelectedGroups={GroupCount}, SelectedFiles={FileCount}", selectedGroups.Count, totalFiles);
            TotalFilesForImport = totalFiles;
            CurrentFileBeingImported = 0;
            ProcessedFilesForImport = 0;
            FailedFilesForImport = 0;
            FailedImportRecords.Clear();
            CurrentGroupFileBeingImported = 0;
            TotalFilesInCurrentGroup = 0;
            CurrentGroupProgressPercent = 0;
            CurrentImportGroupTitle = string.Empty;
            ImportElapsedText = "00:00:00";
            ImportEtaText = "--:--:--";
            ImportDataRateText = "-- MB/s";
            ShowImportProgressDialog = true;
            StatusMessage = AppLocalizer.Get("Vm_Status_StartingImport");
            ProgressPercent = 0;
            _processedBytesForImport = 0;

            AddNotificationFeedEntry(AppLocalizer.Get("Notify_ImportSessionStart"), isSessionDivider: true);

            _importStartedAtUtc = DateTime.UtcNow;
            _importCancellationSource?.Dispose();
            _importCancellationSource = new CancellationTokenSource();
            var importCts = _importCancellationSource;
            var stopwatch = Stopwatch.StartNew();
            var timerTask = Task.Run(async () =>
            {
                while (!importCts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, importCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ImportElapsedText = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");

                        if (ProcessedFilesForImport > 0 && TotalFilesForImport > ProcessedFilesForImport)
                        {
                            double avgSecondsPerFile = stopwatch.Elapsed.TotalSeconds / ProcessedFilesForImport;
                            double remainingSeconds = (TotalFilesForImport - ProcessedFilesForImport) * avgSecondsPerFile;
                            ImportEtaText = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds)).ToString(@"hh\:mm\:ss");
                        }
                        else if (TotalFilesForImport <= ProcessedFilesForImport && TotalFilesForImport > 0)
                        {
                            ImportEtaText = "00:00:00";
                        }

                        if (stopwatch.Elapsed.TotalSeconds > 0.25 && _processedBytesForImport > 0)
                        {
                            double bytesPerSecond = _processedBytesForImport / stopwatch.Elapsed.TotalSeconds;
                            ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
                        }
                    });
                }
            });

            try
            {

                if (SelectedSource is UnifiedSourceItem)
                {
                    await ExecuteUnifiedImportAsync(selectedGroups, importCts.Token);
                }
                else
                {
                    IFileProvider provider = _fileProviderFactory.CreateLocalProvider();
                    if (SelectedSource is FtpSourceItem ftp)
                    {
                        provider = _fileProviderFactory.CreateFtpProvider(ftp.Host, ftp.Port, ftp.User, ftp.Pass);
                    }
                    else if (SelectedSource is AdbSourceItem adb)
                    {
                        provider = _fileProviderFactory.CreateAdbProvider(adb.DeviceSerial);
                    }

                    try
                    {
                        var engine = _ingestEngineFactory.Create(provider);

                        engine.ProgressChanged += (percent, msg) =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusMessage = msg;
                            });
                        };

                        engine.ItemProcessed += progress =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProcessedFilesForImport++;
                                if (!progress.Success)
                                {
                                    FailedFilesForImport++;
                                    FailedImportRecords.Add(new FailedImportRecord
                                    {
                                        SourcePath = progress.SourcePath,
                                        FileName = progress.FileName,
                                        ErrorMessage = string.IsNullOrWhiteSpace(progress.ErrorMessage) ? "Import failed." : progress.ErrorMessage
                                    });
                                }
                                else
                                {
                                    _processedBytesForImport += Math.Max(0, progress.FileSizeBytes);
                                }

                                CurrentFileBeingImported = ProcessedFilesForImport - FailedFilesForImport;
                                CurrentGroupFileBeingImported = progress.GroupCurrent;
                                TotalFilesInCurrentGroup = progress.GroupTotal;
                                CurrentGroupProgressPercent = progress.GroupTotal > 0 ? (progress.GroupCurrent * 100) / progress.GroupTotal : 0;
                                CurrentImportGroupTitle = progress.GroupTitle;
                                ProgressPercent = TotalFilesForImport > 0 ? (ProcessedFilesForImport * 100) / TotalFilesForImport : 0;

                                string state = progress.Success ? "Copying" : "Failed";
                                StatusMessage = $"{state} {progress.FileName} | overall {ProcessedFilesForImport}/{TotalFilesForImport} | group {progress.GroupCurrent}/{progress.GroupTotal}";
                            });
                        };

                        await System.Threading.Tasks.Task.Run(async () =>
                        {
                            foreach (var group in selectedGroups)
                            {
                                await engine.IngestGroupAsync(
                                    group,
                                    DestinationRoot,
                                    NamingTemplate,
                                    importCts.Token,
                                    CreateIngestOptions(group),
                                    DeleteAfterImport);
                            }
                        });
                    }
                    finally
                    {
                        if (provider is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
                        }
                    }
                }

                string completionMsg = FailedFilesForImport > 0
                    ? $"✓ Import completed with warnings. Imported {CurrentFileBeingImported}/{TotalFilesForImport}, failed {FailedFilesForImport}."
                    : $"✓ Import completed successfully! Imported {CurrentFileBeingImported}/{TotalFilesForImport}.";
                LastImportSummary =
                    $"Last import — succeeded {CurrentFileBeingImported}/{TotalFilesForImport}, failed {FailedFilesForImport}. " +
                    $"Destination: {DestinationRoot}. " +
                    $"Reports folder: \"_ImportReports\" under your destination.";
                ProgressPercent = 100;
                CurrentGroupProgressPercent = 100;
                ImportEtaText = "00:00:00";
                if (stopwatch.Elapsed.TotalSeconds > 0.25 && _processedBytesForImport > 0)
                {
                    double bytesPerSecond = _processedBytesForImport / stopwatch.Elapsed.TotalSeconds;
                    ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
                }

                _suppressStatusFeedFromStatusMessage = true;
                try
                {
                    StatusMessage = completionMsg;
                    AccessibilityAnnouncement = completionMsg;
                    AddNotificationFeedEntry(completionMsg, useSuccessAccent: true);
                }
                finally
                {
                    _suppressStatusFeedFromStatusMessage = false;
                }

                ShowPostDeleteRecoveryBanner =
                    DeleteAfterImport &&
                    ProcessedFilesForImport > FailedFilesForImport;

                ShowWindowsImportCompletionNotification(CurrentFileBeingImported, TotalFilesForImport, FailedFilesForImport);

                SaveImportHistoryRecord(stopwatch.Elapsed);
                ExportImportReportArtifact(stopwatch.Elapsed, selectedGroups);
                LastSessionDestinationRoot = DestinationRoot;

                // Brief beat so the completion state is visible (avoids ~1s dead air from the old 1000ms delay).
                await System.Threading.Tasks.Task.Delay(400);
                ShowImportProgressDialog = false;

                RunPostImportActions(selectedGroups);
                Groups.Clear();
                _sourceItemsCache.Clear();
                ClearPendingImportPlan();
                if (SelectedSource != null)
                {
                    LoadSourceItems(SelectedSource);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed.");
                StatusMessage = $"Import failed: {ex.Message}";
                string failAnnouncement = $"{AppLocalizer.Get("Vm_Status_ImportFailedShort")}: {ex.Message}";
                AccessibilityAnnouncement = failAnnouncement;
                AddNotificationFeedEntry(failAnnouncement);
            }
            finally
            {
                importCts.Cancel();
                try
                {
                    await timerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when import completes.
                }

                ImportElapsedText = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                IsImporting = false;
                ShowImportProgressDialog = false;
                _importCancellationSource?.Dispose();
                _importCancellationSource = null;
                _logger.LogInformation("Import finished. Imported={ImportedCount}, Failed={FailedCount}", CurrentFileBeingImported, FailedFilesForImport);
            }

            TryStartNextQueuedImport();
        }
    }
}
