using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
        public string ExportSettingsJson()
        {
            var config = new AppConfig
            {
                SchemaVersion = 1,
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
                FtpPass = string.Empty, // Passwords never exported in JSON per PRIVACY.md
                FtpRemoteFolder = FtpRemoteFolder,
                AutoReconnectLastFtp = AutoReconnectLastFtp,
                PreferAdbTransferWhenAvailable = PreferAdbTransferWhenAvailable,
                SettingsMenuExpanded = SettingsMenuExpanded,
                ScanPath = ScanPath,
                SelectAll = SelectAll,
                IsDarkTheme = IsDarkTheme,
                SaveCrashDetails = SaveCrashDetails,
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
                StripGpsAndPiiOnEmbed = StripGpsAndPiiOnEmbed,
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
                DestinationPreset = DestinationPreset,
                LastSessionDestinationRoot = LastSessionDestinationRoot,
                SavedFtpSources = BuildSavedFtpSourcesSnapshot()
            };

            return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        }

        public bool ImportSettingsJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config == null) return false;

                if (config.SchemaVersion <= 0)
                {
                    config.SchemaVersion = 1;
                }

                ApplyConfigToViewModel(config);
                SaveConfig();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import settings from JSON.");
                return false;
            }
        }
    }
}
