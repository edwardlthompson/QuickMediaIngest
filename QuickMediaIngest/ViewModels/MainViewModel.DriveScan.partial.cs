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

        public void ExportImportReportArtifact(TimeSpan duration, List<ItemGroup> selectedGroups)
        {
            try
            {
                string reportDir = Path.Combine(DestinationRoot, "_ImportReports");
                Directory.CreateDirectory(reportDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                string rawSource = SelectedSource?.ToString() ?? "Unknown";
                string sanitizedSource = QuickMediaIngest.Core.PrivacyReport.PrivacyReportSanitize.SanitizeReportText(rawSource);
                string sanitizedDestination = QuickMediaIngest.Core.PrivacyReport.PrivacyReportSanitize.SanitizeReportText(DestinationRoot);

                var report = new ImportReportArtifact
                {
                    GeneratedAt = DateTime.Now,
                    Source = sanitizedSource,
                    Destination = sanitizedDestination,
                    DurationSeconds = duration.TotalSeconds,
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    VerificationMode = VerificationMode,
                    DuplicatePolicy = DuplicatePolicy,
                    ItemRatings = selectedGroups.SelectMany(g => g.Items)
                        .Where(i => i.Rating > 0 || !string.IsNullOrEmpty(i.ColorLabel))
                        .Select(i => new ImportItemRatingArtifact
                        {
                            FileName = QuickMediaIngest.Core.PrivacyReport.PrivacyReportSanitize.SanitizeReportText(i.FileName),
                            Rating = i.Rating,
                            ColorLabel = i.ColorLabel
                        }).ToList(),
                    Failed = FailedImportRecords.Select(f => new FailedImportRecord
                    {
                        FileName = QuickMediaIngest.Core.PrivacyReport.PrivacyReportSanitize.SanitizeReportText(f.FileName),
                        ErrorMessage = QuickMediaIngest.Core.PrivacyReport.PrivacyReportSanitize.SanitizeReportText(f.ErrorMessage)
                    }).ToList()
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
                if (report.Failed.Count > 0)
                {
                    text.AppendLine("Failed Files:");
                    foreach (var failure in report.Failed)
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
