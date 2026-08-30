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
using QuickMediaIngest.Services;
using QuickMediaIngest;


namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {

        [RelayCommand]
        private void ExportImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = AppLocalizer.Get("Vm_ExportImportHistoryTitle"),
                        Filter = AppLocalizer.Get("Vm_ExportImportHistoryFilter"),
                        FileName = AppLocalizer.Get("Vm_ExportImportHistory_DefaultFileName")
                    };

                    bool? result = dlg.ShowDialog();
                    if (result == true && !string.IsNullOrEmpty(dlg.FileName))
                    {
                        string file = dlg.FileName;
                        if (file.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            // Export CSV with proper escaping and UTF8 encoding
                            var sb = new StringBuilder();
                            string EscapeCsv(string? s)
                            {
                                if (string.IsNullOrEmpty(s)) return string.Empty;
                                if (s.Contains('"')) s = s.Replace("\"", "\"\"");
                                if (s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r'))
                                    return $"\"{s}\"";
                                return s;
                            }

                            sb.AppendLine(AppLocalizer.Get("Vm_ExportImportHistory_CsvHeader"));
                            foreach (var r in ImportHistoryRecords)
                            {
                                var fields = new[]
                                {
                                    EscapeCsv(r.StartedAtLocal.ToString("yyyy-MM-dd HH:mm:ss")),
                                    EscapeCsv(r.DurationSeconds.ToString()),
                                    EscapeCsv(r.FilesSelected.ToString()),
                                    EscapeCsv(r.FilesImported.ToString()),
                                    EscapeCsv(r.FailedFiles.ToString()),
                                    EscapeCsv(r.Source ?? string.Empty),
                                    EscapeCsv(r.Destination ?? string.Empty)
                                };
                                sb.AppendLine(string.Join(',', fields));
                            }

                            File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
                        }
                        else
                        {
                            // Default: JSON
                            string json = System.Text.Json.JsonSerializer.Serialize(ImportHistoryRecords.ToList(), new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(file, json);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Export import history failed.");
            }
        }
        public ObservableCollection<string> CommonFtpFolders { get; } = new ObservableCollection<string>
        {
            "/DCIM",
            "/DCIM/Camera",
            "/Pictures",
            "/Movies"
        };
        public ObservableCollection<FtpFolderOption> BrowsedFtpFolders { get; } = new ObservableCollection<FtpFolderOption>();
        public ObservableCollection<UpdateIntervalOption> IntervalOptions { get; } = new ObservableCollection<UpdateIntervalOption>();

        public ObservableCollection<string> PackageTypeOptions { get; } = new ObservableCollection<string>
        {
            "Portable",
            "Installer"
        };
        public ObservableCollection<string> ThumbnailPerformanceOptions { get; } = new ObservableCollection<string>
        {
            "Low",
            "Balanced",
            "Max",
            "Ultra"
        };
        public ObservableCollection<string> DuplicatePolicyOptions { get; } = new ObservableCollection<string>
        {
            "Suffix",
            "Skip",
            "OverwriteIfNewer"
        };
        public ObservableCollection<string> VerificationModeOptions { get; } = new ObservableCollection<string>
        {
            "Fast",
            "Strict"
        };

        [RelayCommand] private void Import() => ExecuteImport();
        [RelayCommand] private void QueueImport() => QueueCurrentImport();
        [RelayCommand] private void PreflightImport() => ExecuteImportPreflight();
        [RelayCommand] private void RetryFailedImports() => ExecuteRetryFailedImports();
        [RelayCommand] private void ResumePendingImport() => ExecuteResumePendingImport();
        [RelayCommand] private void SavePreset() => SaveCurrentPreset();
        [RelayCommand] private void LoadPreset() => LoadLatestPreset();
        [RelayCommand] private void DownloadUpdate() => ExecuteDownloadUpdate();
        [RelayCommand] private void ToggleAbout() => ShowAboutDialog = !ShowAboutDialog;

        [RelayCommand]
        private void OpenLogsFolder()
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuickMediaIngest",
                    "logs");
                _shellService.OpenFolder(logPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open logs folder.");
                MessageBox.Show(
                    AppLocalizer.Format("Msg_OpenLogsFailed_Body", ex.Message),
                    AppLocalizer.Get("Msg_Error_Title"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand] private void OpenChangelog() => _shellService.OpenUrl("https://github.com/edwardlthompson/QuickMediaIngest/blob/main/CHANGELOG.md");
        [RelayCommand]
        private void OpenGitHub()
        {
            const string repo = "https://github.com/edwardlthompson/QuickMediaIngest";
            try
            {
                if (!string.IsNullOrEmpty(AppVersion))
                {
                    // Prefer opening the release/tag that matches the running version
                    string tag = AppVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? AppVersion : "v" + AppVersion;
                    string releaseUrl = $"{repo}/releases/tag/{tag}";
                    _shellService.OpenUrl(releaseUrl);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not open release URL for version {Version}; falling back to repo.", AppVersion);
            }

            _shellService.OpenUrl(repo);
        }
    }
}
