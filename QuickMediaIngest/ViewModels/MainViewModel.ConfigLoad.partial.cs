using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        private void LoadConfig()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = System.Text.Json.JsonSerializer.Deserialize<QuickMediaIngest.AppConfig>(json);
                    if (config != null)
                    {
                        bool purgePlaintextFtpPass = false;
                        _loadingConfig = true;
                        try
                        {
                            UpdateIntervalHours = config.UpdateIntervalHours;
                            if (!string.IsNullOrEmpty(config.DestinationRoot)) DestinationRoot = config.DestinationRoot;
                            DeleteAfterImport = config.DeleteAfterImport;
                            DeleteAfterImportPromptDismissed = config.DeleteAfterImportPromptDismissed;
                            if (!string.IsNullOrEmpty(config.NamingTemplate)) NamingTemplate = config.NamingTemplate;
                            if (!string.IsNullOrWhiteSpace(config.NamingPreset)) NamingPreset = config.NamingPreset;
                            if (!string.IsNullOrWhiteSpace(config.NamingDateFormat)) NamingDateFormat = config.NamingDateFormat;
                            if (!string.IsNullOrWhiteSpace(config.NamingTimeFormat)) NamingTimeFormat = config.NamingTimeFormat;
                            if (!string.IsNullOrWhiteSpace(config.NamingSeparator)) NamingSeparator = config.NamingSeparator;
                            NamingIncludeSequence = config.NamingIncludeSequence;
                            if (!string.IsNullOrWhiteSpace(config.NamingShootNameSample)) NamingShootNameSample = config.NamingShootNameSample;
                            NamingLowercase = config.NamingLowercase;
                            if (!string.IsNullOrWhiteSpace(config.ThumbnailPerformanceMode)) ThumbnailPerformanceMode = config.ThumbnailPerformanceMode;
                            GroupRawAndRenderedPairs = config.GroupRawAndRenderedPairs;
                            string legacyFtpHost = config.FtpHost ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(legacyFtpHost))
                            {
                                FtpHost = FtpHostNormalizer.Normalize(legacyFtpHost);
                            }

                            FtpPort = config.FtpPort > 0 ? config.FtpPort : 21;
                            if (!string.IsNullOrWhiteSpace(config.FtpUser)) FtpUser = config.FtpUser;

                            if (_ftpCredentialStore.TryReadPasswordWithLegacyKeys(FtpHost, FtpPort, legacyFtpHost, out string vaultPassword) &&
                                !string.IsNullOrEmpty(vaultPassword))
                            {
                                FtpPass = vaultPassword;
                                try
                                {
                                    _ftpCredentialStore.WritePassword(FtpHost, FtpPort, FtpUser, vaultPassword);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not migrate FTP credential key to normalized host.");
                                }

                                // Vault already holds the secret — strip any leftover plaintext from disk.
                                if (!string.IsNullOrEmpty(config.FtpPass))
                                {
                                    purgePlaintextFtpPass = true;
                                }
                            }
                            else if (!string.IsNullOrEmpty(config.FtpPass))
                            {
                                FtpPass = config.FtpPass;
                                try
                                {
                                    _ftpCredentialStore.WritePassword(FtpHost, FtpPort, FtpUser, config.FtpPass);
                                    purgePlaintextFtpPass = true;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not migrate legacy FTP password to Credential Manager.");
                                }
                            }
                            else
                            {
                                FtpPass = string.Empty;
                            }

                            if (!string.IsNullOrWhiteSpace(config.FtpRemoteFolder)) FtpRemoteFolder = NormalizeFtpPath(config.FtpRemoteFolder);
                            AutoReconnectLastFtp = config.AutoReconnectLastFtp;
                            SettingsMenuExpanded = config.SettingsMenuExpanded;
                            if (!string.IsNullOrWhiteSpace(config.ScanPath)) ScanPath = config.ScanPath;
                            SelectAll = config.SelectAll;
                            if (config.IsDarkTheme.HasValue)
                            {
                                IsDarkTheme = config.IsDarkTheme.Value;
                                App.ApplyTheme(!IsDarkTheme);
                            }
                            if (config.ThumbnailSize > 0) ThumbnailSize = config.ThumbnailSize;
                            ScanIncludeSubfolders = config.ScanIncludeSubfolders;
                            TimeBetweenShootsHours = Math.Clamp(config.TimeBetweenShootsHours <= 0 ? 4 : config.TimeBetweenShootsHours, 1, 24);
                            LimitFtpThumbnailLoad = false;
                            FtpInitialThumbnailCount = 0;
                            ExpandPreviewStacks = config.ExpandPreviewStacks;
                            if (!string.IsNullOrWhiteSpace(config.DuplicatePolicy)) DuplicatePolicy = config.DuplicatePolicy;
                            if (!string.IsNullOrWhiteSpace(config.VerificationMode)) VerificationMode = config.VerificationMode;
                            UiLanguage = config.UiLanguage ?? string.Empty;
                            EmbedKeywordsOnImport = config.EmbedKeywordsOnImport;
                            ConfirmBeforeImport = config.ConfirmBeforeImport;
                            OpenDestinationFolderWhenImportCompletes = config.OpenDestinationFolderWhenImportCompletes ?? true;
                            ShowCompactImportSummaryModal = config.ShowCompactImportSummaryModal ?? true;
                            ConfirmCancelImportRequest = config.ConfirmCancelImportRequest ?? true;
                            ImportCooldownBetweenFilesMs = Math.Max(0, config.ImportCooldownBetweenFilesMs);
                            ImportSingleThreaded = config.ImportSingleThreaded;
                            LastNotifiedUpdateTag = config.LastNotifiedUpdateTag ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(config.DestinationPreset)) DestinationPreset = config.DestinationPreset;
                            if (!string.IsNullOrWhiteSpace(config.LastSessionDestinationRoot)) LastSessionDestinationRoot = config.LastSessionDestinationRoot;
                            SuppressExcludedFolderScanReminders = config.SuppressExcludedFolderScanReminders;
                            SidebarCollapsed = config.SidebarCollapsed;
                            SidebarNotificationsExpanded = config.SidebarNotificationsExpanded ?? true;
                            SettingsPrefsDestinationExpanded = config.SettingsPrefsDestinationExpanded ?? true;
                            SettingsPrefsNamingExpanded = config.SettingsPrefsNamingExpanded ?? true;
                            SettingsPrefsLanguageExpanded = config.SettingsPrefsLanguageExpanded ?? true;
                            SettingsPrefsImportSettingsExpanded = config.SettingsPrefsImportSettingsExpanded
                                ?? ((config.SettingsPrefsImportBehaviorExpanded ?? true) || config.SettingsAdvancedExpanded);
                            if (config.RibbonTileOrder is { Count: > 0 })
                                _ribbonTileOrder = config.RibbonTileOrder;
                            if (!string.IsNullOrEmpty(config.UpdatePackageType)) UpdatePackageType = config.UpdatePackageType;
                            if (config.WindowWidth >= 400) _savedWindowWidth = config.WindowWidth;
                            if (config.WindowHeight >= 300) _savedWindowHeight = config.WindowHeight;
                            _savedWindowMaximized = config.WindowMaximized;
                            if (config.WindowLeft.HasValue && config.WindowTop.HasValue &&
                                !double.IsNaN(config.WindowLeft.Value) && !double.IsNaN(config.WindowTop.Value) &&
                                !double.IsInfinity(config.WindowLeft.Value) && !double.IsInfinity(config.WindowTop.Value))
                            {
                                _savedWindowLeft = config.WindowLeft;
                                _savedWindowTop = config.WindowTop;
                            }

                            OnPropertyChanged("UpdateIntervalHours");
                            OnPropertyChanged("UpdatePackageType");
                            OnPropertyChanged("DestinationRoot");
                            OnPropertyChanged("DeleteAfterImport");
                            OnPropertyChanged(nameof(DeleteAfterImportPromptDismissed));
                            OnPropertyChanged(nameof(UiLanguage));
                            OnPropertyChanged(nameof(EmbedKeywordsOnImport));
                            OnPropertyChanged(nameof(ConfirmBeforeImport));
                            OnPropertyChanged(nameof(SuppressExcludedFolderScanReminders));
                            OnPropertyChanged(nameof(SidebarCollapsed));
                            OnPropertyChanged(nameof(SidebarNotificationsExpanded));
                            OnPropertyChanged(nameof(SettingsPrefsDestinationExpanded));
                            OnPropertyChanged(nameof(SettingsPrefsNamingExpanded));
                            OnPropertyChanged(nameof(SettingsPrefsLanguageExpanded));
                            OnPropertyChanged(nameof(SettingsPrefsImportSettingsExpanded));
                            OnPropertyChanged("NamingTemplate");
                            OnPropertyChanged("ScanPath");
                            OnPropertyChanged("SelectAll");
                            OnPropertyChanged("IsDarkTheme");
                            OnPropertyChanged("ThumbnailSize");
                            OnPropertyChanged("ScanIncludeSubfolders");
                            OnPropertyChanged("TimeBetweenShootsHours");
                            OnPropertyChanged("LimitFtpThumbnailLoad");
                            OnPropertyChanged("FtpInitialThumbnailCount");
                            this.IsFirstRun = config.IsFirstRun;
                            _selectedDriveDeviceIds.Clear();
                            foreach (var id in config.SelectedDriveDeviceIds ?? Enumerable.Empty<string>())
                            {
                                if (!string.IsNullOrWhiteSpace(id))
                                {
                                    _selectedDriveDeviceIds.Add(id);
                                }
                            }
                            if (_selectedDriveDeviceIds.Count == 0)
                            {
                                foreach (var oldPath in config.SelectedDrivePaths ?? Enumerable.Empty<string>())
                                {
                                    if (!string.IsNullOrWhiteSpace(oldPath))
                                    {
                                        _selectedDriveDeviceIds.Add($"path:{oldPath.ToUpperInvariant()}");
                                    }
                                }
                            }

                            _skippedFoldersBySource.Clear();
                            foreach (var kvp in config.SkippedFoldersBySource ?? new Dictionary<string, List<string>>())
                            {
                                if (string.IsNullOrWhiteSpace(kvp.Key))
                                {
                                    continue;
                                }

                                _skippedFoldersBySource[kvp.Key] = kvp.Value?.Where(v => !string.IsNullOrWhiteSpace(v)).ToList() ?? new List<string>();
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                SelectedTokens.Clear();
                                if (!string.IsNullOrEmpty(NamingTemplate))
                                {
                                    var matches = System.Text.RegularExpressions.Regex.Matches(NamingTemplate, @"\[[^\]]+\]|[^\[\]]+");
                                    foreach (System.Text.RegularExpressions.Match m in matches)
                                    {
                                        if (!string.IsNullOrEmpty(m.Value))
                                        {
                                            SelectedTokens.Add(new TokenItem { Value = m.Value });
                                            if (m.Value.StartsWith("[") && m.Value.EndsWith("]"))
                                            {
                                                AvailableTokens.Remove(m.Value);
                                            }
                                        }
                                    }
                                }
                            });

                            RefreshDestinationPresetLabels();
                            RestoreFtpSourcesFromConfig(config.SavedFtpSources);
                        }
                        finally
                        {
                            _loadingConfig = false;
                        }

                        if (purgePlaintextFtpPass)
                        {
                            SaveConfig();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Loading configuration failed.");
            }
        }
    }
}
