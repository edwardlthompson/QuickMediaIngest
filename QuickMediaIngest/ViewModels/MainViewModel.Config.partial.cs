using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public void SaveConfig()
        {
            try
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(FtpHost))
                    {
                        if (!string.IsNullOrEmpty(FtpPass))
                        {
                            _ftpCredentialStore.WritePassword(FtpHost, FtpPort, FtpUser, FtpPass);
                        }
                        else
                        {
                            _ftpCredentialStore.DeletePassword(FtpHost, FtpPort);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "FTP password could not be stored in Windows Credential Manager.");
                }

                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "config.json");

                var config = new AppConfig
                {
                    UpdateIntervalHours = UpdateIntervalHours,
                    DestinationRoot = DestinationRoot,
                    DeleteAfterImport = DeleteAfterImport,
                    DeleteAfterImportPromptDismissed = DeleteAfterImportPromptDismissed,
                    NamingTemplate = NamingTemplate,
                    NamingPreset = NamingPreset,
                    NamingDateFormat = NamingDateFormat,
                    NamingTimeFormat = NamingTimeFormat,
                    NamingSeparator = NamingSeparator,
                    NamingIncludeSequence = NamingIncludeSequence,
                    NamingShootNameSample = NamingShootNameSample,
                    NamingLowercase = NamingLowercase,
                    ThumbnailPerformanceMode = ThumbnailPerformanceMode,
                    GroupRawAndRenderedPairs = GroupRawAndRenderedPairs,
                    FtpHost = FtpHost,
                    FtpPort = FtpPort,
                    FtpUser = FtpUser,
                    FtpPass = string.Empty,
                    FtpRemoteFolder = FtpRemoteFolder,
                    AutoReconnectLastFtp = AutoReconnectLastFtp,
                    SettingsMenuExpanded = SettingsMenuExpanded,
                    ScanPath = ScanPath,
                    SelectAll = SelectAll,
                    IsDarkTheme = IsDarkTheme,
                    ThumbnailSize = ThumbnailSize,
                    ScanIncludeSubfolders = ScanIncludeSubfolders,
                    TimeBetweenShootsHours = TimeBetweenShootsHours,
                    LimitFtpThumbnailLoad = LimitFtpThumbnailLoad,
                    FtpInitialThumbnailCount = FtpInitialThumbnailCount,
                    ExpandPreviewStacks = ExpandPreviewStacks,
                    DuplicatePolicy = DuplicatePolicy,
                    VerificationMode = VerificationMode,
                    UiLanguage = UiLanguage,
                    EmbedKeywordsOnImport = EmbedKeywordsOnImport,
                    ConfirmBeforeImport = ConfirmBeforeImport,
                    SuppressExcludedFolderScanReminders = SuppressExcludedFolderScanReminders,
                    SidebarCollapsed = SidebarCollapsed,
                    SidebarNotificationsExpanded = SidebarNotificationsExpanded,
                    SettingsPrefsDestinationExpanded = SettingsPrefsDestinationExpanded,
                    SettingsPrefsNamingExpanded = SettingsPrefsNamingExpanded,
                    SettingsPrefsLanguageExpanded = SettingsPrefsLanguageExpanded,
                    SettingsPrefsImportSettingsExpanded = SettingsPrefsImportSettingsExpanded,
                    RibbonTileOrder = _ribbonTileOrder.Count > 0 ? _ribbonTileOrder : null,
                    UpdatePackageType = UpdatePackageType,
                    WindowWidth = _savedWindowWidth,
                    WindowHeight = _savedWindowHeight,
                    WindowMaximized = _savedWindowMaximized,
                    WindowLeft = _savedWindowLeft,
                    WindowTop = _savedWindowTop,
                    IsFirstRun = this.IsFirstRun,
                    SelectedDriveDeviceIds = _selectedDriveDeviceIds.ToList(),
                    SkippedFoldersBySource = _skippedFoldersBySource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList(), StringComparer.OrdinalIgnoreCase),
                    OpenDestinationFolderWhenImportCompletes = OpenDestinationFolderWhenImportCompletes,
                    ShowCompactImportSummaryModal = ShowCompactImportSummaryModal,
                    ConfirmCancelImportRequest = ConfirmCancelImportRequest,
                    ImportCooldownBetweenFilesMs = ImportCooldownBetweenFilesMs,
                    ImportSingleThreaded = ImportSingleThreaded,
                    LastNotifiedUpdateTag = LastNotifiedUpdateTag,
                    DestinationPreset = DestinationPreset,
                    LastSessionDestinationRoot = LastSessionDestinationRoot
                };

                string json = System.Text.Json.JsonSerializer.Serialize(config);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Saving configuration failed.");
            }
        }

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
                            if (!string.IsNullOrWhiteSpace(config.FtpHost)) FtpHost = config.FtpHost;
                            FtpPort = config.FtpPort > 0 ? config.FtpPort : 21;
                            if (!string.IsNullOrWhiteSpace(config.FtpUser)) FtpUser = config.FtpUser;
                            if (_ftpCredentialStore.TryReadPassword(FtpHost, FtpPort, out string vaultPassword) &&
                                !string.IsNullOrEmpty(vaultPassword))
                            {
                                FtpPass = vaultPassword;
                            }
                            else if (!string.IsNullOrEmpty(config.FtpPass))
                            {
                                FtpPass = config.FtpPass;
                                try
                                {
                                    _ftpCredentialStore.WritePassword(FtpHost, FtpPort, FtpUser, config.FtpPass);
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

                            // Parse NamingTemplate to SelectedTokens
                            Application.Current.Dispatcher.Invoke(() => {
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
                        }
                        finally
                        {
                            _loadingConfig = false;
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
