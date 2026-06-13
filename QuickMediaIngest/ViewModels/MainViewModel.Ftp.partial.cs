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
            SaveConfig();
            RefreshImportReadinessSummary();
            OnPropertyChanged(nameof(IsDeleteAfterImportEnabled));
        }
        partial void OnDeleteAfterImportPromptDismissedChanged(bool value) => SaveConfig();

        public bool IsFtpBusy => IsTestingFtp || IsBrowsingFtpFolders;
        partial void OnIsBrowsingFtpFoldersChanged(bool value) => OnPropertyChanged(nameof(IsFtpBusy));

        partial void OnLimitFtpThumbnailLoadChanged(bool value) => SaveConfig();
        partial void OnFtpInitialThumbnailCountChanged(int value)
        {
            if (value < 20) FtpInitialThumbnailCount = 20;
            else if (value > 2000) FtpInitialThumbnailCount = 2000;
            SaveConfig();
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

        public double SavedWindowWidth => _savedWindowWidth;
        public double SavedWindowHeight => _savedWindowHeight;
        public bool SavedWindowMaximized => _savedWindowMaximized;
        public double? SavedWindowLeft => _savedWindowLeft;
        public double? SavedWindowTop => _savedWindowTop;

        public void SaveWindowState(double width, double height, bool maximized, double? left = null, double? top = null)
        {
            _savedWindowWidth = width;
            _savedWindowHeight = height;
            _savedWindowMaximized = maximized;
            if (!maximized && left.HasValue && top.HasValue)
            {
                _savedWindowLeft = left;
                _savedWindowTop = top;
            }
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
        partial void OnThumbnailSizeChanged(double value) => SaveConfig();
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

        public bool HasSelectedSource => SelectedSource != null;
        public bool IsLocalSourceSelected => SelectedSource is string;
        public bool IsFtpSourceSelected => SelectedSource is FtpSourceItem;
        public bool IsUnifiedSourceSelected => SelectedSource is UnifiedSourceItem;
        public string RawGroupingStatusText => GroupRawAndRenderedPairs ? "RAW/JPEG grouping: On" : "RAW/JPEG grouping: Off";
        public bool IsRawJpegGroupingEnabled => GroupRawAndRenderedPairs;
        public bool IsDeleteAfterImportEnabled => DeleteAfterImport;
        public bool IsKeywordEmbeddingEnabled => EmbedKeywordsOnImport;

        public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        public string BuildDate
        {
            get
            {
                try
                {
                    string? processPath = Environment.ProcessPath;
                    if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
                    {
                        return File.GetLastWriteTime(processPath).ToString("yyyy-MM-dd HH:mm");
                    }

                    // Avoid Assembly.Location: empty for single-file publishes (IL3000).
                    string baseDir = AppContext.BaseDirectory;
                    if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
                    {
                        string[] candidates = Directory.GetFiles(baseDir, "QuickMediaIngest*.exe");
                        string? newest = candidates
                            .OrderByDescending(File.GetLastWriteTimeUtc)
                            .FirstOrDefault();
                        if (!string.IsNullOrWhiteSpace(newest) && File.Exists(newest))
                        {
                            return File.GetLastWriteTime(newest).ToString("yyyy-MM-dd HH:mm");
                        }
                    }
                }
                catch
                {
                    // Ignore build date lookup errors.
                }

                return "Unknown";
            }
        }

        partial void OnIsDarkThemeChanged(bool value)
        {
            App.ApplyTheme(!value);
            SaveConfig();
        }

        public ObservableCollection<object> Sources { get; } = new ObservableCollection<object>();
        public ObservableCollection<DriveSelectionOption> DriveSelectionItems { get; } = new ObservableCollection<DriveSelectionOption>();
        public ObservableCollection<ExcludedDriveEntry> ExcludedDriveEntries { get; } = new ObservableCollection<ExcludedDriveEntry>();
        public ObservableCollection<SkippedFolderRuleEntry> SkippedFolderRuleEntries { get; } = new ObservableCollection<SkippedFolderRuleEntry>();
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();
        public ObservableCollection<ImportHistoryRecord> ImportHistoryRecords { get; } = new ObservableCollection<ImportHistoryRecord>();
        public ObservableCollection<FailedImportRecord> FailedImportRecords { get; } = new ObservableCollection<FailedImportRecord>();

        [RelayCommand]
        private void ClearImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => ImportHistoryRecords.Clear());
                string path = GetImportHistoryPath();
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Clear import history failed.");
            }
        }

        [RelayCommand]
        private void ConfirmClearImportHistory()
        {
            var result = MessageBox.Show(
                AppLocalizer.Get("Msg_ClearImportHistory_Body"),
                AppLocalizer.Get("Msg_ClearImportHistory_Title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                ClearImportHistoryCommand.Execute(null);
            }
        }
    }
}
