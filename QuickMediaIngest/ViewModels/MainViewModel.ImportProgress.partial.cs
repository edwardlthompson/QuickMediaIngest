using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Application = System.Windows.Application;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        private ImportByteProgressTracker? _importByteProgressTracker;
        private Stopwatch? _importProgressStopwatch;
        private readonly object _importUiProgressGate = new();
        private ImportByteProgressSnapshot? _pendingByteProgressSnapshot;
        private bool _importByteProgressUiScheduled;

        private void BeginImportByteProgressTracking(List<ItemGroup> selectedGroups, int totalFiles, Stopwatch stopwatch)
        {
            TotalBytesForImport = ImportDestinationEstimator.SumSelectedBytes(selectedGroups);

            _importProgressStopwatch = stopwatch;
            ClearImportByteProgressTracking();
            _importByteProgressTracker = new ImportByteProgressTracker(TotalBytesForImport, totalFiles);
            _importByteProgressTracker.ProgressChanged += OnImportByteProgressChanged;
            ProcessedBytesForImport = 0;
        }

        private void ClearImportByteProgressTracking()
        {
            if (_importByteProgressTracker != null)
            {
                _importByteProgressTracker.ProgressChanged -= OnImportByteProgressChanged;
                _importByteProgressTracker = null;
            }

            _importProgressStopwatch = null;
            lock (_importUiProgressGate)
            {
                _pendingByteProgressSnapshot = null;
                _importByteProgressUiScheduled = false;
            }
        }

        /// <summary>
        /// Never use sync Dispatcher.Invoke from copy threads — every 1MB buffer used to block
        /// workers on the UI thread and freeze imports mid-card.
        /// </summary>
        private void OnImportByteProgressChanged(ImportByteProgressSnapshot snapshot)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                ApplyImportByteProgress(snapshot, null);
                return;
            }

            lock (_importUiProgressGate)
            {
                _pendingByteProgressSnapshot = snapshot;
                if (_importByteProgressUiScheduled)
                {
                    return;
                }

                _importByteProgressUiScheduled = true;
            }

            dispatcher.BeginInvoke(
                () =>
                {
                    ImportByteProgressSnapshot? latest;
                    lock (_importUiProgressGate)
                    {
                        latest = _pendingByteProgressSnapshot;
                        _pendingByteProgressSnapshot = null;
                        _importByteProgressUiScheduled = false;
                    }

                    if (latest != null)
                    {
                        ApplyImportByteProgress(latest, null);
                    }
                },
                DispatcherPriority.Background);
        }

        private void WireIngestEngineProgress(IngestEngine engine)
        {
            engine.ProgressChanged += (_, msg) =>
            {
                PostImportUi(() => StatusMessage = msg);
            };

            engine.ItemProcessed += progress =>
            {
                PostImportUi(() =>
                {
                    ImportByteProgressSnapshot snapshot = _importByteProgressTracker?.GetSnapshot()
                        ?? new ImportByteProgressSnapshot
                        {
                            TotalBytes = TotalBytesForImport,
                            TotalFiles = TotalFilesForImport,
                            CompletedBytes = ProcessedBytesForImport,
                            EffectiveBytes = ProcessedBytesForImport,
                        };

                    ApplyImportByteProgress(snapshot, progress);
                });
            };
        }

        private static void PostImportUi(Action action)
        {
            Dispatcher? dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action, DispatcherPriority.Background);
        }

        private void RegisterManualImportFailure(ImportItem item, string errorMessage)
        {
            _importByteProgressTracker?.RegisterFileStarted(item.SourcePath, Math.Max(0, item.FileSize));
            _importByteProgressTracker?.RegisterFileCompleted(item.SourcePath, Math.Max(0, item.FileSize), success: false);

            ImportByteProgressSnapshot snapshot = _importByteProgressTracker?.GetSnapshot()
                ?? new ImportByteProgressSnapshot { TotalBytes = TotalBytesForImport, TotalFiles = TotalFilesForImport };

            ApplyImportByteProgress(snapshot, new IngestProgressInfo
            {
                SourcePath = item.SourcePath,
                FileName = item.FileName,
                Success = false,
                ErrorMessage = errorMessage,
                IsStarted = false,
            });
        }

        private void ApplyImportByteProgress(ImportByteProgressSnapshot snapshot, IngestProgressInfo? fileInfo)
        {
            ProcessedBytesForImport = snapshot.CompletedBytes;
            ProcessedFilesForImport = snapshot.FilesProcessed;
            FailedFilesForImport = snapshot.FilesFailed;
            CurrentFileBeingImported = snapshot.FilesCompleted;

            if (TotalBytesForImport > 0)
            {
                long capped = Math.Min(snapshot.EffectiveBytes, TotalBytesForImport);
                ProgressPercent = (int)((capped * 100) / TotalBytesForImport);
            }
            else if (TotalFilesForImport > 0)
            {
                ProgressPercent = (ProcessedFilesForImport * 100) / TotalFilesForImport;
            }

            if (_importProgressStopwatch != null)
            {
                UpdateImportEtaAndRate(_importProgressStopwatch, snapshot);
            }

            if (fileInfo == null)
            {
                return;
            }

            if (fileInfo.IsStarted)
            {
                StatusMessage = AppLocalizer.Format(
                    "Vm_Status_ImportStartingLine",
                    fileInfo.FileName,
                    snapshot.FilesInFlight,
                    ProcessedFilesForImport,
                    TotalFilesForImport);
                return;
            }

            if (!fileInfo.Success)
            {
                FailedImportRecords.Add(new FailedImportRecord
                {
                    SourcePath = fileInfo.SourcePath,
                    FileName = fileInfo.FileName,
                    ErrorMessage = string.IsNullOrWhiteSpace(fileInfo.ErrorMessage) ? "Import failed." : fileInfo.ErrorMessage,
                });
            }

            CurrentGroupFileBeingImported = fileInfo.GroupCurrent;
            TotalFilesInCurrentGroup = fileInfo.GroupTotal;
            CurrentGroupProgressPercent = fileInfo.GroupTotal > 0 ? (fileInfo.GroupCurrent * 100) / fileInfo.GroupTotal : 0;
            CurrentImportGroupTitle = fileInfo.GroupTitle;

            string state = fileInfo.Success
                ? AppLocalizer.Get("Vm_Import_StateCopying")
                : AppLocalizer.Get("Vm_Import_StateFailed");
            StatusMessage = AppLocalizer.Format(
                "Vm_Status_ImportProgressLine",
                state,
                fileInfo.FileName,
                ProcessedFilesForImport,
                TotalFilesForImport,
                fileInfo.GroupCurrent,
                fileInfo.GroupTotal);
        }

        private void UpdateImportEtaAndRate(Stopwatch stopwatch, ImportByteProgressSnapshot snapshot)
        {
            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds <= 0)
            {
                return;
            }

            long effectiveBytes = Math.Max(0, snapshot.EffectiveBytes);
            double bytesPerSecond = effectiveBytes / elapsedSeconds;
            if (bytesPerSecond > 0)
            {
                ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
            }

            if (TotalBytesForImport > 0 && effectiveBytes > 0 && effectiveBytes < TotalBytesForImport)
            {
                double remainingBytes = TotalBytesForImport - effectiveBytes;
                ImportEtaText = TimeSpan.FromSeconds(Math.Max(0, remainingBytes / bytesPerSecond)).ToString(@"hh\:mm\:ss");
            }
            else if (TotalBytesForImport > 0 && effectiveBytes >= TotalBytesForImport)
            {
                ImportEtaText = "00:00:00";
            }
            else if (TotalFilesForImport > 0 && snapshot.FilesProcessed >= TotalFilesForImport)
            {
                ImportEtaText = "00:00:00";
            }
            else if (TotalFilesForImport > snapshot.FilesProcessed && snapshot.FilesProcessed > 0)
            {
                double avgSecondsPerFile = elapsedSeconds / snapshot.FilesProcessed;
                double remainingSeconds = (TotalFilesForImport - snapshot.FilesProcessed) * avgSecondsPerFile;
                ImportEtaText = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds)).ToString(@"hh\:mm\:ss");
            }
        }
    }
}
