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
        public ObservableCollection<string> NamingPresetOptions { get; } = new ObservableCollection<string>
        {
            "Recommended (Date + Shoot + Original)",
            "Date + Time + Shoot + Original",
            "Shoot + Date + Original",
            "Custom"
        };
        public ObservableCollection<string> NamingDateFormatOptions { get; } = new ObservableCollection<string>
        {
            "yyyy-MM-dd",
            "yyyyMMdd"
        };
        public ObservableCollection<string> NamingTimeFormatOptions { get; } = new ObservableCollection<string>
        {
            "HH-mm-ss",
            "HHmmss",
            "HH-mm-ss-fff",
            "HHmmssfff"
        };
        public ObservableCollection<string> NamingSeparatorOptions { get; } = new ObservableCollection<string>
        {
            "_",
            "-"
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
        public ObservableCollection<string> NamingPreviewExamples { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> AvailableTokens { get; } = new ObservableCollection<string>
        {
            "[Date]", "[Time]", "[TimeMs]", "[YYYY]", "[MM]", "[DD]", "[HH]", "[mm]", "[ss]", "[fff]", "[ShootName]", "[Original]", "[Sequence]", "[Ext]", "_", "-"
        };

        public ObservableCollection<TokenItem> SelectedTokens { get; } = new ObservableCollection<TokenItem>();

        public void UpdateNamingFromTokens()
        {
            NamingTemplate = string.Join("", SelectedTokens.Select(t => t.Value));
            OnPropertyChanged("NamingTemplate");
            SaveConfig();
        }

        private void ApplyNamingPreset(string preset)
        {
            _updatingNamingFromUi = true;
            switch (preset)
            {
                case "Recommended (Date + Shoot + Original)":
                    NamingIncludeDate = true;
                    NamingIncludeTime = false;
                    NamingIncludeSequence = false;
                    NamingIncludeShootName = true;
                    NamingIncludeOriginalName = true;
                    break;
                case "Date + Time + Shoot + Original":
                    NamingIncludeDate = true;
                    NamingIncludeTime = true;
                    NamingIncludeSequence = false;
                    NamingIncludeShootName = true;
                    NamingIncludeOriginalName = true;
                    break;
                case "Shoot + Date + Original":
                    NamingIncludeDate = true;
                    NamingIncludeTime = false;
                    NamingIncludeSequence = false;
                    NamingIncludeShootName = true;
                    NamingIncludeOriginalName = true;
                    break;
                default:
                    // Custom keeps user-selected options.
                    break;
            }
            _updatingNamingFromUi = false;
            UpdateNamingTemplateFromOptions();
        }

        private void UpdateNamingTemplateFromOptions()
        {
            if (_updatingNamingFromUi)
            {
                return;
            }

            var parts = new List<string>();
            if (NamingIncludeDate)
            {
                parts.Add(NamingDateFormat == "yyyyMMdd" ? "[YYYY][MM][DD]" : "[Date]");
            }
            if (NamingIncludeTime)
            {
                parts.Add(NamingTimeFormat switch
                {
                    "HHmmss" => "[HH][mm][ss]",
                    "HH-mm-ss-fff" => "[TimeMs]",
                    "HHmmssfff" => "[HH][mm][ss][fff]",
                    _ => "[Time]"
                });
            }
            if (NamingIncludeSequence)
            {
                parts.Add("[Sequence]");
            }
            if (NamingIncludeShootName)
            {
                parts.Add("[ShootName]");
            }
            if (NamingIncludeOriginalName)
            {
                parts.Add("[Original]");
            }

            if (parts.Count == 0)
            {
                parts.Add("[Original]");
            }

            _updatingNamingFromUi = true;
            NamingTemplate = string.Join(NamingSeparator, parts);
            _updatingNamingFromUi = false;

            RefreshNamingPreviewExamples();
            SaveConfig();
        }

        private void SyncNamingOptionsFromTemplate()
        {
            _updatingNamingFromUi = true;
            NamingIncludeDate = NamingTemplate.Contains("[Date]", StringComparison.Ordinal) ||
                               (NamingTemplate.Contains("[YYYY]", StringComparison.Ordinal) &&
                                NamingTemplate.Contains("[MM]", StringComparison.Ordinal) &&
                                NamingTemplate.Contains("[DD]", StringComparison.Ordinal));
            NamingDateFormat = NamingTemplate.Contains("[YYYY][MM][DD]", StringComparison.Ordinal) ? "yyyyMMdd" : "yyyy-MM-dd";
            NamingIncludeTime = NamingTemplate.Contains("[Time]", StringComparison.Ordinal) ||
                                NamingTemplate.Contains("[TimeMs]", StringComparison.Ordinal) ||
                                (NamingTemplate.Contains("[HH]", StringComparison.Ordinal) &&
                                 NamingTemplate.Contains("[mm]", StringComparison.Ordinal) &&
                                 NamingTemplate.Contains("[ss]", StringComparison.Ordinal));
            NamingTimeFormat = NamingTemplate.Contains("[HH][mm][ss][fff]", StringComparison.Ordinal) ? "HHmmssfff"
                : NamingTemplate.Contains("[TimeMs]", StringComparison.Ordinal) ? "HH-mm-ss-fff"
                : NamingTemplate.Contains("[HH][mm][ss]", StringComparison.Ordinal) ? "HHmmss"
                : "HH-mm-ss";
            NamingIncludeSequence = NamingTemplate.Contains("[Sequence]", StringComparison.Ordinal);
            NamingIncludeShootName = NamingTemplate.Contains("[ShootName]", StringComparison.Ordinal);
            NamingIncludeOriginalName = NamingTemplate.Contains("[Original]", StringComparison.Ordinal);
            NamingSeparator = NamingTemplate.Contains("-") && !NamingTemplate.Contains("_") ? "-" : "_";
            _updatingNamingFromUi = false;
        }

        private void RefreshNamingPreviewExamples()
        {
            try
            {
                string separator = string.IsNullOrWhiteSpace(NamingSeparator) ? "_" : NamingSeparator;
                string date = NamingDateFormat == "yyyyMMdd" ? "20260425" : "2026-04-25";
                string time = NamingTimeFormat switch
                {
                    "HHmmss" => "195649",
                    "HH-mm-ss-fff" => "19-56-49-123",
                    "HHmmssfff" => "195649123",
                    _ => "19-56-49"
                };
                string shoot = string.IsNullOrWhiteSpace(NamingShootNameSample) ? "my-shoot" : NamingShootNameSample.Trim();
                string[] originals = { "img_0001", "img_0002", "img_0003" };

                string template = NamingTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = "[Date]" + separator + "[ShootName]" + separator + "[Original]";
                }

                NamingPreviewExamples.Clear();
                for (int index = 0; index < originals.Length; index++)
                {
                    string original = originals[index];
                    string output = template
                        .Replace("[Date]", date, StringComparison.Ordinal)
                        .Replace("[Time]", time, StringComparison.Ordinal)
                        .Replace("[TimeMs]", "19-56-49-123", StringComparison.Ordinal)
                        .Replace("[YYYY]", "2026", StringComparison.Ordinal)
                        .Replace("[MM]", "04", StringComparison.Ordinal)
                        .Replace("[DD]", "25", StringComparison.Ordinal)
                        .Replace("[HH]", "19", StringComparison.Ordinal)
                        .Replace("[mm]", "56", StringComparison.Ordinal)
                        .Replace("[ss]", "49", StringComparison.Ordinal)
                        .Replace("[fff]", "123", StringComparison.Ordinal)
                        .Replace("[ShootName]", shoot, StringComparison.Ordinal)
                        .Replace("[Original]", original, StringComparison.Ordinal)
                        .Replace("[Sequence]", (index + 1).ToString("D4"), StringComparison.Ordinal)
                        .Replace("[Ext]", "jpg", StringComparison.Ordinal)
                        .Replace("__", "_", StringComparison.Ordinal)
                        .Replace("--", "-", StringComparison.Ordinal)
                        .Trim('_', '-');

                    if (NamingLowercase)
                    {
                        output = output.ToLowerInvariant();
                    }

                    NamingPreviewExamples.Add($"{output}.jpg");
                }
            }
            catch
            {
                // Keep UI stable if preview generation fails.
            }
        }

        [RelayCommand] private void Import() => ExecuteImport();
        [RelayCommand] private void QueueImport() => QueueCurrentImport();
        [RelayCommand] private void PreflightImport() => ExecuteImportPreflight();
        [RelayCommand] private void RetryFailedImports() => ExecuteRetryFailedImports();
        [RelayCommand] private void ResumePendingImport() => ExecuteResumePendingImport();
        [RelayCommand] private void SavePreset() => SaveCurrentPreset();
        [RelayCommand] private void LoadPreset() => LoadLatestPreset();
        [RelayCommand] private void DownloadUpdate() => ExecuteDownloadUpdate();
        [RelayCommand] private void ToggleAbout() => ShowAboutDialog = !ShowAboutDialog;
        [RelayCommand] private void OpenChangelog() => OpenUrl("https://github.com/edwardlthompson/QuickMediaIngest/blob/main/CHANGELOG.md");
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
                    OpenUrl(releaseUrl);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not open release URL for version {Version}; falling back to repo.", AppVersion);
            }

            OpenUrl(repo);
        }
    }
}
