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
