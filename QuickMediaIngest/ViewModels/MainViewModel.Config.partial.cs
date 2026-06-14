using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public void SaveConfig()
        {
            if (_loadingConfig)
            {
                return;
            }

            try
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(FtpHost))
                    {
                        string password = ResolveFtpPassword();
                        if (!string.IsNullOrEmpty(password))
                        {
                            _ftpCredentialStore.WritePassword(FtpHost, FtpPort, FtpUser, password);
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
                    LastSessionDestinationRoot = LastSessionDestinationRoot,
                    SavedFtpSources = BuildSavedFtpSourcesSnapshot()
                };

                PreserveFtpFieldsForSave(config, path);

                string json = System.Text.Json.JsonSerializer.Serialize(config);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Saving configuration failed.");
            }
        }

        private List<SavedFtpSourceEntry> BuildSavedFtpSourcesSnapshot()
        {
            return Sources
                .OfType<FtpSourceItem>()
                .Select(ftp => new SavedFtpSourceEntry
                {
                    Host = FtpHostNormalizer.Normalize(ftp.Host),
                    Port = ftp.Port,
                    User = ftp.User,
                    RemoteFolder = NormalizeFtpPath(ftp.RemoteFolder)
                })
                .GroupBy(s => $"{s.Host}|{s.Port}|{s.RemoteFolder}", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
        }

        private void PreserveFtpFieldsForSave(AppConfig config, string configPath)
        {
            if (string.IsNullOrWhiteSpace(config.FtpHost))
            {
                var sidebar = Sources.OfType<FtpSourceItem>().FirstOrDefault();
                if (sidebar != null)
                {
                    config.FtpHost = FtpHostNormalizer.Normalize(sidebar.Host);
                    config.FtpPort = sidebar.Port;
                    config.FtpUser = sidebar.User;
                    config.FtpRemoteFolder = NormalizeFtpPath(sidebar.RemoteFolder);
                }
            }

            if (!string.IsNullOrWhiteSpace(config.FtpHost) &&
                (config.SavedFtpSources == null || config.SavedFtpSources.Count == 0))
            {
                config.SavedFtpSources = new List<SavedFtpSourceEntry>
                {
                    new()
                    {
                        Host = config.FtpHost,
                        Port = config.FtpPort,
                        User = config.FtpUser,
                        RemoteFolder = NormalizeFtpPath(config.FtpRemoteFolder)
                    }
                };
            }

            if (!File.Exists(configPath))
            {
                return;
            }

            try
            {
                var existing = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                if (existing == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(config.FtpHost) && !string.IsNullOrWhiteSpace(existing.FtpHost))
                {
                    config.FtpHost = FtpHostNormalizer.Normalize(existing.FtpHost);
                    config.FtpPort = existing.FtpPort > 0 ? existing.FtpPort : 21;
                    config.FtpUser = existing.FtpUser ?? string.Empty;
                    config.FtpRemoteFolder = NormalizeFtpPath(existing.FtpRemoteFolder);
                }

                if ((config.SavedFtpSources == null || config.SavedFtpSources.Count == 0) &&
                    existing.SavedFtpSources is { Count: > 0 })
                {
                    config.SavedFtpSources = existing.SavedFtpSources;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not merge existing FTP settings during save.");
            }
        }

        private void RestoreFtpSourcesFromConfig(IReadOnlyList<SavedFtpSourceEntry>? savedEntries)
        {
            var entries = new List<SavedFtpSourceEntry>();
            if (savedEntries is { Count: > 0 })
            {
                entries.AddRange(savedEntries);
            }
            else if (!string.IsNullOrWhiteSpace(FtpHost))
            {
                entries.Add(new SavedFtpSourceEntry
                {
                    Host = FtpHost,
                    Port = FtpPort,
                    User = FtpUser,
                    RemoteFolder = NormalizeFtpPath(FtpRemoteFolder)
                });
            }

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Host))
                {
                    continue;
                }

                string host = FtpHostNormalizer.Normalize(entry.Host);
                string remoteFolder = NormalizeFtpPath(entry.RemoteFolder);
                bool exists = Sources.OfType<FtpSourceItem>().Any(s =>
                    string.Equals(s.Host, host, StringComparison.OrdinalIgnoreCase) &&
                    s.Port == entry.Port &&
                    string.Equals(NormalizeFtpPath(s.RemoteFolder), remoteFolder, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    continue;
                }

                string password = FtpSourceCredentials.ResolvePassword(
                    string.Empty,
                    host,
                    entry.Port,
                    entry.Host,
                    _ftpCredentialStore);

                Sources.Add(new FtpSourceItem
                {
                    Host = host,
                    Port = entry.Port > 0 ? entry.Port : 21,
                    User = entry.User,
                    Pass = password,
                    RemoteFolder = remoteFolder
                });
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                var first = entries.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Host));
                if (first != null)
                {
                    FtpHost = FtpHostNormalizer.Normalize(first.Host);
                    FtpPort = first.Port > 0 ? first.Port : 21;
                    FtpUser = first.User;
                    FtpRemoteFolder = NormalizeFtpPath(first.RemoteFolder);
                    FtpPass = FtpSourceCredentials.ResolvePassword(FtpPass, FtpHost, FtpPort, FtpHost, _ftpCredentialStore);
                }
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
                            // Legacy FTP thumbnail batch limits — no UI; always load all previews on startup.
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
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Loading configuration failed.");
            }
        }

        [RelayCommand]
        private void SaveAndCloseSettings()
        {
            SaveConfig();
            ShowSettingsDialog = false;
        }

        [RelayCommand]
        private void CloseSettingsOverlay() => ShowSettingsDialog = false;

        [RelayCommand]
        private void BrowseDestination()
        {
            try
            {
                string initial = DestinationRoot;
                if (string.IsNullOrWhiteSpace(initial) || !Directory.Exists(initial))
                {
                    initial = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                }

                try
                {
                    if (_fileDialogService.TryPickFolder(
                            initial,
                            AppLocalizer.Get("Dlg_SelectDestinationFolder"),
                            out string selected))
                    {
                        DestinationPreset = "Custom";
                        DestinationRoot = selected;
                    }
                }
                catch
                {
                    _shellService.OpenFolder(initial);
                    MessageBox.Show(
                        AppLocalizer.Get("Msg_FolderPickerExplorerFallback"),
                        AppLocalizer.Get("Msg_SelectFolder_Title"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Browse destination folder failed.");
            }
        }
    }
}
