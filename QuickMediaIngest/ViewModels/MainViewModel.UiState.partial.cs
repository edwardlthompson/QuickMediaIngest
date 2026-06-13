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
        [ObservableProperty] private bool settingsPrefsNamingExpanded = true;
        [ObservableProperty] private bool settingsPrefsLanguageExpanded = true;
        /// <summary>Unified Preferences expander for all import-related options.</summary>
        [ObservableProperty] private bool settingsPrefsImportSettingsExpanded = true;
        [ObservableProperty] private string importReadinessSummary = string.Empty;
        [ObservableProperty] private string lastImportSummary = string.Empty;
        [ObservableProperty] private string previewHealthSummary = string.Empty;
        [ObservableProperty] private string ftpThumbnailPhaseDetail = string.Empty;
        [ObservableProperty] private bool allGroupsExpanded = false;
        [ObservableProperty] private string selectedFilesStatusLine = string.Empty;
        [ObservableProperty] private string destinationStatusLine = string.Empty;
        [ObservableProperty] private string deleteAfterImportStatusLine = string.Empty;
        [ObservableProperty] private string keywordsStatusLine = string.Empty;
        [ObservableProperty] private string duplicatePolicy = "Suffix";
        [ObservableProperty] private string verificationMode = "Fast";
        [ObservableProperty] private bool isBrowsingFtpFolders = false;
        [ObservableProperty] private string selectedFtpPresetFolder = "/DCIM";
        [ObservableProperty] private FtpFolderOption? selectedBrowsedFtpFolder;
        [ObservableProperty] private string ftpDialogStatusMessage = "";
        [ObservableProperty] private bool limitFtpThumbnailLoad = false;
        [ObservableProperty] private int ftpInitialThumbnailCount = 0;
        [ObservableProperty] private bool showSkippedFoldersDialog = false;
        [ObservableProperty] private string skippedFoldersReportTitle = AppLocalizer.Get("Vm_SkippedFoldersReportTitle");
        [ObservableProperty] private string skippedFoldersReportText = string.Empty;
        /// <summary>When true, do not open the skipped-folder summary dialog when the only skips are paths excluded by user rules (FTP listing errors still show).</summary>
        [ObservableProperty] private bool suppressExcludedFolderScanReminders;
        /// <summary>When true, the skipped-folder dialog shows an option to stop reminding when skips are only from Scan exclusions.</summary>
        [ObservableProperty] private bool showSkippedFoldersSuppressReminderOption;
        [ObservableProperty] private bool showDriveSelectionDialog = false;
        [ObservableProperty] private bool isDarkTheme = true;

        /// <summary>Release tag for which the user was already alerted via update popup.</summary>
        [ObservableProperty] private string lastNotifiedUpdateTag = string.Empty;
        /// <summary>When true, Explorer opens your destination folder after import completes successfully.</summary>
        [ObservableProperty] private bool openDestinationFolderWhenImportCompletes = true;
        /// <summary>Shows disk space vs. selection size before the normal confirm-import step.</summary>
        [ObservableProperty] private bool showCompactImportSummaryModal = true;
        [ObservableProperty] private bool confirmCancelImportRequest = true;
        [ObservableProperty] private int importCooldownBetweenFilesMs;
        [ObservableProperty] private bool importSingleThreaded;
        [ObservableProperty] private string destinationPreset = "Custom";
        /// <summary>Folder used when preset is &quot;Last session&quot;.</summary>
        [ObservableProperty] private string lastSessionDestinationRoot = string.Empty;
        /// <summary>Announced to assistive tech on import completion/failure.</summary>
        [ObservableProperty] private string accessibilityAnnouncement = string.Empty;

        public ObservableCollection<DestinationPresetOption> DestinationPresetOptions { get; } = new();

        public bool HasNoSidebarSources => Sources.Count == 0;

        public bool HasEmptyFilterNoGroups =>
            Groups.Count == 0 &&
            (!string.IsNullOrWhiteSpace(FilterKeyword) ||
             FilterStartDate.HasValue ||
             FilterEndDate.HasValue ||
             !string.IsNullOrWhiteSpace(FilterFileType));

        public bool ShowShootGroupsList => Groups.Count > 0;

        public bool ShowEmptyNoSourcesPanel => !ShowShootGroupsList && HasNoSidebarSources;

        public bool ShowEmptyFilterPanel => !ShowShootGroupsList && !HasNoSidebarSources && HasEmptyFilterNoGroups;

        public bool ShowEmptyFtpFailurePanel =>
            !ShowShootGroupsList &&
            !HasNoSidebarSources &&
            !HasEmptyFilterNoGroups &&
            (HasUnifiedFtpListingFailures || HasLastFtpReconnectFailure);

        public bool ShowEmptyScanPanel =>
            !ShowShootGroupsList &&
            !HasNoSidebarSources &&
            !HasEmptyFilterNoGroups &&
            !ShowEmptyFtpFailurePanel;

        public bool ShowShootListEmptyPlaceholder => !ShowShootGroupsList;

        private void RefreshUxEmptyStateHints()
        {
            OnPropertyChanged(nameof(HasNoSidebarSources));
            OnPropertyChanged(nameof(HasEmptyFilterNoGroups));
            OnPropertyChanged(nameof(ShowShootGroupsList));
            OnPropertyChanged(nameof(ShowShootListEmptyPlaceholder));
            OnPropertyChanged(nameof(ShowEmptyNoSourcesPanel));
            OnPropertyChanged(nameof(ShowEmptyFilterPanel));
            OnPropertyChanged(nameof(ShowEmptyFtpFailurePanel));
            OnPropertyChanged(nameof(ShowEmptyScanPanel));
        }

        private static string BuildShootExpansionKey(ItemGroup g) =>
            $"{g.FolderPath ?? string.Empty}|{g.Title ?? string.Empty}|{g.StartDate.Ticks}";

        // Observable properties (must be at class scope, after constructor)
        /// <summary>
        /// Performs asynchronous initialization for the main view model at app startup.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_startupInitialized)
                return;

            // Load configuration and import history (sync, but could be made async if needed)
            LoadConfig();
            _ = Task.Run(() =>
            {
                try
                {
                    _databaseService.TryPeriodicVacuum();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "SQLite periodic VACUUM was skipped.");
                }
            });
            RebuildSkippedFolderRuleEntries();
            if (string.IsNullOrWhiteSpace(FtpDialogStatusMessage))
            {
                FtpDialogStatusMessage = AppLocalizer.Get("Vm_Ftp_DefaultDialogStatus");
            }
            RepopulateLanguageOptions();
            SyncNamingOptionsFromTemplate();
            RefreshNamingPreviewExamples();
            LoadImportHistory();
            TryRestorePendingImportPlanNotice();
            // Initial population of sidebar sources (drives + saved FTP)
            try
            {
                await ScanDrivesAsync();
                // Start watching for device connect/disconnect events
                try
                {
                    _deviceWatcher.DeviceConnected += (drive) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            try
                            {
                                var info = new DriveInfo(drive);
                                if (!info.IsReady) return;
                                string deviceId = GetOrCreateDeviceIdForDrive(info);
                                _driveDeviceIdByPath[info.Name] = deviceId;
                                _drivePathByDeviceId[deviceId] = info.Name;

                                bool includeByDefault = _selectedDriveDeviceIds.Count == 0 && info.DriveType == DriveType.Removable;
                                if ((includeByDefault || _selectedDriveDeviceIds.Contains(deviceId)) && !Sources.Contains(info.Name))
                                {
                                    Sources.Add(info.Name);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogDebug(ex, "Drive connect handler failed.");
                            }
                        });
                    };
                    _deviceWatcher.DeviceDisconnected += (drive) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Remove string drives that match the drive letter
                            for (int i = Sources.Count - 1; i >= 0; i--)
                            {
                                if (Sources[i] is string s && string.Equals(s, drive, StringComparison.OrdinalIgnoreCase))
                                {
                                    Sources.RemoveAt(i);
                                }
                            }
                            if (SelectedSource is string ss && string.Equals(ss, drive, StringComparison.OrdinalIgnoreCase))
                                SelectedSource = null;
                        });
                    };
                    _deviceWatcher.Start();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Device watcher startup failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Initial drive scan or device watcher registration failed.");
            }

            // Defer FTP reconnect so startup (splash → main window shown) does not contend with network I/O.
            _ = Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _ = TryReconnectLastFtpAsync()),
                System.Windows.Threading.DispatcherPriority.Background);

            _startupInitialized = true;

            CheckUpdates();

            await Task.CompletedTask;
        }

        private void TryRestorePendingImportPlanNotice()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (!File.Exists(path))
                {
                    return;
                }

                string json = File.ReadAllText(path);
                var plan = System.Text.Json.JsonSerializer.Deserialize<PendingImportPlan>(json);
                if (plan == null || plan.SelectedSourcePaths.Count == 0)
                {
                    return;
                }

                StatusMessage = $"Recovered pending import plan ({plan.SelectedSourcePaths.Count} files from {plan.SourceDisplay}).";
            }
            catch
            {
                // Ignore pending plan read failures.
            }
        }

        // Observable properties (must be at class scope)
        // Internal state fields
        // Only remove truly unused fields to resolve warnings.
        // The following fields are required for class functionality and are
        // initialized in the constructor.
        private readonly ILocalScanner _scanner;
        private readonly IFtpScanner _ftpScanner;
        private readonly IShootFilterService _shootFilterService;
        private readonly IFtpWorkflowService _ftpWorkflowService;
        private readonly IUnifiedConcreteSourceScanService _unifiedConcreteSourceScanService;
        private readonly IFtpCredentialStore _ftpCredentialStore;
        private readonly IFileDialogService _fileDialogService;
        private readonly IShellService _shellService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IUpdateService _updateService;
        private readonly IDeviceWatcher _deviceWatcher;
        private readonly IFileProviderFactory _fileProviderFactory;
        private readonly IIngestEngineFactory _ingestEngineFactory;
        private readonly GroupBuilder _groupBuilder;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<MainViewModel> _logger;
        private bool _loadingConfig = false;
        private bool _refreshingDestinationPresetLabels = false;
        private bool _startupInitialized = false;
        private double _savedWindowWidth = 960;
        private double _savedWindowHeight = 620;
        private bool _savedWindowMaximized = false;
        private double? _savedWindowLeft;
        private double? _savedWindowTop;
        private List<ImportItem> _currentSourceItems = new();
        private readonly Dictionary<string, List<ImportItem>> _sourceItemsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, object?> _thumbnailByItemKey = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _importCancellationSource;
        private readonly Dictionary<string, bool> _shootGroupExpandedMemory = new(StringComparer.OrdinalIgnoreCase);
        private readonly UnifiedSourceItem _unifiedSource = new();
        private bool _updatingNamingFromUi = false;
        private bool _suppressStatusFeedFromStatusMessage;

        public ObservableCollection<FilterChipViewModel> ActiveFilterChips { get; } = new();
        public ObservableCollection<NotificationFeedLine> NotificationFeed { get; } = new();

        /// <summary>Entries for the Preferences display language combo (bound with SelectedValuePath/Code).</summary>
        public ObservableCollection<LanguageOption> UiLanguageOptions { get; } = new();

        // Remove only these truly unused fields:
        // private bool _isUpdatingSelectAll = false;
        // private bool isUpdatingSelectAll;
        // private IDeviceWatcher? _watcher;
        partial void OnSelectedSourceChanged(object? value)
        {
            OnPropertyChanged(nameof(HasSelectedSource));
            OnPropertyChanged(nameof(IsLocalSourceSelected));
            OnPropertyChanged(nameof(IsFtpSourceSelected));
            OnPropertyChanged(nameof(IsUnifiedSourceSelected));

            if (value is string drive)
            {
                ScanPath = drive;
            }
            else if (value is FtpSourceItem ftp)
            {
                ScanPath = NormalizeFtpPath(ftp.RemoteFolder);
            }
            else if (value is UnifiedSourceItem)
            {
                ScanPath = string.Empty;
            }

            if (value is UnifiedSourceItem)
            {
                _ = LoadUnifiedSourceItemsAsync(forceRefresh: false);
            }
            else if (value != null)
            {
                LoadSourceItems(value, forceRefresh: false);
            }
        }

        partial void OnDestinationRootChanged(string value)
        {
            SaveConfig();
            RefreshImportReadinessSummary();
        }

        partial void OnDestinationPresetChanged(string value)
        {
            if (_loadingConfig || _refreshingDestinationPresetLabels)
            {
                return;
            }

            switch (value)
            {
                case "Pictures":
                    DestinationRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QuickMediaIngest");
                    break;
                case "LastSession":
                    if (!string.IsNullOrWhiteSpace(LastSessionDestinationRoot) &&
                        Directory.Exists(LastSessionDestinationRoot))
                    {
                        DestinationRoot = LastSessionDestinationRoot;
                    }
                    else
                    {
                        StatusMessage = AppLocalizer.Get("Vm_Status_LastSessionDestinationUnavailable");
                    }

                    break;
            }
        }
    }
}
