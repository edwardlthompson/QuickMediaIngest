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

        partial void OnOpenDestinationFolderWhenImportCompletesChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnShowCompactImportSummaryModalChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnConfirmCancelImportRequestChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnImportCooldownBetweenFilesMsChanged(int value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnImportSingleThreadedChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnLastSessionDestinationRootChanged(string value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }
        partial void OnDeleteAfterImportChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
            RefreshImportReadinessSummary();
            OnPropertyChanged(nameof(IsDeleteAfterImportEnabled));
        }
        partial void OnDeleteAfterImportPromptDismissedChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        public bool IsFtpBusy => IsTestingFtp || IsBrowsingFtpFolders;
        partial void OnIsBrowsingFtpFoldersChanged(bool value) => OnPropertyChanged(nameof(IsFtpBusy));

        partial void OnLimitFtpThumbnailLoadChanged(bool value)
        {
            if (!_loadingConfig)
            {
                SaveConfig();
            }
        }

        partial void OnFtpInitialThumbnailCountChanged(int value)
        {
            if (_loadingConfig)
            {
                return;
            }

            if (value < 20) FtpInitialThumbnailCount = 20;
            else if (value > 2000) FtpInitialThumbnailCount = 2000;
            SaveConfig();
        }

        partial void OnFtpHostChanged(string value)
        {
            if (_loadingConfig || string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (!value.Contains("ftp://", StringComparison.OrdinalIgnoreCase) &&
                !value.Contains("ftps://", StringComparison.OrdinalIgnoreCase) &&
                !value.Contains(':', StringComparison.Ordinal))
            {
                return;
            }

            if (!FtpHostNormalizer.TryParseHostAndPort(value, out string normalized, out int? port))
            {
                return;
            }

            if (port.HasValue && FtpPort != port.Value)
            {
                FtpPort = port.Value;
            }

            if (!string.Equals(FtpHost, normalized, StringComparison.Ordinal))
            {
                FtpHost = normalized;
            }
        }

        private List<string> _ribbonTileOrder = new();
        public List<string> RibbonTileOrder
        {
            get => _ribbonTileOrder;
            set { _ribbonTileOrder = value; OnPropertyChanged(); }
        }

        public void SaveTileOrder(IEnumerable<string> order)
        {
            _ribbonTileOrder = order.ToList();
            SaveConfig();
        }


        [RelayCommand]
        private async Task ToggleAddFtp()
        {
            bool newState = !ShowAddFtpDialog;
            _logger.LogDebug("ToggleAddFtp requested. NewState={NewState}", newState);
            ShowAddFtpDialog = newState;
            await Task.Yield();
            _logger.LogDebug("ToggleAddFtp completed. ShowAddFtpDialog={ShowAddFtpDialog}", ShowAddFtpDialog);
        }
        [RelayCommand] private void SaveFtp() => ExecuteSaveFtp();
        [RelayCommand] private void TestFtpConnection() => ExecuteTestFtpConnection();
        [RelayCommand] private void BrowseFtpFolders() => ExecuteBrowseFtpFolders();
        [RelayCommand] private void UseBrowsedFtpFolder() => ExecuteUseBrowsedFtpFolder();
        [RelayCommand] private void CopySkippedFoldersReport() => ExecuteCopySkippedFoldersReport();
        [RelayCommand] private void CloseSkippedFoldersReport() => ExecuteCloseSkippedFoldersReport();

        partial void OnUpdateIntervalHoursChanged(int value) { SaveConfig(); CheckUpdates(); }
        partial void OnUpdatePackageTypeChanged(string value) => SaveConfig();
        partial void OnNamingTemplateChanged(string value)
        {
            if (_updatingNamingFromUi)
            {
                SaveConfig();
                return;
            }

            SyncNamingOptionsFromTemplate();
            RefreshNamingPreviewExamples();
            SaveConfig();
        }
        partial void OnNamingPresetChanged(string value)
        {
            ApplyNamingPreset(value);
            SaveConfig();
        }
        partial void OnNamingIncludeDateChanged(bool value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingIncludeTimeChanged(bool value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingIncludeSequenceChanged(bool value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingIncludeShootNameChanged(bool value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingIncludeOriginalNameChanged(bool value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingDateFormatChanged(string value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingTimeFormatChanged(string value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingSeparatorChanged(string value) => UpdateNamingTemplateFromOptions();
        partial void OnNamingShootNameSampleChanged(string value)
        {
            RefreshNamingPreviewExamples();
            SaveConfig();
        }
        partial void OnNamingLowercaseChanged(bool value)
        {
            RefreshNamingPreviewExamples();
            SaveConfig();
        }
        partial void OnSettingsMenuExpandedChanged(bool value) => SaveConfig();
        partial void OnThumbnailPerformanceModeChanged(string value) => SaveConfig();
        partial void OnThumbnailSizeChanged(double value)
        {
            double clamped = Math.Clamp(value, 50, 300);
            if (Math.Abs(clamped - value) > 0.01)
            {
                ThumbnailSize = clamped;
                return;
            }

            SaveConfig();
        }
        partial void OnExpandPreviewStacksChanged(bool value)
        {
            RebuildGroupsFromCurrentItems();
            SaveConfig();
        }
        partial void OnGroupRawAndRenderedPairsChanged(bool value)
        {
            RebuildGroupsFromCurrentItems();
            OnPropertyChanged(nameof(RawGroupingStatusText));
            OnPropertyChanged(nameof(IsRawJpegGroupingEnabled));
            SaveConfig();
        }
        partial void OnDuplicatePolicyChanged(string value)
        {
            SaveConfig();
            RefreshImportReadinessSummary();
        }
        partial void OnVerificationModeChanged(string value)
        {
            SaveConfig();
            RefreshImportReadinessSummary();
        }
        partial void OnUiLanguageChanged(string value) => SaveConfig();
        partial void OnShowSettingsDialogChanged(bool value)
        {
            if (value)
            {
                // Device-id I/O must not run on the UI thread (preferences used to freeze here).
                _ = RefreshExclusionManagementListsAsync();
            }
        }

        partial void OnShowScanExclusionsPanelChanged(bool value)
        {
            if (value)
            {
                _ = RefreshExclusionManagementListsAsync();
            }
        }
        partial void OnEmbedKeywordsOnImportChanged(bool value)
        {
            SaveConfig();
            RefreshImportReadinessSummary();
            OnPropertyChanged(nameof(IsKeywordEmbeddingEnabled));
        }
        partial void OnAllGroupsExpandedChanged(bool value)
        {
            if (_isBulkUpdatingGroupExpansion)
            {
                return;
            }

            _isBulkUpdatingGroupExpansion = true;
            try
            {
                foreach (var group in Groups)
                {
                    group.IsExpanded = value;
                }
            }
            finally
            {
                _isBulkUpdatingGroupExpansion = false;
            }
        }
        partial void OnStatusMessageChanged(string value)
        {
            if (!_suppressStatusFeedFromStatusMessage)
            {
                AddNotificationFeedEntry(value);
            }
        }
        partial void OnScanProgressMessageChanged(string value) => AddNotificationFeedEntry(value);
        partial void OnConfirmBeforeImportChanged(bool value) => SaveConfig();
        partial void OnSuppressExcludedFolderScanRemindersChanged(bool value) => SaveConfig();
        partial void OnSidebarCollapsedChanged(bool value) => SaveConfig();
        partial void OnSidebarNotificationsExpandedChanged(bool value) => SaveConfig();
        partial void OnSettingsPrefsDestinationExpandedChanged(bool value) => SaveConfig();
        partial void OnSettingsPrefsNamingExpandedChanged(bool value) => SaveConfig();
        partial void OnSettingsPrefsLanguageExpandedChanged(bool value) => SaveConfig();
        partial void OnSettingsPrefsImportSettingsExpandedChanged(bool value) => SaveConfig();
        partial void OnScanPathChanged(string value)
        {
            if (SelectedSource is FtpSourceItem ftp)
                ftp.RemoteFolder = NormalizeFtpPath(value);
            _sourceItemsCache.Clear();
            SaveConfig();
        }
        partial void OnScanIncludeSubfoldersChanged(bool value)
        {
            _sourceItemsCache.Clear();
            SaveConfig();
        }

        partial void OnIsDarkThemeChanged(bool value)
        {
            App.ApplyTheme(!value);
            SaveConfig();
        }
    }
}
