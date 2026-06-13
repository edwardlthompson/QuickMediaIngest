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

        private string BuildLocalSourceRuleKey(string localPath)
        {
            return $"local-device|{ResolveDeviceIdFromLocalPath(localPath)}";
        }

        private void SaveImportHistoryRecord(TimeSpan duration)
        {
            try
            {
                var record = new ImportHistoryRecord
                {
                    StartedAtLocal = _importStartedAtUtc == DateTime.MinValue ? DateTime.Now : _importStartedAtUtc.ToLocalTime(),
                    DurationSeconds = Math.Max(0, duration.TotalSeconds),
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    Source = SelectedSource?.ToString() ?? "Unknown",
                    Destination = DestinationRoot
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImportHistoryRecords.Insert(0, record);
                    while (ImportHistoryRecords.Count > 50)
                    {
                        ImportHistoryRecords.RemoveAt(ImportHistoryRecords.Count - 1);
                    }
                });

                string path = GetImportHistoryPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Path.GetTempPath());
                string json = System.Text.Json.JsonSerializer.Serialize(ImportHistoryRecords.ToList());
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore history persistence errors.
            }
        }

        private string BuildSelectedSourceId()
        {
            return SelectedSource switch
            {
                FtpSourceItem ftp => BuildSourceKey(ftp),
                string local => BuildLocalSourceRuleKey(local),
                UnifiedSourceItem => "unified",
                _ => "unknown"
            };
        }

        private static string GetPendingImportPlanPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "pending-import.json");
        }

        private void SavePendingImportPlan(List<ItemGroup> selectedGroups)
        {
            try
            {
                var selectedPaths = selectedGroups
                    .SelectMany(g => g.Items)
                    .Where(i => i.IsSelected)
                    .Select(i => i.SourcePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var plan = new PendingImportPlan
                {
                    CreatedAt = DateTime.Now,
                    SourceId = BuildSelectedSourceId(),
                    SourceDisplay = SelectedSource?.ToString() ?? "Unknown",
                    DestinationRoot = DestinationRoot,
                    NamingTemplate = NamingTemplate,
                    SelectedSourcePaths = selectedPaths
                };
                string json = System.Text.Json.JsonSerializer.Serialize(plan, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetPendingImportPlanPath(), json);
            }
            catch
            {
                // Ignore pending plan persistence failures.
            }
        }

        private void ClearPendingImportPlan()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore clear failures.
            }
        }

        private void ExportImportReportArtifact(TimeSpan duration, List<ItemGroup> selectedGroups)
        {
            try
            {
                string reportDir = Path.Combine(DestinationRoot, "_ImportReports");
                Directory.CreateDirectory(reportDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var report = new ImportReportArtifact
                {
                    GeneratedAt = DateTime.Now,
                    Source = SelectedSource?.ToString() ?? "Unknown",
                    Destination = DestinationRoot,
                    DurationSeconds = duration.TotalSeconds,
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    VerificationMode = VerificationMode,
                    DuplicatePolicy = DuplicatePolicy,
                    Failed = FailedImportRecords.ToList()
                };

                string jsonPath = Path.Combine(reportDir, $"import-report-{timestamp}.json");
                File.WriteAllText(jsonPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                var text = new StringBuilder();
                text.AppendLine($"Import Report - {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                text.AppendLine($"Source: {report.Source}");
                text.AppendLine($"Destination: {report.Destination}");
                text.AppendLine($"DurationSeconds: {report.DurationSeconds:0.##}");
                text.AppendLine($"Imported: {report.FilesImported}/{report.FilesSelected}");
                text.AppendLine($"Failed: {report.FailedFiles}");
                text.AppendLine($"Verification: {report.VerificationMode}");
                text.AppendLine($"DuplicatePolicy: {report.DuplicatePolicy}");
                if (FailedImportRecords.Count > 0)
                {
                    text.AppendLine("Failed Files:");
                    foreach (var failure in FailedImportRecords)
                    {
                        text.AppendLine($"- {failure.FileName} | {failure.ErrorMessage}");
                    }
                }
                string txtPath = Path.Combine(reportDir, $"import-report-{timestamp}.txt");
                File.WriteAllText(txtPath, text.ToString());
            }
            catch
            {
                // Ignore report export errors.
            }
        }

        private void LoadImportHistory()
        {
            try
            {
                string path = GetImportHistoryPath();
                if (!File.Exists(path))
                {
                    return;
                }

                string json = File.ReadAllText(path);
                var records = System.Text.Json.JsonSerializer.Deserialize<List<ImportHistoryRecord>>(json) ?? new List<ImportHistoryRecord>();

                ImportHistoryRecords.Clear();
                foreach (var record in records.OrderByDescending(r => r.StartedAtLocal).Take(50))
                {
                    ImportHistoryRecords.Add(record);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Import history file could not be loaded.");
            }
        }

        private static string GetImportHistoryPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            return Path.Combine(folder, "import-history.json");
        }

        /// <summary>
        /// <see cref="DriveInfo.IsReady"/> can block on unreachable volumes; never call it synchronously on the UI thread.
        /// </summary>
        private async Task<bool> IsDriveReadyWithTimeoutAsync(DriveInfo drive, int timeoutMs = 1500)
        {
            try
            {
                Task<bool> task = Task.Run(() =>
                {
                    try
                    {
                        return drive.IsReady;
                    }
                    catch
                    {
                        return false;
                    }
                });
                return await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Drive IsReady check timed out for {DriveName} ({DriveType}) after {TimeoutMs} ms; treating as not ready.",
                    drive.Name,
                    drive.DriveType,
                    timeoutMs);
                return false;
            }
        }

        /// <summary>
        /// Lists fixed/removable drives off the UI thread, with a bounded wait per volume for IsReady.
        /// </summary>
        private async Task<List<DriveInfo>> EnumerateCandidateDrivesAsync()
        {
            DriveInfo[] all;
            try
            {
                all = DriveInfo.GetDrives();
            }
            catch
            {
                return new List<DriveInfo>();
            }

            var typed = all
                .Where(d => d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed)
                .ToList();

            var checks = await Task.WhenAll(
                typed.Select(async d =>
                {
                    bool ok = await IsDriveReadyWithTimeoutAsync(d).ConfigureAwait(false);
                    return (drive: d, ok);
                })).ConfigureAwait(false);

            return checks.Where(x => x.ok).Select(x => x.drive).ToList();
        }

        private async Task ScanDrivesAsync()
        {
            try
            {
                _sourceItemsCache.Clear();

                List<DriveInfo> candidateDrives = await EnumerateCandidateDrivesAsync().ConfigureAwait(false);

                (DriveInfo drive, string deviceId)[] resolved = await Task.WhenAll(
                    candidateDrives.Select(async d =>
                    {
                        string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                        return (drive: d, deviceId: id);
                    })).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var pair in resolved)
                    {
                        _driveDeviceIdByPath[pair.drive.Name] = pair.deviceId;
                        _drivePathByDeviceId[pair.deviceId] = pair.drive.Name;
                    }

                    var activeDrives = resolved
                        .Where(pair =>
                        {
                            bool includeByDefault = _selectedDriveDeviceIds.Count == 0 && pair.drive.DriveType == DriveType.Removable;
                            return includeByDefault ||
                                   _selectedDriveDeviceIds.Contains(pair.deviceId) ||
                                   _selectedDriveDeviceIds.Contains($"path:{pair.drive.Name.ToUpperInvariant()}");
                        })
                        .Select(pair => pair.drive.Name)
                        .ToList();

                    for (int i = Sources.Count - 1; i >= 0; i--)
                    {
                        if (Sources[i] is string s)
                        {
                            if (s.Contains(':') && !activeDrives.Contains(s))
                            {
                                Sources.RemoveAt(i);
                                if (SelectedSource as string == s) SelectedSource = null;
                            }
                        }
                    }

                    foreach (string drive in activeDrives)
                    {
                        if (!Sources.Contains(drive))
                        {
                            Sources.Add(drive);
                        }
                    }

                    if (!Sources.Contains(_unifiedSource))
                    {
                        Sources.Insert(0, _unifiedSource);
                    }
                });
            }
            catch
            {
                // Keep UI/config load stable on drive enumeration errors.
            }
        }

        private static string NormalizeFtpPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";

            string normalized = path.Trim().Replace("\\", "/");
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized;
        }
    }
}
