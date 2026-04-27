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


namespace QuickMediaIngest.ViewModels
{
    /// <summary>
    /// One entry in the display language list (code = BCP 47, empty = use Windows language).
    /// </summary>
    public sealed class LanguageOption
    {
        public LanguageOption(string code, string label)
        {
            Code = code;
            Label = label;
        }

        public string Code { get; }
        public string Label { get; }
    }

    /// <summary>One line in the sidebar notification feed.</summary>
    public sealed class NotificationFeedLine
    {
        public required string DisplayText { get; init; }
        public bool UseSuccessAccent { get; init; }
    }

    /// <summary>
    /// Active filter chip shown in the main toolbar.
    /// </summary>
    public sealed class FilterChipViewModel
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
    }

    public class SidebarOption
    {
        public string Label { get; set; } = string.Empty;
        public ICommand? Command { get; set; }
    }

    public class SidebarSection : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public ObservableCollection<SidebarOption> Options { get; set; } = new();
        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
    }

    public class AdbSourceItem
    {
        public string DeviceSerial { get; set; } = "default";
        public override string ToString() => "Android (ADB)";
    }

    public partial class MainViewModel : ObservableObject
    {
        private const string FilterTypeAllMedia = "All Media";
        private const string FilterTypeImages = "Images";
        private const string FilterTypeVideos = "Videos";
        private const string FilterTypeRaw = "RAW";
        private const string FilterTypeJpeg = "JPG/JPEG";

        [RelayCommand]
        private void OpenImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var w = new QuickMediaIngest.ImportHistoryWindow();
                    w.Owner = Application.Current.MainWindow;
                    w.DataContext = this;
                    w.Show();
                });
            }
            catch { }
        }
        [ObservableProperty] private bool showSettingsDialog = false;
        [ObservableProperty] private bool showScanExclusionsPanel = false;

        [RelayCommand] private void ToggleSettings()
        {
            ShowScanExclusionsPanel = false;
            ShowSettingsDialog = true;
        }

        [RelayCommand] private void OpenScanExclusions()
        {
            ShowSettingsDialog = false;
            ShowScanExclusionsPanel = true;
        }

        [RelayCommand] private void CloseScanExclusions() => ShowScanExclusionsPanel = false;

        public IEnumerable<ImportHistoryRecord> RecentImportHistory => ImportHistoryRecords.Take(7);

        // --- Sidebar and import progress fields ---
        private bool _isUpdatingSelectAll = false;
        private bool _isBulkUpdatingGroupExpansion = false;
        private bool _selectAll = true;
        private long _processedBytesForImport = 0;
        private DateTime _importStartedAtUtc = DateTime.MinValue;
        private readonly Queue<QueuedImportJob> _importQueue = new();
        private readonly object _importQueueLock = new();
        private readonly HashSet<string> _selectedDriveDeviceIds = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _skippedFoldersBySource = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _driveDeviceIdByPath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _drivePathByDeviceId = new(StringComparer.OrdinalIgnoreCase);
        // Sidebar sections for expandable menu
        public ObservableCollection<SidebarSection> SidebarSections { get; } = new();

        private void InitializeSidebarSections()
        {
            SidebarSections.Clear();
            SidebarSections.Add(new SidebarSection
            {
                Title = "Import",
                IsExpanded = true,
                Options =
                {
                    new SidebarOption { Label = "Start Import", Command = ImportCommand },
                    new SidebarOption { Label = "Import History", Command = OpenImportHistoryCommand },
                    new SidebarOption { Label = "Select All", Command = SelectAllCommand },
                    new SidebarOption { Label = "Deselect All", Command = new RelayCommand(DeselectAllShoots) },
                }
            });
            SidebarSections.Add(new SidebarSection
            {
                Title = "Sources",
                Options =
                {
                    new SidebarOption { Label = "Add FTP Source", Command = ToggleAddFtpCommand },
                    new SidebarOption { Label = "Rescan Drives", Command = RescanCommand },
                    new SidebarOption { Label = "Refresh Unified", Command = RefreshUnifiedCommand },
                }
            });
        }

        // Call this in constructor or initialization
        // ... rest of MainViewModel ...
        [ObservableProperty] DateTime? filterStartDate = null;
        [ObservableProperty] DateTime? filterEndDate = null;
        [ObservableProperty] string filterFileType = string.Empty;
        [ObservableProperty] string filterKeyword = string.Empty;

        private System.ComponentModel.ICollectionView? _filteredItemsView;
        public System.ComponentModel.ICollectionView? FilteredItemsView
        {
            get => _filteredItemsView;
            private set => SetProperty(ref _filteredItemsView, value);
        }

        public ObservableCollection<string> AvailableFileTypes { get; } = new ObservableCollection<string>();

        [RelayCommand]
        public void ClearFilters()
        {
            FilterStartDate = null;
            FilterEndDate = null;
            FilterFileType = string.Empty;
            FilterKeyword = string.Empty;
        }

        [RelayCommand]
        void DetectDuplicates()
        {
            // Build a dictionary of quick hashes to ImportItems
            var hashDict = new Dictionary<string, List<ImportItem>>();
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    string hash = ComputeQuickHash(item);
                    if (!hashDict.TryGetValue(hash, out var list))
                    {
                        list = new List<ImportItem>();
                        hashDict[hash] = list;
                    }
                    list.Add(item);
                }
            }
            // Mark duplicates
            foreach (var list in hashDict.Values)
            {
                bool isDup = list.Count > 1;
                foreach (var item in list)
                    item.IsDuplicate = isDup;
            }
        }

        string ComputeQuickHash(ImportItem item)
        {
            // Use file path + size as a quick hash (can be improved)
            string input = item.SourcePath + ":" + item.FileSize;
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            byte[] hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public MainViewModel(
            ILocalScanner scanner,
            IFtpScanner ftpScanner,
            IThumbnailService thumbnailService,
            IUpdateService updateService,
            IDeviceWatcher deviceWatcher,
            IFileProviderFactory fileProviderFactory,
            IIngestEngineFactory ingestEngineFactory,
            GroupBuilder groupBuilder,
            ILogger<MainViewModel> logger)
        {
            _scanner = scanner;
            _ftpScanner = ftpScanner;
            _thumbnailService = thumbnailService;
            _updateService = updateService;
            _deviceWatcher = deviceWatcher;
            _fileProviderFactory = fileProviderFactory;
            _ingestEngineFactory = ingestEngineFactory;
            _groupBuilder = groupBuilder;
            _logger = logger;

            InitializeSidebarSections();
            ImportHistoryRecords.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RecentImportHistory));
        }

        // Observable properties (must be at class scope, after constructor)
        [ObservableProperty] private string ftpHost = string.Empty;
        [ObservableProperty] private int ftpPort = 21;
        [ObservableProperty] private string ftpUser = string.Empty;
        [ObservableProperty] private string ftpPass = string.Empty;
        [ObservableProperty] private string ftpRemoteFolder = "/DCIM";
        [ObservableProperty] private bool autoReconnectLastFtp = true;
        [ObservableProperty] private bool isTestingFtp = false;
        [ObservableProperty] private bool showAddFtpDialog = false;
        [ObservableProperty] private bool settingsMenuExpanded = true;
        [ObservableProperty] private string albumName = "New Album";
        [ObservableProperty] private string statusMessage = "Ready";
        [ObservableProperty] private int progressPercent = 0;
        [ObservableProperty] private int totalFilesForImport = 0;
        [ObservableProperty] private int currentFileBeingImported = 0;
        [ObservableProperty] private int processedFilesForImport = 0;
        [ObservableProperty] private int failedFilesForImport = 0;
        [ObservableProperty] private int currentGroupFileBeingImported = 0;
        [ObservableProperty] private int totalFilesInCurrentGroup = 0;
        [ObservableProperty] private int currentGroupProgressPercent = 0;
        [ObservableProperty] private string currentImportGroupTitle = string.Empty;
        [ObservableProperty] private string importElapsedText = "00:00:00";
        [ObservableProperty] private string importEtaText = "--:--:--";
        [ObservableProperty] private string importDataRateText = "-- MB/s";
        [ObservableProperty] private DateTime importStartedAtUtc = DateTime.MinValue;
        [ObservableProperty] private long processedBytesForImport = 0;
        [ObservableProperty] private bool showImportProgressDialog = false;
        [ObservableProperty] private bool showScanProgressDialog = false;
        [ObservableProperty] private int scanProgressPercent = 0;
        [ObservableProperty] private int totalFoldersToScan = 0;
        [ObservableProperty] private int scannedFolders = 0;
        [ObservableProperty] private int totalFilesToScan = 0;
        [ObservableProperty] private int scannedFiles = 0;
        [ObservableProperty] private string scanDialogTitle = "Loading Import List...";
        [ObservableProperty] private string scanProgressMessage = "Preparing scan...";
        [ObservableProperty] private string currentScanFolder = "/";
        [ObservableProperty] private int currentScanFolderProcessedFiles = 0;
        [ObservableProperty] private int currentScanFolderTotalFiles = 0;
        [ObservableProperty] private object? selectedSource;
        [ObservableProperty] private string destinationRoot = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "QuickMediaIngest");
        [ObservableProperty] private bool deleteAfterImport = false;
        /// <summary>After the user responds once to the delete-after-import safety prompt (OK or Cancel), do not show it again.</summary>
        [ObservableProperty] private bool deleteAfterImportPromptDismissed;
        [ObservableProperty] private bool selectAll = true;
        [ObservableProperty] private bool showUpdateBanner = false;
        [ObservableProperty] private string updateUrl = string.Empty;
        [ObservableProperty] private bool showAboutDialog = false;
        [ObservableProperty] private bool isUpdateAvailable = false;
        [ObservableProperty] private double updateProgress = 0.0;
        [ObservableProperty] private string updateStatus = string.Empty;
        [ObservableProperty] private bool isDownloadingUpdate = false;
        [ObservableProperty] private string updateDownloadSpeedText = "-- MB/s";
        [ObservableProperty] private string updateDownloadEtaText = "--:--:--";
        [ObservableProperty] private bool isCheckingForUpdate = false;
        [ObservableProperty] private int updateIntervalHours = 24;
        [ObservableProperty] private string updatePackageType = "Portable";
        [ObservableProperty] private string namingTemplate = "[Date]_[ShootName]_[Original]";
        [ObservableProperty] private string namingPreset = "Recommended (Date + Shoot + Original)";
        [ObservableProperty] private bool namingIncludeDate = true;
        [ObservableProperty] private bool namingIncludeTime = false;
        [ObservableProperty] private bool namingIncludeSequence = false;
        [ObservableProperty] private bool namingIncludeShootName = true;
        [ObservableProperty] private bool namingIncludeOriginalName = true;
        [ObservableProperty] private string namingDateFormat = "yyyy-MM-dd";
        [ObservableProperty] private string namingTimeFormat = "HH-mm-ss";
        [ObservableProperty] private string namingSeparator = "_";
        [ObservableProperty] private string namingShootNameSample = "my-shoot";
        [ObservableProperty] private bool namingLowercase = true;
        [ObservableProperty] private string thumbnailPerformanceMode = "Balanced";
        [ObservableProperty] private double thumbnailSize = 120;
        [ObservableProperty] private string scanPath = string.Empty;
        [ObservableProperty] private bool scanIncludeSubfolders = true;
        [ObservableProperty] private bool isImporting = false;
        [ObservableProperty] private int queuedImportCount = 0;
        [ObservableProperty] private int timeBetweenShootsHours = 4;
        [ObservableProperty] private bool expandPreviewStacks = false;
        [ObservableProperty] private bool groupRawAndRenderedPairs = false;
        [ObservableProperty] private string uiLanguage = string.Empty;
        [ObservableProperty] private bool embedKeywordsOnImport = false;
        [ObservableProperty] private bool confirmBeforeImport = false;
        [ObservableProperty] private bool settingsAdvancedExpanded = false;
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
        [ObservableProperty] private string skippedFoldersReportTitle = "FTP Scan: Skipped Folders";
        [ObservableProperty] private string skippedFoldersReportText = string.Empty;
        /// <summary>When true, do not open the skipped-folder summary dialog when the only skips are paths excluded by user rules (FTP listing errors still show).</summary>
        [ObservableProperty] private bool suppressExcludedFolderScanReminders;
        /// <summary>When true, the skipped-folder dialog shows an option to stop reminding when skips are only from Scan exclusions.</summary>
        [ObservableProperty] private bool showSkippedFoldersSuppressReminderOption;
        [ObservableProperty] private bool showDriveSelectionDialog = false;
        [ObservableProperty] private bool isDarkTheme = true;

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
                            catch
                            {
                                // Ignore transient drive-event failures.
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
                catch { }
            }
            catch { }

            // Defer FTP reconnect so startup (splash → main window shown) does not contend with network I/O.
            _ = Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _ = TryReconnectLastFtpAsync()),
                System.Windows.Threading.DispatcherPriority.Background);

            _startupInitialized = true;

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
        private readonly IThumbnailService _thumbnailService;
        private readonly IUpdateService _updateService;
        private readonly IDeviceWatcher _deviceWatcher;
        private readonly IFileProviderFactory _fileProviderFactory;
        private readonly IIngestEngineFactory _ingestEngineFactory;
        private readonly GroupBuilder _groupBuilder;
        private readonly ILogger<MainViewModel> _logger;
        private bool _startupInitialized = false;
        private double _savedWindowWidth = 960;
        private double _savedWindowHeight = 620;
        private bool _savedWindowMaximized = false;
        private double? _savedWindowLeft;
        private double? _savedWindowTop;
        private List<ImportItem> _currentSourceItems = new();
        private readonly Dictionary<string, List<ImportItem>> _sourceItemsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, object?> _thumbnailByItemKey = new(StringComparer.OrdinalIgnoreCase);
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


        [RelayCommand] private async Task ToggleAddFtp()
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
        partial void OnSettingsAdvancedExpandedChanged(bool value) => SaveConfig();
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
            catch { }
        }

        [RelayCommand]
        private void ExportImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title = "Export Import History",
                        Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                        FileName = "import-history.json"
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

                            sb.AppendLine("StartedAt,DurationSeconds,FilesSelected,FilesImported,FailedFiles,Source,Destination");
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
            catch { }
        }
        public ObservableCollection<string> CommonFtpFolders { get; } = new ObservableCollection<string>
        {
            "/DCIM",
            "/DCIM/Camera",
            "/Pictures",
            "/Movies"
        };
        public ObservableCollection<FtpFolderOption> BrowsedFtpFolders { get; } = new ObservableCollection<FtpFolderOption>();
                public ObservableCollection<UpdateIntervalOption> IntervalOptions { get; } = new ObservableCollection<UpdateIntervalOption>
        {
            new UpdateIntervalOption { Display = "Daily", Hours = 24 },
            new UpdateIntervalOption { Display = "Weekly", Hours = 168 },
            new UpdateIntervalOption { Display = "Monthly", Hours = 720 },
            new UpdateIntervalOption { Display = "Off", Hours = -1 }
        };

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
        [RelayCommand] private void OpenGitHub()
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
            catch { }

            OpenUrl(repo);
        }
        [RelayCommand] private void RefreshUpdate() => CheckUpdates(force: true);
        [RelayCommand]
        private async Task RefreshAllSources()
        {
            await ScanDrivesAsync();
            _sourceItemsCache.Clear();
            _thumbnailByItemKey.Clear();
            ClearThumbnailDiskCache();

            if (SelectedSource is UnifiedSourceItem || SelectedSource == null)
            {
                await LoadUnifiedSourceItemsAsync(forceRefresh: true);
                if (SelectedSource == null)
                {
                    SelectedSource = _unifiedSource;
                }
                return;
            }

            LoadSourceItems(SelectedSource, forceRefresh: true);
        }
        // BrowseDestination command removed; UI entry deleted.
        [RelayCommand] private void Rescan() => OpenDriveSelectionDialog();
        [RelayCommand] private void BrowseScanPath() => ExecuteBrowseScanPath();
        [RelayCommand] private void BuildSelectedPreviews() => ExecuteBuildSelectedPreviews();
        [RelayCommand] private void SelectAllShoots() => SetAllShootsSelected(true);
        [RelayCommand] private void DeselectAllShoots() => SetAllShootsSelected(false);
        [RelayCommand]
        private async Task ConfirmDriveSelection() => await ExecuteConfirmDriveSelectionAsync();
        [RelayCommand] private void CancelDriveSelection() => ShowDriveSelectionDialog = false;
        [RelayCommand] private void SkipFolder(string? folderPath) => ExecuteSkipFolder(folderPath);

        [RelayCommand]
        private void ExitApplication()
        {
            SaveConfig();
            Application.Current.Shutdown(0);
        }
        [RelayCommand]
        private async Task RemoveDriveExclusion(string? deviceId) => await ExecuteRemoveDriveExclusionAsync(deviceId);
        [RelayCommand] private void RemoveSkippedFolderRule(SkippedFolderRuleEntry? entry) => ExecuteRemoveSkippedFolderRule(entry);
        public void SelectAllVisible() => SetAllShootsSelected(true);
        public void DeselectAllVisible() => SetAllShootsSelected(false);

        // Keyboard accelerator commands for UI
        public ICommand SelectAllCommand => new RelayCommand(SelectAllShoots);
        public ICommand CancelCommand => new RelayCommand(DismissTopOverlay);

        partial void OnSelectedFtpPresetFolderChanged(string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                FtpRemoteFolder = value;
        }
        partial void OnTimeBetweenShootsHoursChanged(int value)
        {
            int clamped = Math.Clamp(value, 1, 24);
            if (timeBetweenShootsHours != clamped)
                TimeBetweenShootsHours = clamped;
            SaveConfig();
            RebuildGroupsFromCurrentItems();
        }

        private void CheckUpdates(bool force = false)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                _logger.LogInformation("Checking for updates from view model. Force={Force}", force);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsCheckingForUpdate = true;
                    UpdateStatus = "Checking for updates...";
                    UpdateProgress = 0.0;
                });

                var url = await _updateService.CheckForUpdateAsync(UpdateIntervalHours, force, UpdatePackageType);

                if (!string.IsNullOrEmpty(url))
                {
                    string assetLabel = GetUpdateAssetLabel(url);
                     Application.Current.Dispatcher.Invoke(() =>
                     {
                         UpdateUrl = url;
                         ShowUpdateBanner = true;
                         IsUpdateAvailable = true;
                         UpdateStatus = $"Update available: {assetLabel}";
                         StatusMessage = $"Update found on GitHub: {assetLabel}";
                         UpdateProgress = 0.0;
                     });
                }
                else if (force)
                {
                    string expected = UpdatePackageType == "Installer" ? "QuickMediaIngest.msi" : "QuickMediaIngest.exe";
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                         StatusMessage = $"No updates found. App is up to date ({expected}).";
                         UpdateStatus = $"No updates found for {expected}.";
                         IsUpdateAvailable = false;
                     });
                }

                Application.Current.Dispatcher.Invoke(() => IsCheckingForUpdate = false);
            });
        }

        private static string GetUpdateAssetLabel(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return "release page";
            }

            try
            {
                var uri = new Uri(url);
                string fileName = Path.GetFileName(uri.LocalPath);
                return string.IsNullOrWhiteSpace(fileName) ? "release page" : Uri.UnescapeDataString(fileName);
            }
            catch
            {
                return "release page";
            }
        }

        private async void ExecuteDownloadUpdate()
        {
            if (string.IsNullOrEmpty(UpdateUrl)) return;

            _logger.LogInformation("Starting update download from {UpdateUrl}.", UpdateUrl);

            IsDownloadingUpdate = true;
            UpdateProgress = 0.0;
            UpdateStatus = "Starting download...";
            ShowUpdateBanner = false;

            try
            {
                string ext = Path.GetExtension(UpdateUrl).ToLowerInvariant();
                string fileName = ext switch
                {
                    ".msi" => "QuickMediaIngest_Update.msi",
                    ".exe" => "QuickMediaIngest_Update.exe",
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(fileName))
                {
                    string updateTempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "updates");
                    Directory.CreateDirectory(updateTempDir);
                    string versionSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                    string tempPath = Path.Combine(
                        updateTempDir,
                        $"{Path.GetFileNameWithoutExtension(fileName)}_{versionSuffix}{Path.GetExtension(fileName)}");

                    using var client = new System.Net.Http.HttpClient();
                    using var response = await client.GetAsync(UpdateUrl, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var contentLength = response.Content.Headers.ContentLength;
                    using var contentStream = await response.Content.ReadAsStreamAsync();
                    using var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        var buffer = new byte[81920];
                        long totalRead = 0;
                        int read;
                        var sw = Stopwatch.StartNew();
                        while ((read = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, read));
                            totalRead += read;

                            // Compute progress, speed and ETA
                            double percent = 0.0;
                            if (contentLength.HasValue && contentLength.Value > 0)
                            {
                                percent = Math.Round((double)totalRead / contentLength.Value * 100.0, 1);
                            }

                            double bytesPerSecond = sw.Elapsed.TotalSeconds > 0 ? totalRead / sw.Elapsed.TotalSeconds : 0;
                            string speedText = bytesPerSecond > 0 ? $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s" : "-- MB/s";
                            string etaText = "--:--:--";
                            if (contentLength.HasValue && bytesPerSecond > 0)
                            {
                                double remaining = Math.Max(0, contentLength.Value - totalRead);
                                etaText = TimeSpan.FromSeconds(remaining / bytesPerSecond).ToString(@"hh\:mm\:ss");
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                UpdateProgress = percent;
                                UpdateStatus = contentLength.HasValue ? $"Downloading update... {percent}%" : $"Downloading... {totalRead / 1024:N0} KB";
                                UpdateDownloadSpeedText = speedText;
                                UpdateDownloadEtaText = etaText;
                            });
                        }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UpdateProgress = 100.0;
                        UpdateStatus = "Download complete. Preparing external updater...";
                    });

                    if (ext == ".msi" || ext == ".exe")
                    {
                        string currentExePath = Environment.ProcessPath
                            ?? Process.GetCurrentProcess().MainModule?.FileName
                            ?? string.Empty;
                        if (string.IsNullOrWhiteSpace(currentExePath) || !File.Exists(currentExePath))
                        {
                            throw new InvalidOperationException("Unable to locate current executable path for update handoff.");
                        }

                        string updaterScript = BuildUpdateHandoffScript(
                            tempPath,
                            ext,
                            currentExePath,
                            Process.GetCurrentProcess().Id,
                            UpdatePackageType);

                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{updaterScript}\"")
                        {
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        });

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            UpdateStatus = "Update handoff started. Closing app to install update...";
                        });
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    // Non-executable update: open in browser
                    OpenUrl(UpdateUrl);
                    Application.Current.Dispatcher.Invoke(() => UpdateStatus = "Opened update URL in browser.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update download failed.");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateStatus = $"Update download failed: {ex.Message}";
                    IsUpdateAvailable = true;
                    ShowUpdateBanner = true;
                });
            }
            finally
            {
                IsDownloadingUpdate = false;
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        private static string BuildUpdateHandoffScript(string downloadedUpdatePath, string ext, string currentExePath, int currentPid, string packageType)
        {
            string tempScript = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "updates", $"apply-update-{DateTime.UtcNow:yyyyMMddHHmmssfff}.cmd");
            Directory.CreateDirectory(Path.GetDirectoryName(tempScript) ?? Path.GetTempPath());

            string script = $@"@echo off
setlocal enableextensions
set ""QMI_UPDATE_FILE={downloadedUpdatePath}""
set ""QMI_CURRENT_EXE={currentExePath}""
set ""QMI_PID={currentPid}""
set ""QMI_PACKAGE={packageType}""
set ""QMI_EXT={ext}""

for /L %%i in (1,1,180) do (
  tasklist /FI ""PID eq %QMI_PID%"" | findstr /I /C:""%QMI_PID%"" >nul
  if errorlevel 1 goto :ready
  timeout /t 1 /nobreak >nul
)

:ready
if /I ""%QMI_EXT%""=="".msi"" (
  start """" /wait msiexec /i ""%QMI_UPDATE_FILE%"" /passive /norestart
  start """" ""%QMI_CURRENT_EXE%""
  goto :cleanup
)

if /I ""%QMI_EXT%""=="".exe"" (
  if /I ""%QMI_PACKAGE%""==""Portable"" (
    copy /Y ""%QMI_UPDATE_FILE%"" ""%QMI_CURRENT_EXE%"" >nul
    start """" ""%QMI_CURRENT_EXE%""
  ) else (
    start """" ""%QMI_UPDATE_FILE%""
  )
  goto :cleanup
)

:cleanup
del /Q ""%QMI_UPDATE_FILE%"" >nul 2>nul
del /Q ""%~f0"" >nul 2>nul
";

            File.WriteAllText(tempScript, script, Encoding.ASCII);
            return tempScript;
        }

                private void ExecuteSaveFtp()
        {
            if (string.IsNullOrEmpty(FtpHost)) return;

            string remoteFolder = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);

            var ftp = new FtpSourceItem
            {
                Host = FtpHost,
                Port = FtpPort,
                User = FtpUser,
                Pass = FtpPass,
                RemoteFolder = remoteFolder
            };

            bool exists = Sources.OfType<FtpSourceItem>().Any(f => f.Host == ftp.Host && f.RemoteFolder == ftp.RemoteFolder);
            if (!exists) Sources.Add(ftp);

            FtpRemoteFolder = remoteFolder;
            ShowAddFtpDialog = false;
            SaveConfig();
            SelectedSource = ftp; // triggers scan instantly
        }

        private async void ExecuteTestFtpConnection()
        {
            if (IsTestingFtp)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_EnterHostBeforeTest");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_PortRange");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(FtpRemoteFolder);
            IsTestingFtp = true;
            StatusMessage = $"Testing FTP connection to {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Testing FTP connection to {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await Task.Run(async () =>
                    await _ftpScanner.TestConnectionAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        FtpPass,
                        remotePath,
                        15,
                        timeout.Token));

                StatusMessage = result.Success
                    ? $"FTP test successful. {result.Message} Use /DCIM or /DCIM/Camera for faster phone scans."
                    : $"FTP test failed. {result.Message}";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection test failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"FTP test failed. {ex.Message}";
                FtpDialogStatusMessage = StatusMessage;
            }
            finally
            {
                IsTestingFtp = false;
            }
        }

        private async void ExecuteBrowseFtpFolders()
        {
            if (IsBrowsingFtpFolders || IsTestingFtp)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(FtpHost))
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_EnterHostBeforeBrowse");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_PortRange");
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            IsBrowsingFtpFolders = true;
            StatusMessage = $"Browsing FTP folders at {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;
            _logger.LogInformation("Browsing FTP folders at {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var folders = await Task.Run(async () =>
                    await _ftpScanner.ListDirectoriesAsync(
                        FtpHost,
                        FtpPort,
                        FtpUser,
                        FtpPass,
                        remotePath,
                        15,
                        timeout.Token));

                BrowsedFtpFolders.Clear();
                BrowsedFtpFolders.Add(new FtpFolderOption { Path = remotePath, Label = $"Use current folder ({remotePath})" });

                string? parentPath = GetParentFtpPath(remotePath);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    BrowsedFtpFolders.Add(new FtpFolderOption { Path = parentPath, Label = $"Parent folder ({parentPath})" });
                }

                foreach (string folder in folders)
                {
                    if (!BrowsedFtpFolders.Any(option => string.Equals(option.Path, folder, StringComparison.OrdinalIgnoreCase)))
                    {
                        BrowsedFtpFolders.Add(new FtpFolderOption { Path = folder, Label = folder });
                    }
                }

                SelectedBrowsedFtpFolder = BrowsedFtpFolders.FirstOrDefault();
                StatusMessage = BrowsedFtpFolders.Count > 0
                    ? $"Connected to FTP. Found {BrowsedFtpFolders.Count} folder option(s) under {remotePath}."
                    : $"Connected to FTP, but no folders were found under {remotePath}.";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("FTP browse timed out for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"Connected to FTP, but browsing {remotePath} timed out. Try /DCIM or /DCIM/Camera.";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP folder browse failed for {Host}:{Port}{RemotePath}.", FtpHost, FtpPort, remotePath);
                StatusMessage = $"FTP folder browse failed. {ex.Message}";
                FtpDialogStatusMessage = StatusMessage;
            }
            finally
            {
                IsBrowsingFtpFolders = false;
            }
        }

        private void ExecuteUseBrowsedFtpFolder()
        {
            if (SelectedBrowsedFtpFolder == null)
            {
                StatusMessage = AppLocalizer.Get("Vm_Ftp_ChooseBrowsedFolder");
                return;
            }

            FtpRemoteFolder = SelectedBrowsedFtpFolder.Path;
            SelectedFtpPresetFolder = SelectedBrowsedFtpFolder.Path;
            StatusMessage = $"FTP folder selected: {SelectedBrowsedFtpFolder.Path}";
            FtpDialogStatusMessage = StatusMessage;
        }

        private async Task TryReconnectLastFtpAsync()
        {
            if (!AutoReconnectLastFtp || string.IsNullOrWhiteSpace(FtpHost))
            {
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var result = await _ftpScanner.TestConnectionAsync(
                    FtpHost,
                    FtpPort,
                    FtpUser,
                    FtpPass,
                    remotePath,
                    8,
                    timeout.Token);

                if (!result.Success)
                {
                    StatusMessage = $"Last FTP source not reachable: {FtpHost}:{FtpPort}{remotePath}";
                    return;
                }

                var ftp = new FtpSourceItem
                {
                    Host = FtpHost,
                    Port = FtpPort,
                    User = FtpUser,
                    Pass = FtpPass,
                    RemoteFolder = remotePath
                };

                bool exists = Sources.OfType<FtpSourceItem>().Any(s =>
                    string.Equals(s.Host, ftp.Host, StringComparison.OrdinalIgnoreCase) &&
                    s.Port == ftp.Port &&
                    string.Equals(NormalizeFtpPath(s.RemoteFolder), remotePath, StringComparison.OrdinalIgnoreCase));

                if (!exists)
                {
                    Sources.Add(ftp);
                }

                StatusMessage = $"Reconnected FTP source: {FtpHost}:{FtpPort}{remotePath}";
            }
            catch
            {
                StatusMessage = $"Last FTP source not reachable: {FtpHost}:{FtpPort}{remotePath}";
            }
        }

        private async void LoadSourceItems(object source, bool forceRefresh = false)
        {
            foreach (var existing in Groups)
            {
                existing.PropertyChanged -= Group_PropertyChanged;
            }
            Groups.Clear();
            EnsureFilteredItemsViewSource();

            string sourceLabel = source.ToString() ?? "source";
            var ftpListingFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string sourceKey = string.Empty;
            try 
            {
                _logger.LogInformation("Loading source items for {SourceLabel}.", sourceLabel);
                List<QuickMediaIngest.Core.Models.ImportItem> items;
                ShowScanProgressDialog = true;
                ScanDialogTitle = "Loading Import List...";
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = 0;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = "Preparing scan...";
                CurrentScanFolder = "/";
                CurrentScanFolderProcessedFiles = 0;
                CurrentScanFolderTotalFiles = 0;

                if (source is FtpSourceItem ftp)
                {
                    string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(ScanPath) ? ftp.RemoteFolder : ScanPath);
                    ftp.RemoteFolder = remotePath;
                    sourceLabel = $"{ftp.Host}{remotePath}";
                    sourceKey = BuildSourceKey(ftp);

                    if (!forceRefresh && _sourceItemsCache.TryGetValue(sourceKey, out var cachedFtpItems))
                    {
                        items = CloneItems(cachedFtpItems);
                        ScanProgressMessage = "Loaded from cache.";
                        ScannedFiles = items.Count;
                        TotalFilesToScan = items.Count;
                        ScanProgressPercent = 100;
                        goto BuildGroups;
                    }

                    StatusMessage = $"Scanning FTP: {sourceLabel}...";
                    ScanProgressMessage = $"Scanning FTP folders in {remotePath}...";

                    items = await _ftpScanner.ScanAsync(
                        ftp.Host,
                        ftp.Port,
                        ftp.User,
                        ftp.Pass,
                        remotePath,
                        ScanIncludeSubfolders,
                        120,
                        CancellationToken.None,
                        progress =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ScannedFolders = progress.ProcessedFolders;
                                TotalFoldersToScan = Math.Max(progress.TotalFolders, progress.ProcessedFolders);
                                ScannedFiles = progress.ProcessedFiles;
                                TotalFilesToScan = Math.Max(progress.TotalFiles, progress.ProcessedFiles);
                                CurrentScanFolder = progress.CurrentFolder;
                                CurrentScanFolderProcessedFiles = progress.CurrentFolderProcessedFiles;
                                CurrentScanFolderTotalFiles = progress.CurrentFolderTotalFiles;

                                if (progress.Phase == "Prescan")
                                {
                                    ScanProgressPercent = 0;
                                    string noteSuffix = string.IsNullOrWhiteSpace(progress.Note) ? string.Empty : $" | {progress.Note}";
                                    ScanProgressMessage = $"Pre-scanning: {progress.ProcessedFolders}/{Math.Max(progress.TotalFolders, progress.ProcessedFolders)} folders (found {progress.TotalFiles} files so far, skipped {progress.SkippedFolders}) | {progress.CurrentFolder}{noteSuffix}";
                                }
                                else
                                {
                                    ScanProgressPercent = progress.TotalFiles > 0
                                        ? (progress.ProcessedFiles * 100) / progress.TotalFiles
                                        : (progress.TotalFolders > 0 ? (progress.ProcessedFolders * 100) / progress.TotalFolders : 0);
                                    string noteSuffix = string.IsNullOrWhiteSpace(progress.Note) ? string.Empty : $" | {progress.Note}";
                                    ScanProgressMessage = $"Scanning: {progress.ProcessedFiles}/{Math.Max(progress.TotalFiles, progress.ProcessedFiles)} files | current folder {progress.CurrentFolderProcessedFiles}/{Math.Max(progress.CurrentFolderTotalFiles, progress.CurrentFolderProcessedFiles)} | folder {progress.ProcessedFolders}/{Math.Max(progress.TotalFolders, progress.ProcessedFolders)} | skipped {progress.SkippedFolders} | {progress.CurrentFolder}{noteSuffix}";
                                }

                                if (!string.IsNullOrWhiteSpace(progress.Note) && progress.Note.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                                {
                                    ftpListingFailures.Add($"{progress.CurrentFolder} - {progress.Note}");
                                }
                            });
                        });
                }
                else if (source is string drive)
                {
                    string localPath = ResolveLocalScanPath(drive, ScanPath);
                    sourceLabel = localPath;
                    sourceKey = BuildSourceKey(localPath);

                    if (!forceRefresh && _sourceItemsCache.TryGetValue(sourceKey, out var cachedLocalItems))
                    {
                        items = CloneItems(cachedLocalItems);
                        ScanProgressMessage = "Loaded from cache.";
                        ScannedFiles = items.Count;
                        TotalFilesToScan = items.Count;
                        ScanProgressPercent = 100;
                        goto BuildGroups;
                    }

                    if (!Directory.Exists(localPath))
                    {
                        StatusMessage = $"Scan path not found: {localPath}";
                        return;
                    }

                    StatusMessage = $"Scanning: {localPath}...";
                    ScanProgressMessage = $"Scanning local folders in {localPath}...";
                    CurrentScanFolder = localPath;
                    items = await Task.Run(() => _scanner.Scan(localPath, ScanIncludeSubfolders, (scanned, total) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ScannedFolders = scanned;
                            TotalFoldersToScan = total;
                            ScanProgressPercent = total > 0 ? (scanned * 100) / total : 0;
                            ScanProgressMessage = $"Scanning folders: {scanned}/{total}";
                            ScannedFiles = 0;
                            TotalFilesToScan = 0;
                            CurrentScanFolderProcessedFiles = 0;
                            CurrentScanFolderTotalFiles = 0;
                        });
                    }));
                }
                else return;

BuildGroups:
                StampItems(items, sourceKey, source is FtpSourceItem);
                ApplySkippedFolderFilters(items, userExcludedFolders);
                _sourceItemsCache[sourceKey] = CloneItems(items);

                _currentSourceItems = items;
                RebuildGroupsFromCurrentItems();

                if (Groups.Count > 0)
                {
                    await LoadThumbnailsAsync(Groups.ToList(), source, sourceLabel);
                    _sourceItemsCache[sourceKey] = CloneItems(_currentSourceItems);
                }
                else
                {
                    StatusMessage = $"Found 0 group(s) from {sourceLabel}.";
                }

                MaybeShowSkippedFoldersScanReport(sourceLabel, ftpListingFailures, userExcludedFolders);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Source scan was canceled for {SourceLabel}.", sourceLabel);
                StatusMessage = $"FTP scan was canceled while scanning {sourceLabel}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning source {SourceLabel}.", sourceLabel);
                StatusMessage = $"Error scanning {sourceLabel}: {ex.Message}";
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }

        private void SetAllShootsSelected(bool isSelected)
        {
            _isUpdatingSelectAll = true;
            try
            {
                foreach (var group in Groups)
                {
                    group.IsSelected = isSelected;
                }

                _selectAll = isSelected;
                OnPropertyChanged(nameof(SelectAll));
            }
            finally
            {
                _isUpdatingSelectAll = false;
            }
        }

        private void Group_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemGroup.IsSelected))
            {
                if (_isUpdatingSelectAll) return;
                UpdateSelectAllFromGroups();
            }

            if (e.PropertyName == nameof(ItemGroup.KeywordsText))
            {
                RefreshImportReadinessSummary();
            }

            if (e.PropertyName == nameof(ItemGroup.IsExpanded))
            {
                if (_isBulkUpdatingGroupExpansion)
                {
                    return;
                }

                bool allExpanded = Groups.Count > 0 && Groups.All(g => g.IsExpanded);
                if (AllGroupsExpanded != allExpanded)
                {
                    AllGroupsExpanded = allExpanded;
                }
            }
        }

        private void UpdateSelectAllFromGroups()
        {
            if (Groups.Count == 0)
            {
                return;
            }

            bool allSelected = Groups.All(g => g.IsSelected);
            if (_selectAll == allSelected)
            {
                return;
            }

            _isUpdatingSelectAll = true;
            try
            {
                _selectAll = allSelected;
                OnPropertyChanged(nameof(SelectAll));
            }
            finally
            {
                _isUpdatingSelectAll = false;
            }
        }

        private void RebuildGroupsFromCurrentItems()
        {
            foreach (var existing in Groups)
            {
                existing.PropertyChanged -= Group_PropertyChanged;
            }

            DetachImportItemSelectionHandlers();

            Groups.Clear();
            EnsureFilteredItemsViewSource();


            if (_currentSourceItems.Count == 0)
            {
                return;
            }


            var groups = _groupBuilder.BuildGroups(_currentSourceItems, TimeSpan.FromHours(TimeBetweenShootsHours));


            foreach (var group in groups)
            {
                if (group.Items.Count == 0)
                {
                    continue;
                }

                group.AlbumName = AlbumName;
                group.FolderPath = group.Items[0].IsFtpSource
                    ? ExtractFtpFolderPath(group.Items[0].SourcePath)
                    : (Path.GetDirectoryName(group.Items[0].SourcePath) ?? string.Empty);
                group.SyncSelectionFromItems();
                ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
                foreach (var item in group.Items)
                {
                    string key = BuildItemKey(item);
                    if (item.Thumbnail == null && _thumbnailByItemKey.TryGetValue(key, out var cachedThumb))
                    {
                        item.Thumbnail = cachedThumb;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                    }
                }
                AttachImportItemSelectionHandlers(group);
                group.PropertyChanged += Group_PropertyChanged;
                Groups.Add(group);
            }


            UpdateSelectAllFromGroups();
            AllGroupsExpanded = Groups.Count > 0 && Groups.All(g => g.IsExpanded);
            ApplyFiltersToCurrentGroups();
            RefreshImportReadinessSummary();
            SyncActiveFilterChips();
            RefreshPreviewHealthSummary();
            StatusMessage = $"Updated folder separation to {TimeBetweenShootsHours} hour{(TimeBetweenShootsHours == 1 ? string.Empty : "s")}.";
        }

        private static readonly HashSet<string> RenderedPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".heic", ".heif", ".png", ".webp", ".tif", ".tiff"
        };

        private static readonly HashSet<string> RawPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dng", ".cr2", ".cr3", ".nef", ".arw", ".raf", ".orf", ".rw2", ".srw"
        };

        private static void ApplyPreviewStacks(List<ImportItem> items, bool expandPreviewStacks, bool groupRawAndRenderedPairs)
        {
            foreach (var item in items)
            {
                item.IsPreviewVisible = true;
                item.IsStackRepresentative = true;
                item.StackKey = item.SourcePath;
                item.PreviewLabel = item.FileName;
            }

            if (!groupRawAndRenderedPairs)
            {
                return;
            }

            var imageItems = items.Where(i => !i.IsVideo).ToList();
            var groups = imageItems.GroupBy(i => Path.GetFileNameWithoutExtension(i.FileName), StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                var members = group.ToList();
                if (members.Count <= 1)
                {
                    continue;
                }

                var rendered = members.Where(m => RenderedPreviewExtensions.Contains(Path.GetExtension(m.FileName))).ToList();
                var raws = members.Where(m => RawPreviewExtensions.Contains(Path.GetExtension(m.FileName))).ToList();
                if (rendered.Count == 0 || raws.Count == 0)
                {
                    continue;
                }

                var representative = rendered
                    .OrderBy(m => Path.GetExtension(m.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ? 0 :
                                  Path.GetExtension(m.FileName).Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ? 1 :
                                  Path.GetExtension(m.FileName).Equals(".heic", StringComparison.OrdinalIgnoreCase) ? 2 : 3)
                    .First();

                string stackKey = group.Key;
                int hiddenCount = members.Count - 1;
                foreach (var member in members)
                {
                    member.StackKey = stackKey;
                    member.IsStackRepresentative = ReferenceEquals(member, representative);
                    member.IsPreviewVisible = expandPreviewStacks || member.IsStackRepresentative;
                    member.PreviewLabel = member.IsStackRepresentative && hiddenCount > 0
                        ? $"{member.FileName} (+{hiddenCount})"
                        : member.FileName;
                }
            }
        }

        private static void SyncStackSelections(IEnumerable<ItemGroup> groups)
        {
            foreach (var group in groups)
            {
                var stackGroups = group.Items
                    .GroupBy(i => i.StackKey, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1);

                foreach (var stack in stackGroups)
                {
                    var leader = stack.FirstOrDefault(i => i.IsStackRepresentative) ?? stack.First();
                    bool selected = leader.IsSelected;
                    foreach (var member in stack)
                    {
                        member.IsSelected = selected;
                    }
                }
            }
        }

        private static void ClearThumbnailDiskCache()
        {
            try
            {
                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "Thumbnails");
                if (Directory.Exists(cacheDir))
                {
                    Directory.Delete(cacheDir, true);
                }
            }
            catch
            {
                // Ignore cache purge failures.
            }
        }

        private void MaybeShowSkippedFoldersScanReport(string sourceLabel, HashSet<string> ftpListingFailures, HashSet<string> userExcludedFolders)
        {
            int ftpCount = ftpListingFailures.Count;
            int excludedCount = userExcludedFolders.Count;
            if (ftpCount == 0 && excludedCount == 0)
            {
                return;
            }

            if (ftpCount == 0 && excludedCount > 0 && SuppressExcludedFolderScanReminders)
            {
                StatusMessage = AppLocalizer.Format("Vm_Status_ScanExcludedFoldersOnlySummary", excludedCount);
                return;
            }

            var ftpOrdered = ftpListingFailures.OrderBy(s => s).ToList();
            var excludedOrdered = userExcludedFolders.OrderBy(s => s).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            ShowSkippedFoldersSuppressReminderOption = ftpCount == 0 && excludedCount > 0;

            const int maxToShow = 15;
            static string FormatList(IReadOnlyList<string> lines, int max)
            {
                var shown = lines.Take(max).ToList();
                string tail = lines.Count > max ? $"\n...and {lines.Count - max} more." : string.Empty;
                return string.Join("\n", shown) + tail;
            }

            var sections = new List<string>();
            if (excludedOrdered.Count > 0)
            {
                sections.Add(AppLocalizer.Format("Vm_SkippedScan_SectionExcluded", FormatList(excludedOrdered, maxToShow)));
            }

            if (ftpOrdered.Count > 0)
            {
                sections.Add(AppLocalizer.Format("Vm_SkippedScan_SectionFtp", FormatList(ftpOrdered, maxToShow)));
                sections.Add(AppLocalizer.Get("Vm_SkippedScan_FtpTip"));
            }

            string message = string.Join("\n\n", sections.Where(s => !string.IsNullOrWhiteSpace(s)));

            string title;
            if (ftpOrdered.Count > 0 && excludedOrdered.Count > 0)
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleCombined", sourceLabel);
            }
            else if (ftpOrdered.Count > 0)
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleFtpOnly", ftpOrdered.Count);
            }
            else
            {
                title = AppLocalizer.Format("Vm_SkippedScan_TitleExcludedOnly", excludedOrdered.Count);
            }

            StatusMessage = ftpOrdered.Count > 0
                ? AppLocalizer.Format("Vm_Status_ScanSummaryWithFtpIssues", excludedOrdered.Count, ftpOrdered.Count)
                : AppLocalizer.Format("Vm_Status_ScanSummaryExcludedOnly", excludedOrdered.Count);

            SkippedFoldersReportTitle = title;
            SkippedFoldersReportText = message;
            ShowSkippedFoldersDialog = true;
        }

        // Ensures the filtered items view source is set up and refreshed for filtering/search
        private void EnsureFilteredItemsViewSource()
        {
            // Build a flat list of all ImportItems from all groups
            var allItems = Groups.SelectMany(g => g.Items).ToList();

            // Update AvailableFileTypes
            var fileTypes = allItems.Select(i => i.FileType).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();
            AvailableFileTypes.Clear();
            AvailableFileTypes.Add(string.Empty); // (All)
            AvailableFileTypes.Add(FilterTypeAllMedia);
            AvailableFileTypes.Add(FilterTypeImages);
            AvailableFileTypes.Add(FilterTypeVideos);
            AvailableFileTypes.Add(FilterTypeRaw);
            AvailableFileTypes.Add(FilterTypeJpeg);
            foreach (var t in fileTypes)
                AvailableFileTypes.Add(t);

            // Set up the CollectionView for filtering
            var cvs = System.Windows.Data.CollectionViewSource.GetDefaultView(allItems);
            cvs.Filter = o =>
            {
                if (o is not ImportItem item) return false;
                // Date filter
                if (FilterStartDate.HasValue && item.DateTaken < FilterStartDate.Value.Date)
                    return false;
                if (FilterEndDate.HasValue && item.DateTaken > FilterEndDate.Value.Date.AddDays(1).AddTicks(-1))
                    return false;
                // File type filter
                if (!MatchesFileTypeFilter(item, FilterFileType))
                    return false;
                // Keyword filter (filename)
                if (!string.IsNullOrWhiteSpace(FilterKeyword) && !item.FileName.Contains(FilterKeyword, StringComparison.OrdinalIgnoreCase))
                    return false;
                return true;
            };
            FilteredItemsView = cvs;
            cvs.Refresh();
        }
        partial void OnFilterStartDateChanged(DateTime? value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterEndDateChanged(DateTime? value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterFileTypeChanged(string value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }
        partial void OnFilterKeywordChanged(string value)
        {
            EnsureFilteredItemsViewSource();
            ApplyFiltersToCurrentGroups();
            SyncActiveFilterChips();
        }

        private void ApplyFiltersToCurrentGroups()
        {
            foreach (var group in Groups)
            {
                ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
                foreach (var item in group.Items)
                {
                    bool visible = true;
                    if (FilterStartDate.HasValue && item.DateTaken < FilterStartDate.Value.Date)
                    {
                        visible = false;
                    }
                    if (visible && FilterEndDate.HasValue && item.DateTaken > FilterEndDate.Value.Date.AddDays(1).AddTicks(-1))
                    {
                        visible = false;
                    }
                    if (visible && !MatchesFileTypeFilter(item, FilterFileType))
                    {
                        visible = false;
                    }
                    if (visible && !string.IsNullOrWhiteSpace(FilterKeyword) && !item.FileName.Contains(FilterKeyword, StringComparison.OrdinalIgnoreCase))
                    {
                        visible = false;
                    }

                    item.IsPreviewVisible = item.IsPreviewVisible && visible;
                }
            }

            RefreshPreviewHealthSummary();
        }

        private static bool MatchesFileTypeFilter(ImportItem item, string selectedFilter)
        {
            if (string.IsNullOrWhiteSpace(selectedFilter))
            {
                return true;
            }

            string extension = item.FileType?.TrimStart('.').ToUpperInvariant() ?? string.Empty;
            switch (selectedFilter)
            {
                case FilterTypeAllMedia:
                    return true;
                case FilterTypeImages:
                    return !item.IsVideo;
                case FilterTypeVideos:
                    return item.IsVideo;
                case FilterTypeRaw:
                    return RawPreviewExtensions.Contains($".{extension.ToLowerInvariant()}");
                case FilterTypeJpeg:
                    return extension is "JPG" or "JPEG";
                default:
                    return string.Equals(item.FileType, selectedFilter, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void AddNotificationFeedEntry(string? message, bool useSuccessAccent = false)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            void InsertEntry()
            {
                string line = $"{DateTime.Now:HH:mm:ss} - {message.Trim()}";
                if (NotificationFeed.Count > 0 &&
                    string.Equals(NotificationFeed[0].DisplayText, line, StringComparison.Ordinal))
                {
                    return;
                }

                NotificationFeed.Insert(0, new NotificationFeedLine
                {
                    DisplayText = line,
                    UseSuccessAccent = useSuccessAccent
                });
                const int maxFeedEntries = 200;
                while (NotificationFeed.Count > maxFeedEntries)
                {
                    NotificationFeed.RemoveAt(NotificationFeed.Count - 1);
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null)
            {
                return;
            }

            if (dispatcher.CheckAccess())
            {
                InsertEntry();
            }
            else
            {
                dispatcher.Invoke(InsertEntry);
            }
        }

        private void ExecuteCopySkippedFoldersReport()
        {
            if (string.IsNullOrWhiteSpace(SkippedFoldersReportText))
            {
                return;
            }

            try
            {
                Clipboard.SetText(SkippedFoldersReportText);
                StatusMessage = AppLocalizer.Get("Vm_Status_SkippedFolderCopied");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to copy report: {ex.Message}";
            }
        }

        private void ExecuteCloseSkippedFoldersReport()
        {
            ShowSkippedFoldersDialog = false;
        }

        /// <summary>Escape / Cancel: closes the topmost in-app overlay. Does not cancel an in-progress import (only its dialog is tied to import completion).</summary>
        private void DismissTopOverlay()
        {
            if (ShowScanExclusionsPanel)
            {
                ShowScanExclusionsPanel = false;
                return;
            }

            if (ShowSettingsDialog)
            {
                ShowSettingsDialog = false;
                return;
            }

            if (ShowScanProgressDialog)
            {
                ShowScanProgressDialog = false;
                return;
            }

            if (ShowDriveSelectionDialog)
            {
                ShowDriveSelectionDialog = false;
                return;
            }

            if (ShowAddFtpDialog)
            {
                ShowAddFtpDialog = false;
                return;
            }

            if (ShowAboutDialog)
            {
                ShowAboutDialog = false;
                return;
            }

            if (ShowSkippedFoldersDialog)
            {
                ExecuteCloseSkippedFoldersReport();
            }
        }

        private void SyncActiveFilterChips()
        {
            ActiveFilterChips.Clear();
            if (!string.IsNullOrWhiteSpace(FilterKeyword))
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "keyword", Label = $"Keyword: {FilterKeyword}" });
            }

            if (!string.IsNullOrWhiteSpace(FilterFileType))
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "type", Label = $"Type: {FilterFileType}" });
            }

            if (FilterStartDate.HasValue)
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "start", Label = $"From: {FilterStartDate.Value:yyyy-MM-dd}" });
            }

            if (FilterEndDate.HasValue)
            {
                ActiveFilterChips.Add(new FilterChipViewModel { Id = "end", Label = $"To: {FilterEndDate.Value:yyyy-MM-dd}" });
            }
        }

        [RelayCommand]
        private void RemoveFilterChip(string? chipId)
        {
            switch (chipId)
            {
                case "keyword":
                    FilterKeyword = string.Empty;
                    break;
                case "type":
                    FilterFileType = string.Empty;
                    break;
                case "start":
                    FilterStartDate = null;
                    break;
                case "end":
                    FilterEndDate = null;
                    break;
                default:
                    return;
            }
        }

        private void RefreshPreviewHealthSummary()
        {
            try
            {
                int loaded = 0;
                int failed = 0;
                int missing = 0;

                foreach (var group in Groups)
                {
                    foreach (var item in group.Items.Where(i => i.IsPreviewVisible))
                    {
                        switch (item.ThumbnailPreviewStatus)
                        {
                            case ThumbnailPreviewStatus.Loaded:
                                loaded++;
                                break;
                            case ThumbnailPreviewStatus.Failed:
                                failed++;
                                break;
                            default:
                                missing++;
                                break;
                        }
                    }
                }

                PreviewHealthSummary = AppLocalizer.Format("Vm_PreviewHealth", loaded, failed, missing);
            }
            catch
            {
                PreviewHealthSummary = string.Empty;
            }
        }

        private void RefreshImportReadinessSummary()
        {
            try
            {
                int selected = Groups.Sum(g => g.Items.Count(i => i.IsSelected));
                string dest = string.IsNullOrWhiteSpace(DestinationRoot) ? "(not set)" : DestinationRoot;
                int shootsWithKeywords = 0;
                foreach (var g in Groups)
                {
                    if (!g.Items.Any(i => i.IsSelected))
                    {
                        continue;
                    }

                    if (KeywordInputParser.Parse(g.KeywordsText).Count > 0)
                    {
                        shootsWithKeywords++;
                    }
                }

                string kw = !EmbedKeywordsOnImport
                    ? AppLocalizer.Get("Vm_Readiness_KwOff")
                    : AppLocalizer.Format("Vm_Readiness_KwOn", shootsWithKeywords);

                ImportReadinessSummary = AppLocalizer.Format(
                    "Vm_Readiness_Line",
                    selected,
                    dest,
                    DuplicatePolicy,
                    VerificationMode,
                    DeleteAfterImport ? AppLocalizer.Get("Vm_Yes") : AppLocalizer.Get("Vm_No"),
                    kw);

                SelectedFilesStatusLine = $"Files selected: {selected}";
                DestinationStatusLine = $"Save destination: {dest}";
                DeleteAfterImportStatusLine = $"Delete after import: {(DeleteAfterImport ? "On" : "Off")}";
                KeywordsStatusLine = $"Keywords: {kw}";
            }
            catch
            {
                ImportReadinessSummary = string.Empty;
                SelectedFilesStatusLine = string.Empty;
                DestinationStatusLine = string.Empty;
                DeleteAfterImportStatusLine = string.Empty;
                KeywordsStatusLine = string.Empty;
            }
        }

        private void ImportItem_SelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ImportItem.IsSelected))
            {
                RefreshImportReadinessSummary();
            }
        }

        private void DetachImportItemSelectionHandlers()
        {
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.PropertyChanged -= ImportItem_SelectionChanged;
                }
            }
        }

        private void AttachImportItemSelectionHandlers(ItemGroup group)
        {
            foreach (var item in group.Items)
            {
                item.PropertyChanged -= ImportItem_SelectionChanged;
                item.PropertyChanged += ImportItem_SelectionChanged;
            }
        }

        private string BuildImportConfirmationMessage(List<ItemGroup> selectedGroups, int totalFiles)
        {
            long bytes = selectedGroups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => Math.Max(0, i.FileSize));
            string mb = (bytes / (1024d * 1024d)).ToString("0.00", CultureInfo.CurrentCulture);

            var sb = new StringBuilder();
            sb.AppendLine(AppLocalizer.Format("Vm_ConfirmImport_Line1", totalFiles, mb));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_Destination"));
            sb.AppendLine(DestinationRoot);
            sb.AppendLine();
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DupPolicy")).Append(' ').AppendLine(DuplicatePolicy);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_Verify")).Append(' ').AppendLine(VerificationMode);
            sb.Append(AppLocalizer.Get("Vm_ConfirmImport_DeleteAfter")).Append(' ')
                .AppendLine(DeleteAfterImport ? AppLocalizer.Get("Vm_Yes") : AppLocalizer.Get("Vm_No"));
            sb.AppendLine();
            sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_KeywordsHeader"));
            if (!EmbedKeywordsOnImport)
            {
                sb.AppendLine(AppLocalizer.Get("Vm_Readiness_KwOff"));
            }
            else
            {
                bool any = false;
                foreach (ItemGroup g in selectedGroups.OrderBy(x => x.Title))
                {
                    List<string> list = KeywordInputParser.Parse(g.KeywordsText);
                    if (list.Count == 0)
                    {
                        continue;
                    }

                    any = true;
                    sb.AppendLine(AppLocalizer.Format("Vm_Confirm_ShootKeywords", g.Title, string.Join(", ", list)));
                }

                if (!any)
                {
                    sb.AppendLine(AppLocalizer.Get("Vm_ConfirmImport_NoKeywords"));
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void RepopulateLanguageOptions()
        {
            UiLanguageOptions.Clear();
            UiLanguageOptions.Add(new LanguageOption("", AppLocalizer.Get("Lang_UseSystem")));
            UiLanguageOptions.Add(new LanguageOption("en", AppLocalizer.Get("Lang_English")));
            UiLanguageOptions.Add(new LanguageOption("fr", AppLocalizer.Get("Lang_French")));
            UiLanguageOptions.Add(new LanguageOption("es", AppLocalizer.Get("Lang_Spanish")));
        }

        [RelayCommand]
        private async Task RetryFailedPreviewLoadsAsync()
        {
            if (Groups.Count == 0 || SelectedSource == null)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NothingToRetry");
                return;
            }

            var failedItems = Groups.SelectMany(g => g.Items).Where(i => i.ThumbnailPreviewStatus == ThumbnailPreviewStatus.Failed).ToList();
            if (failedItems.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NoFailedPreviews");
                return;
            }

            StatusMessage = $"Retrying {failedItems.Count} preview(s)...";
            await Task.Run(() =>
            {
                Parallel.ForEach(failedItems, new ParallelOptions { MaxDegreeOfParallelism = GetThumbnailWorkerCount() }, item =>
                {
                    string key = BuildItemKey(item);
                    object? thumb = null;
                    try
                    {
                        thumb = _thumbnailService.GetThumbnail(item.SourcePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Retry thumbnail failed for {Path}.", item.SourcePath);
                    }

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (thumb != null)
                        {
                            item.Thumbnail = thumb;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            _thumbnailByItemKey[key] = thumb;
                        }
                        else
                        {
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                        }
                    });
                });
            });

            await Application.Current.Dispatcher.InvokeAsync(RefreshPreviewHealthSummary);
            StatusMessage = AppLocalizer.Get("Vm_Status_PreviewRetryFinished");
        }

        [RelayCommand]
        private async Task ClearThumbnailCacheAndReloadPreviewsAsync()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_LoadSourceBeforeClearPreviewCache");
                return;
            }

            try
            {
                _thumbnailByItemKey.Clear();
                ClearThumbnailDiskCache();
                foreach (var group in Groups)
                {
                    foreach (var item in group.Items)
                    {
                        item.Thumbnail = null;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Unknown;
                    }
                }

                string label = GetThumbnailSourceLabel();
                StatusMessage = AppLocalizer.Get("Vm_Status_ThumbnailCacheClearedReloading");
                await LoadThumbnailsAsync(Groups.ToList(), SelectedSource, label);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to reload previews: {ex.Message}";
            }
        }

        private string GetThumbnailSourceLabel()
        {
            return SelectedSource switch
            {
                FtpSourceItem ftp => $"{ftp.Host}{NormalizeFtpPath(ftp.RemoteFolder)}",
                UnifiedSourceItem => "Unified",
                _ => SelectedSource?.ToString() ?? "source"
            };
        }

        [RelayCommand]
        private void ToggleShootStackExpand(ItemGroup? group)
        {
            if (group == null || !GroupRawAndRenderedPairs)
            {
                return;
            }

            group.ExpandStackedPairsInShoot = !group.ExpandStackedPairsInShoot;
            ApplyPreviewStacks(group.Items, ExpandPreviewStacks || group.ExpandStackedPairsInShoot, GroupRawAndRenderedPairs);
            ApplyFiltersToCurrentGroups();
        }

        [RelayCommand]
        private void ToggleGroupExpanded(ItemGroup? group)
        {
            if (group == null)
            {
                return;
            }

            group.IsExpanded = !group.IsExpanded;
        }

        [RelayCommand]
        private void ExpandAllGroups()
        {
            foreach (var group in Groups)
            {
                group.IsExpanded = true;
            }
        }

        [RelayCommand]
        private void CollapseAllGroups()
        {
            foreach (var group in Groups)
            {
                group.IsExpanded = false;
            }
        }

        private async Task LoadThumbnailsAsync(List<ItemGroup> groups, object source, string sourceLabel)
        {
            if (source is UnifiedSourceItem)
            {
                await LoadUnifiedThumbnailsAsync(groups, sourceLabel);
                return;
            }

            if (source is FtpSourceItem ftp)
            {
                await LoadFtpThumbnailsAsync(groups, ftp, sourceLabel, preferBackgroundBatch: false);
                return;
            }

            await Task.Run(() =>
            {
                var allItems = groups.SelectMany(g => g.Items).ToList();
                int total = allItems.Count;

                if (total == 0)
                {
                    return;
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    TotalFilesToScan = total;
                    ScanProgressPercent = 0;
                    ScanProgressMessage = $"Loading previews: 0/{total}";
                });

                int current = 0;

                int workers = GetThumbnailWorkerCount();
                // Use CPU-aware parallelism for local thumbnail decode.
                Parallel.ForEach(allItems, new ParallelOptions { MaxDegreeOfParallelism = workers }, item =>
                {
                    string itemKey = BuildItemKey(item);
                    if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                    {
                        int cCached = Interlocked.Increment(ref current);
                        Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.Thumbnail = cachedThumb;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            ScannedFiles = cCached;
                            ScanProgressPercent = total > 0 ? (cCached * 100) / total : 0;
                            ScanProgressMessage = $"Loading previews: {cCached}/{total}";
                        });
                        return;
                    }

                    object? thumb = null;
                    try
                    {
                        thumb = _thumbnailService.GetThumbnail(item.SourcePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Thumbnail generation failed for local item {SourcePath}.", item.SourcePath);
                    }
                    int c = Interlocked.Increment(ref current);

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (thumb != null)
                        {
                            item.Thumbnail = thumb;
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            _thumbnailByItemKey[itemKey] = thumb;
                        }
                        else
                        {
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                        }
                        ScannedFiles = c;
                        ScanProgressPercent = total > 0 ? (c * 100) / total : 0;
                        ScanProgressMessage = $"Loading previews: {c}/{total}";
                    });
                });

                Application.Current.Dispatcher.Invoke(RefreshPreviewHealthSummary);
            });

            StatusMessage = $"Scanning {sourceLabel} complete. Loaded previews automatically.";
            FtpThumbnailPhaseDetail = string.Empty;
        }

        private async Task LoadFtpThumbnailsAsync(List<ItemGroup> groups, FtpSourceItem ftp, string sourceLabel, bool preferBackgroundBatch = true)
        {
            var allItems = groups
                .SelectMany(g => g.Items)
                .ToList();

            if (allItems.Count == 0)
            {
                StatusMessage = $"Scanning {sourceLabel} complete. No previewable FTP images found.";
                return;
            }

            int total = allItems.Count;
            int initialCount = preferBackgroundBatch && LimitFtpThumbnailLoad ? Math.Min(FtpInitialThumbnailCount, total) : total;

            if (initialCount <= 0)
            {
                initialCount = total;
            }

            var initialItems = allItems.Take(initialCount).ToList();
            var remainingItems = allItems.Skip(initialCount).ToList();

            int loadedInitial = await LoadFtpThumbnailBatchAsync(initialItems, ftp, total, 0, true);

            if (remainingItems.Count == 0)
            {
                StatusMessage = $"Scanning {sourceLabel} complete. Loaded FTP previews automatically {loadedInitial}/{total}.";
                return;
            }

            StatusMessage = $"Loaded first {initialCount}/{total} FTP previews. Loading remaining previews in background...";

            _ = Task.Run(async () =>
            {
                int loadedRemaining = await LoadFtpThumbnailBatchAsync(remainingItems, ftp, total, initialCount, false);
                int loadedTotal = loadedInitial + loadedRemaining;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = $"Background FTP preview loading complete. Loaded {loadedTotal}/{total} previews.";
                });
            });
        }

        private async Task<int> LoadFtpThumbnailBatchAsync(
            List<ImportItem> items,
            FtpSourceItem ftp,
            int totalItemCount,
            int startIndex,
            bool updateScanProgressMessage)
        {
            if (items.Count == 0)
            {
                return 0;
            }

            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

            int loadedCount = 0;
            int skippedCount = 0;
            int processedCount = 0;
            int workerCount = GetFtpThumbnailWorkerCount();

            var indexedItems = items.Select((item, index) => (item, index)).ToList();
            await Parallel.ForEachAsync(
                indexedItems,
                new ParallelOptions { MaxDegreeOfParallelism = workerCount },
                async (entry, _) =>
                {
                    var item = entry.item;
                    int overallIndex = startIndex + entry.index + 1;
                    string itemKey = BuildItemKey(item);

                    string ext = Path.GetExtension(item.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".jpg";
                    }

                    string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                    try
                    {
                        if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                        {
                            Interlocked.Increment(ref loadedCount);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                item.Thumbnail = cachedThumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                            });
                        }
                        else
                        {
                            int timeoutSeconds = IsLikelyVideoPath(item.FileName) ? 120 : 30;
                            bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, timeoutSeconds);
                            if (!downloaded)
                            {
                                Interlocked.Increment(ref skippedCount);
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                                });
                            }
                            else
                            {
                                var thumb = await Task.Run(() => _thumbnailService.GetThumbnail(tempPath));
                                if (thumb != null)
                                {
                                    Interlocked.Increment(ref loadedCount);
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        item.Thumbnail = thumb;
                                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                        _thumbnailByItemKey[itemKey] = thumb;
                                    });
                                }
                                else
                                {
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref skippedCount);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                        });
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                        }
                        catch
                        {
                            // Ignore temp cleanup failures.
                        }
                    }

                    int processed = Interlocked.Increment(ref processedCount);
                    if (processed == 1 || processed % 10 == 0 || processed == items.Count)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ScannedFiles = startIndex + processed;
                            TotalFilesToScan = totalItemCount;
                            ScanProgressPercent = totalItemCount > 0 ? ((startIndex + processed) * 100) / totalItemCount : 0;
                            CurrentScanFolder = item.SourcePath;
                            CurrentScanFolderProcessedFiles = processed;
                            CurrentScanFolderTotalFiles = items.Count;
                            if (updateScanProgressMessage)
                            {
                                ScanProgressMessage = $"Loading FTP previews: {Math.Min(totalItemCount, startIndex + processed)}/{totalItemCount}";
                            }
                        });
                    }
                });

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (skippedCount > 0)
                {
                    ScanProgressMessage = $"Loading FTP previews: {Math.Min(totalItemCount, startIndex + items.Count)}/{totalItemCount} (skipped {skippedCount} stalled/invalid file(s))";
                }

                FtpThumbnailPhaseDetail = $"FTP previews: loaded {loadedCount}/{items.Count} in batch · skipped {skippedCount}";
                RefreshPreviewHealthSummary();
            });

            return loadedCount;
        }

        private static async Task<bool> DownloadFtpFileWithTimeoutAsync(
            FtpSourceItem ftp,
            string remotePath,
            string localPath,
            int timeoutSeconds)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));
            try
            {
                await Task.Run(() => DownloadFtpFileSync(ftp, remotePath, localPath, timeout.Token), timeout.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsLikelyVideoPath(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mov", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".m4v", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".avi", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".wmv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".3gp", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".mts", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".m2ts", StringComparison.OrdinalIgnoreCase);
        }

        private static void DownloadFtpFileSync(FtpSourceItem ftp, string remotePath, string localPath, CancellationToken cancellationToken)
        {
            Uri uri = BuildFtpFileUri(ftp.Host, ftp.Port, remotePath);

#pragma warning disable SYSLIB0014
            var request = (FtpWebRequest)WebRequest.Create(uri);
#pragma warning restore SYSLIB0014
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(ftp.User, ftp.Pass);
            request.UseBinary = true;
            request.UsePassive = true;
            request.KeepAlive = false;
            request.Timeout = 30000;
            request.ReadWriteTimeout = 30000;

            using var response = (FtpWebResponse)request.GetResponse();
            using var source = response.GetResponseStream();
            using var dest = File.Create(localPath);

            if (source == null)
            {
                return;
            }

            byte[] buffer = new byte[65536];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                dest.Write(buffer, 0, read);
            }
        }

        private static Uri BuildFtpFileUri(string host, int port, string remotePath)
        {
            string normalized = NormalizeFtpPath(remotePath);
            string encodedPath = string.Join("/", normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));

            string uriText = string.IsNullOrEmpty(encodedPath)
                ? $"ftp://{host}:{port}/"
                : $"ftp://{host}:{port}/{encodedPath}";

            return new Uri(uriText);
        }

        [RelayCommand]
        private async Task RefreshUnified()
        {
            await LoadUnifiedSourceItemsAsync(forceRefresh: true);
        }

        private async Task LoadUnifiedSourceItemsAsync(bool forceRefresh = false)
        {
            var ftpListingFailures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var userExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var concreteSources = Sources
                .Where(s => s is string || s is FtpSourceItem)
                .ToList();

            if (concreteSources.Count == 0)
            {
                _logger.LogInformation("Unified load skipped: no drive or FTP sources in the sidebar. Add sources or enable fixed drives in drive selection.");
                _currentSourceItems = new List<ImportItem>();
                StatusMessage = AppLocalizer.Get("Vm_Status_NoSourcesForUnified");
                return;
            }

            _logger.LogInformation(
                "Unified load starting: {SourceCount} sources: {SourceSummary}. Sidebar uses removable drives by default; enable fixed drives in drive selection to merge them here.",
                concreteSources.Count,
                string.Join(", ", concreteSources.Select(s => s.ToString() ?? "")));

            try
            {
                ShowScanProgressDialog = true;
                ScanDialogTitle = forceRefresh ? "Refreshing Unified Import List..." : "Loading Unified Import List...";
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = concreteSources.Count;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = "Merging SD and FTP sources...";
                CurrentScanFolder = "/";
                CurrentScanFolderProcessedFiles = 0;
                CurrentScanFolderTotalFiles = 0;

                int sourcesDone = 0;
                int totalSources = concreteSources.Count;

                async Task<List<ImportItem>> ScanOneUnifiedSourceAsync(object src)
                {
                    List<ImportItem> sourceItems;

                    if (src is string drive)
                    {
                        string localPath = drive;
                        string localKey = BuildSourceKey(localPath);

                        if (!forceRefresh && _sourceItemsCache.TryGetValue(localKey, out var cachedLocal))
                        {
                            sourceItems = CloneItems(cachedLocal);
                        }
                        else
                        {
                            if (!Directory.Exists(localPath))
                            {
                                sourceItems = new List<ImportItem>();
                            }
                            else
                            {
                                sourceItems = await Task.Run(() => _scanner.Scan(localPath, ScanIncludeSubfolders)).ConfigureAwait(false);
                            }

                            StampItems(sourceItems, localKey, false);
                            _sourceItemsCache[localKey] = CloneItems(sourceItems);
                        }
                    }
                    else if (src is FtpSourceItem ftp)
                    {
                        string ftpKey = BuildSourceKey(ftp);

                        if (!forceRefresh && _sourceItemsCache.TryGetValue(ftpKey, out var cachedFtp))
                        {
                            sourceItems = CloneItems(cachedFtp);
                        }
                        else
                        {
                            sourceItems = await _ftpScanner.ScanAsync(
                                ftp.Host,
                                ftp.Port,
                                ftp.User,
                                ftp.Pass,
                                NormalizeFtpPath(ftp.RemoteFolder),
                                ScanIncludeSubfolders,
                                120,
                                CancellationToken.None,
                                progress =>
                                {
                                    if (!string.IsNullOrWhiteSpace(progress.Note) && progress.Note.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                                    {
                                        lock (ftpListingFailures)
                                        {
                                            ftpListingFailures.Add($"{progress.CurrentFolder} - {progress.Note}");
                                        }
                                    }
                                }).ConfigureAwait(false);

                            StampItems(sourceItems, ftpKey, true);
                            _sourceItemsCache[ftpKey] = CloneItems(sourceItems);
                        }
                    }
                    else
                    {
                        sourceItems = new List<ImportItem>();
                    }

                    int done = Interlocked.Increment(ref sourcesDone);
                    await Application.Current.Dispatcher
                        .InvokeAsync(() =>
                        {
                            ScannedFolders = Math.Max(ScannedFolders, done);
                            int sh = ScannedFolders;
                            ScanProgressPercent = totalSources > 0 ? (sh * 100) / totalSources : 0;
                            ScanProgressMessage = $"Merged {sh}/{totalSources} sources...";
                        })
                        .Task
                        .ConfigureAwait(false);

                    return sourceItems;
                }

                List<ImportItem>[] parallelResults = await Task.WhenAll(concreteSources.Select(src => ScanOneUnifiedSourceAsync(src))).ConfigureAwait(false);

                var unifiedItems = parallelResults.SelectMany(r => r).ToList();

                ApplySkippedFolderFilters(unifiedItems, userExcludedFolders);

                _currentSourceItems = unifiedItems;

                // Parallel scans finish on thread pool; Groups / ObservableCollections must update on UI thread only.
                List<ItemGroup> groupsForThumbnails = await Application.Current.Dispatcher
                    .InvokeAsync(() =>
                    {
                        ScannedFiles = unifiedItems.Count;
                        TotalFilesToScan = unifiedItems.Count;
                        CurrentScanFolderProcessedFiles = unifiedItems.Count;
                        CurrentScanFolderTotalFiles = unifiedItems.Count;
                        RebuildGroupsFromCurrentItems();
                        ShowScanProgressDialog = false;
                        return Groups.ToList();
                    })
                    .Task
                    .ConfigureAwait(false);

                if (groupsForThumbnails.Count > 0)
                {
                    await LoadThumbnailsAsync(groupsForThumbnails, _unifiedSource, "Unified");
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusMessage = AppLocalizer.Get("Vm_Status_UnifiedNoMedia");
                    });
                }

                if (ftpListingFailures.Count > 0 || userExcludedFolders.Count > 0)
                {
                    Application.Current.Dispatcher.Invoke(() => MaybeShowSkippedFoldersScanReport("Unified", ftpListingFailures, userExcludedFolders));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unified source load failed.");
                StatusMessage = $"Error loading Unified sources: {ex.Message}";
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }

        private async Task LoadUnifiedThumbnailsAsync(List<ItemGroup> groups, string sourceLabel)
        {
            var allItems = groups
                .SelectMany(g => g.Items)
                .ToList();

            if (allItems.Count == 0)
            {
                StatusMessage = $"Scanning {sourceLabel} complete. No previewable images found.";
                return;
            }

            int total = allItems.Count;
            int processedAtomic = 0;

            await Application.Current.Dispatcher
                .InvokeAsync(() =>
                {
                    ScannedFiles = 0;
                    TotalFilesToScan = total;
                    ScanProgressPercent = 0;
                    ScanProgressMessage = $"Loading Unified previews: 0/{total}";
                })
                .Task
                .ConfigureAwait(false);

            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

            void BumpProgress()
            {
                int c = Interlocked.Increment(ref processedAtomic);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ScannedFiles = Math.Max(ScannedFiles, c);
                    int shown = ScannedFiles;
                    TotalFilesToScan = total;
                    ScanProgressPercent = total > 0 ? (shown * 100) / total : 0;
                    ScanProgressMessage = $"Loading Unified previews: {shown}/{total}";
                });
            }

            var needLocal = new List<ImportItem>();
            var needFtp = new List<ImportItem>();

            foreach (var item in allItems)
            {
                string itemKey = BuildItemKey(item);
                if (_thumbnailByItemKey.TryGetValue(itemKey, out var cachedThumb) && cachedThumb != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        item.Thumbnail = cachedThumb;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                    });
                    BumpProgress();
                }
                else if (item.IsFtpSource)
                {
                    needFtp.Add(item);
                }
                else
                {
                    needLocal.Add(item);
                }
            }

            int localWorkers = GetThumbnailWorkerCount();
            int ftpWorkers = GetFtpThumbnailWorkerCount();

            Task localTask = Task.Run(() =>
            {
                Parallel.ForEach(
                    needLocal,
                    new ParallelOptions { MaxDegreeOfParallelism = localWorkers },
                    item =>
                    {
                        string itemKey = BuildItemKey(item);
                        object? thumb = null;
                        try
                        {
                            thumb = _thumbnailService.GetThumbnail(item.SourcePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Unified thumbnail failed for local {Path}.", item.SourcePath);
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (thumb != null)
                            {
                                item.Thumbnail = thumb;
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                _thumbnailByItemKey[itemKey] = thumb;
                            }
                            else
                            {
                                item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                            }
                        });
                        BumpProgress();
                    });
            });

            Task ftpTask = Parallel.ForEachAsync(
                needFtp,
                new ParallelOptions { MaxDegreeOfParallelism = ftpWorkers },
                async (item, ct) =>
                {
                    string itemKey = BuildItemKey(item);
                    if (!ftpSourcesByKey.TryGetValue(item.SourceId, out var ftp))
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                        BumpProgress();
                        return;
                    }

                    string ext = Path.GetExtension(item.FileName);
                    if (string.IsNullOrWhiteSpace(ext))
                    {
                        ext = ".jpg";
                    }

                    string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                    try
                    {
                        int timeoutSeconds = IsLikelyVideoPath(item.FileName) ? 120 : 30;
                        bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, timeoutSeconds).ConfigureAwait(false);
                        if (downloaded)
                        {
                            object? thumb = await Task.Run(() => _thumbnailService.GetThumbnail(tempPath), ct).ConfigureAwait(false);
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (thumb != null)
                                {
                                    item.Thumbnail = thumb;
                                    item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                                    _thumbnailByItemKey[itemKey] = thumb;
                                }
                                else
                                {
                                    item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed;
                                }
                            });
                        }
                        else
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                        }
                    }
                    catch
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() => item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Failed);
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                            {
                                File.Delete(tempPath);
                            }
                        }
                        catch
                        {
                            // Ignore temp cleanup failures.
                        }
                    }

                    BumpProgress();
                });

            await Task.WhenAll(localTask, ftpTask).ConfigureAwait(false);

            Application.Current.Dispatcher.Invoke(RefreshPreviewHealthSummary);
            StatusMessage = $"Scanning {sourceLabel} complete. Loaded unified previews automatically.";
        }

        private static void StampItems(List<ImportItem> items, string sourceId, bool isFtp)
        {
            foreach (var item in items)
            {
                item.SourceId = sourceId;
                item.IsFtpSource = isFtp;
            }
        }

        private static List<ImportItem> CloneItems(List<ImportItem> items)
        {
            return items.Select(i => new ImportItem
            {
                SourcePath = i.SourcePath,
                SourceId = i.SourceId,
                IsFtpSource = i.IsFtpSource,
                FileName = i.FileName,
                FileSize = i.FileSize,
                DateTaken = i.DateTaken,
                IsVideo = i.IsVideo,
                FileType = i.FileType,
                IsSelected = i.IsSelected,
                Thumbnail = i.Thumbnail,
                IsPreviewVisible = i.IsPreviewVisible,
                PreviewLabel = i.PreviewLabel,
                StackKey = i.StackKey,
                IsStackRepresentative = i.IsStackRepresentative,
                ThumbnailPreviewStatus = i.ThumbnailPreviewStatus
            }).ToList();
        }

        private static string BuildItemKey(ImportItem item)
        {
            string sourceId = string.IsNullOrWhiteSpace(item.SourceId) ? "unknown" : item.SourceId;
            return $"{sourceId}|{item.SourcePath}";
        }

        private int GetThumbnailWorkerCount()
        {
            int cpu = Math.Max(2, Environment.ProcessorCount);
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => Math.Clamp(cpu, 6, 16),
                "Ultra" => Math.Clamp(cpu * 2, 12, 32),
                _ => Math.Clamp(Math.Max(3, cpu / 2), 3, 12)
            };
        }

        private int GetFtpThumbnailWorkerCount()
        {
            return ThumbnailPerformanceMode switch
            {
                "Low" => 2,
                "Max" => 8,
                "Ultra" => 16,
                _ => 4
            };
        }

        private static string BuildSourceKey(FtpSourceItem ftp)
        {
            return $"ftp|{ftp.Host}|{ftp.Port}|{NormalizeFtpPath(ftp.RemoteFolder)}";
        }

        private static string BuildSourceKey(string localPath)
        {
            return $"local|{localPath}";
        }

        private async void ExecuteBuildSelectedPreviews()
        {
            if (SelectedSource == null || Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_ScanSourceFirst");
                return;
            }

            var selectedGroups = Groups.Where(g => g.IsSelected).ToList();
            if (selectedGroups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SelectAtLeastOneGroup");
                return;
            }

            try
            {
                ShowScanProgressDialog = true;
                ScanDialogTitle = "Building Previews...";
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = selectedGroups.Count;
                ScannedFiles = 0;
                TotalFilesToScan = selectedGroups.SelectMany(g => g.Items).Count();
                ScanProgressMessage = $"Building previews for {selectedGroups.Count} selected folder(s)...";
                StatusMessage = AppLocalizer.Get("Vm_Status_BuildingPreviews");

                string sourceLabel = SelectedSource is FtpSourceItem ftpSource
                    ? $"{ftpSource.Host}{NormalizeFtpPath(ftpSource.RemoteFolder)}"
                    : SelectedSource.ToString() ?? "source";

                await LoadThumbnailsAsync(selectedGroups, SelectedSource, sourceLabel);
                ScannedFolders = TotalFoldersToScan;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Preview build failed: {ex.Message}";
            }
            finally
            {
                ShowScanProgressDialog = false;
            }
        }

        private async void ExecuteImport()
        {
            if (Groups.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_StatusNothingToImport");
                return;
            }

            SyncStackSelections(Groups);
            var selectedGroups = Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList();
            int totalFiles = selectedGroups.Sum(g => g.Items.Count(i => i.IsSelected));
            if (totalFiles == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_StatusNoFilesSelected");
                return;
            }

            if (ConfirmBeforeImport)
            {
                string confirmBody = BuildImportConfirmationMessage(selectedGroups, totalFiles);
                MessageBoxResult confirmResult = MessageBox.Show(
                    confirmBody,
                    AppLocalizer.Get("Vm_ConfirmImportTitle"),
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.OK)
                {
                    StatusMessage = AppLocalizer.Get("Vm_StatusImportCanceled");
                    return;
                }
            }

            IsImporting = true;
            SavePendingImportPlan(selectedGroups);
            _logger.LogInformation("Import started. SelectedGroups={GroupCount}, SelectedFiles={FileCount}", selectedGroups.Count, totalFiles);
            TotalFilesForImport = totalFiles;
            CurrentFileBeingImported = 0;
            ProcessedFilesForImport = 0;
            FailedFilesForImport = 0;
            FailedImportRecords.Clear();
            CurrentGroupFileBeingImported = 0;
            TotalFilesInCurrentGroup = 0;
            CurrentGroupProgressPercent = 0;
            CurrentImportGroupTitle = string.Empty;
            ImportElapsedText = "00:00:00";
            ImportEtaText = "--:--:--";
            ImportDataRateText = "-- MB/s";
            ShowImportProgressDialog = true;
            StatusMessage = AppLocalizer.Get("Vm_Status_StartingImport");
            ProgressPercent = 0;
            _processedBytesForImport = 0;

            _importStartedAtUtc = DateTime.UtcNow;
            using var importCts = new CancellationTokenSource();
            var stopwatch = Stopwatch.StartNew();
            var timerTask = Task.Run(async () =>
            {
                while (!importCts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(1000, importCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ImportElapsedText = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");

                        if (ProcessedFilesForImport > 0 && TotalFilesForImport > ProcessedFilesForImport)
                        {
                            double avgSecondsPerFile = stopwatch.Elapsed.TotalSeconds / ProcessedFilesForImport;
                            double remainingSeconds = (TotalFilesForImport - ProcessedFilesForImport) * avgSecondsPerFile;
                            ImportEtaText = TimeSpan.FromSeconds(Math.Max(0, remainingSeconds)).ToString(@"hh\:mm\:ss");
                        }
                        else if (TotalFilesForImport <= ProcessedFilesForImport && TotalFilesForImport > 0)
                        {
                            ImportEtaText = "00:00:00";
                        }

                        if (stopwatch.Elapsed.TotalSeconds > 0.25 && _processedBytesForImport > 0)
                        {
                            double bytesPerSecond = _processedBytesForImport / stopwatch.Elapsed.TotalSeconds;
                            ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
                        }
                    });
                }
            });

            try
            {

                if (SelectedSource is UnifiedSourceItem)
                {
                    await ExecuteUnifiedImportAsync(selectedGroups, importCts.Token);
                }
                else
                {
                    IFileProvider provider = _fileProviderFactory.CreateLocalProvider();
                    if (SelectedSource is FtpSourceItem ftp)
                    {
                        provider = _fileProviderFactory.CreateFtpProvider(ftp.Host, ftp.Port, ftp.User, ftp.Pass);
                    }
                    else if (SelectedSource is AdbSourceItem adb)
                    {
                        provider = _fileProviderFactory.CreateAdbProvider(adb.DeviceSerial);
                    }

                    try
                    {
                        var engine = _ingestEngineFactory.Create(provider);

                        engine.ProgressChanged += (percent, msg) =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusMessage = msg;
                            });
                        };

                        engine.ItemProcessed += progress =>
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProcessedFilesForImport++;
                                if (!progress.Success)
                                {
                                    FailedFilesForImport++;
                                    FailedImportRecords.Add(new FailedImportRecord
                                    {
                                        SourcePath = progress.SourcePath,
                                        FileName = progress.FileName,
                                        ErrorMessage = string.IsNullOrWhiteSpace(progress.ErrorMessage) ? "Import failed." : progress.ErrorMessage
                                    });
                                }
                                else
                                {
                                    _processedBytesForImport += Math.Max(0, progress.FileSizeBytes);
                                }

                                CurrentFileBeingImported = ProcessedFilesForImport - FailedFilesForImport;
                                CurrentGroupFileBeingImported = progress.GroupCurrent;
                                TotalFilesInCurrentGroup = progress.GroupTotal;
                                CurrentGroupProgressPercent = progress.GroupTotal > 0 ? (progress.GroupCurrent * 100) / progress.GroupTotal : 0;
                                CurrentImportGroupTitle = progress.GroupTitle;
                                ProgressPercent = TotalFilesForImport > 0 ? (ProcessedFilesForImport * 100) / TotalFilesForImport : 0;

                                string state = progress.Success ? "Copying" : "Failed";
                                StatusMessage = $"{state} {progress.FileName} | overall {ProcessedFilesForImport}/{TotalFilesForImport} | group {progress.GroupCurrent}/{progress.GroupTotal}";
                            });
                        };

                        await System.Threading.Tasks.Task.Run(async () =>
                        {
                            foreach (var group in selectedGroups)
                            {
                                await engine.IngestGroupAsync(group, DestinationRoot, NamingTemplate, importCts.Token, CreateIngestOptions(group));

                                if (DeleteAfterImport)
                                {
                                    foreach (var item in group.Items.Where(i => i.IsSelected))
                                    {
                                        try
                                        {
                                            await provider.DeleteAsync(item.SourcePath, importCts.Token);
                                        }
                                        catch
                                        {
                                            // Ignore source-delete failures so import completion is not blocked.
                                        }
                                    }
                                }
                            }
                        });
                    }
                    finally
                    {
                        if (provider is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
                        }
                    }
                }

                string completionMsg = FailedFilesForImport > 0
                    ? $"✓ Import completed with warnings. Imported {CurrentFileBeingImported}/{TotalFilesForImport}, failed {FailedFilesForImport}."
                    : $"✓ Import completed successfully! Imported {CurrentFileBeingImported}/{TotalFilesForImport}.";
                LastImportSummary =
                    $"Last import — succeeded {CurrentFileBeingImported}/{TotalFilesForImport}, failed {FailedFilesForImport}. " +
                    $"Destination: {DestinationRoot}. " +
                    $"Reports folder: \"_ImportReports\" under your destination.";
                ProgressPercent = 100;
                CurrentGroupProgressPercent = 100;
                ImportEtaText = "00:00:00";
                if (stopwatch.Elapsed.TotalSeconds > 0.25 && _processedBytesForImport > 0)
                {
                    double bytesPerSecond = _processedBytesForImport / stopwatch.Elapsed.TotalSeconds;
                    ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
                }

                _suppressStatusFeedFromStatusMessage = true;
                try
                {
                    StatusMessage = completionMsg;
                    AddNotificationFeedEntry(completionMsg, useSuccessAccent: true);
                }
                finally
                {
                    _suppressStatusFeedFromStatusMessage = false;
                }

                ShowWindowsImportCompletionNotification(CurrentFileBeingImported, TotalFilesForImport, FailedFilesForImport);

                SaveImportHistoryRecord(stopwatch.Elapsed);
                ExportImportReportArtifact(stopwatch.Elapsed, selectedGroups);

                // Brief beat so the completion state is visible (avoids ~1s dead air from the old 1000ms delay).
                await System.Threading.Tasks.Task.Delay(400);
                ShowImportProgressDialog = false;

                // --- Milestone 5: Post-import actions ---
                try
                {
                    // 1. Auto-open destination folder (non-blocking — Explorer startup does not extend import completion).
                    string destRoot = DestinationRoot;
                    if (!string.IsNullOrWhiteSpace(destRoot) && Directory.Exists(destRoot))
                    {
                        _ = Task.Run(() =>
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = destRoot,
                                    UseShellExecute = true
                                });
                            }
                            catch
                            {
                                /* ignore open errors */
                            }
                        });
                    }
                }
                catch { /* Ignore open errors */ }

                try
                {
                    // 2. Eject/unmount source device if local drive
                    if (SelectedSource is string drive && drive.Length >= 2 && drive[1] == ':')
                    {
                        // Use Windows Management Instrumentation (WMI) to eject
                        var driveLetter = drive.TrimEnd('\\');
                        var query = $"SELECT * FROM Win32_Volume WHERE DriveLetter = '{driveLetter}'";
                        using var searcher = new System.Management.ManagementObjectSearcher(query);
                        foreach (System.Management.ManagementObject volume in searcher.Get())
                        {
                            try { volume.InvokeMethod("Dismount", null); volume.InvokeMethod("Remove", null); } catch { }
                        }
                    }
                }
                catch { /* Ignore eject errors */ }

                try
                {
                    // 3. Export sidecar album JSON and .xmp for each imported group
                    foreach (var group in selectedGroups)
                    {
                        var selectedItems = group.Items.Where(i => i.IsSelected).ToList();
                        if (selectedItems.Count == 0) continue;
                        string folderName = _groupBuilder.GetTargetFolderName(group);
                        string targetDir = Path.Combine(DestinationRoot, folderName);
                        if (!Directory.Exists(targetDir)) continue;
                        var album = new {
                            GroupTitle = group.Title,
                            StartDate = group.StartDate,
                            EndDate = group.EndDate,
                            Items = selectedItems.Select(i => new {
                                i.FileName,
                                i.SourcePath,
                                i.FileSize,
                                i.DateTaken
                            }).ToList()
                        };
                        string json = System.Text.Json.JsonSerializer.Serialize(album, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(Path.Combine(targetDir, "album.json"), json);

                        // Legacy Lightroom sidecar metadata (skipped when keyword embedding is enabled — keywords are written during copy).
                        if (!EmbedKeywordsOnImport)
                        {
                            foreach (var item in selectedItems)
                            {
                            string xmpPath = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(item.FileName) + ".xmp");
                            string xmp = $@"<?xpacket begin='﻿' id='W5M0MpCehiHzreSzNTczkc9d'?>
<x:xmpmeta xmlns:x='adobe:ns:meta/'>
  <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
    <rdf:Description rdf:about=''
      xmlns:dc='http://purl.org/dc/elements/1.1/'
      xmlns:xmp='http://ns.adobe.com/xap/1.0/'
      xmlns:photoshop='http://ns.adobe.com/photoshop/1.0/'>
      <dc:title><rdf:Alt><rdf:li xml:lang='x-default'>{System.Security.SecurityElement.Escape(group.Title)}</rdf:li></rdf:Alt></dc:title>
      <xmp:CreateDate>{item.DateTaken:yyyy-MM-ddTHH:mm:ssZ}</xmp:CreateDate>
      <photoshop:DateCreated>{item.DateTaken:yyyy-MM-ddTHH:mm:ssZ}</photoshop:DateCreated>
      <dc:format>{System.Security.SecurityElement.Escape(item.FileType)}</dc:format>
      <dc:identifier>{System.Security.SecurityElement.Escape(item.FileName)}</dc:identifier>
    </rdf:Description>
  </rdf:RDF>
</x:xmpmeta>
<?xpacket end='w'?>";
                            File.WriteAllText(xmpPath, xmp);
                            }
                        }
                    }
                }
                catch { /* Ignore album export errors */ }

                // Clear the imported groups and refresh scan to show updated/deleted state
                Groups.Clear();
                _sourceItemsCache.Clear();
                ClearPendingImportPlan();
                if (SelectedSource != null)
                {
                    LoadSourceItems(SelectedSource);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed.");
                StatusMessage = $"Import failed: {ex.Message}";
            }
            finally
            {
                importCts.Cancel();
                try
                {
                    await timerTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when import completes.
                }

                ImportElapsedText = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
                IsImporting = false;
                ShowImportProgressDialog = false;
                _logger.LogInformation("Import finished. Imported={ImportedCount}, Failed={FailedCount}", CurrentFileBeingImported, FailedFilesForImport);
            }

            TryStartNextQueuedImport();
        }

        private void ExecuteImportPreflight()
        {
            SyncStackSelections(Groups);
            var selectedGroups = Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList();
            int selectedCount = selectedGroups.Sum(g => g.Items.Count(i => i.IsSelected));
            long totalBytes = selectedGroups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => Math.Max(0, i.FileSize));
            var duplicateNames = selectedGroups
                .SelectMany(g => g.Items.Where(i => i.IsSelected))
                .GroupBy(i => i.FileName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .Take(25)
                .ToList();

            var report = new
            {
                GeneratedAt = DateTime.Now,
                DestinationRoot,
                SelectedGroups = selectedGroups.Count,
                SelectedFiles = selectedCount,
                TotalBytes = totalBytes,
                DuplicatePolicy,
                VerificationMode,
                PotentialDuplicateNames = duplicateNames
            };

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "preflight");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"preflight-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            StatusMessage = $"Preflight complete: {selectedCount} files, {(totalBytes / (1024d * 1024d)):0.00} MB. Saved to {path}.";
        }

        private void ExecuteRetryFailedImports()
        {
            if (FailedImportRecords.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_NoFailedFilesToRetry");
                return;
            }

            var failedPaths = new HashSet<string>(FailedImportRecords.Select(f => f.SourcePath), StringComparer.OrdinalIgnoreCase);
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = failedPaths.Contains(item.SourcePath);
                }
                group.SyncSelectionFromItems();
            }

            StatusMessage = $"Retrying {failedPaths.Count} failed file(s)...";
            ExecuteImport();
        }

        private void ExecuteResumePendingImport()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (!File.Exists(path))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPendingPlan");
                    return;
                }

                var plan = System.Text.Json.JsonSerializer.Deserialize<PendingImportPlan>(File.ReadAllText(path));
                if (plan == null || plan.SelectedSourcePaths.Count == 0)
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_EmptyPendingPlan");
                    return;
                }

                if (!string.Equals(plan.SourceId, BuildSelectedSourceId(), StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = $"Pending import source is {plan.SourceDisplay}. Select that source first.";
                    return;
                }

                var selectedSet = new HashSet<string>(plan.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
                foreach (var group in Groups)
                {
                    foreach (var item in group.Items)
                    {
                        item.IsSelected = selectedSet.Contains(item.SourcePath);
                    }
                    group.SyncSelectionFromItems();
                }

                StatusMessage = $"Resuming pending import ({selectedSet.Count} files).";
                ExecuteImport();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to resume pending import: {ex.Message}";
            }
        }

        private void QueueCurrentImport()
        {
            SyncStackSelections(Groups);
            var selectedPaths = Groups
                .SelectMany(g => g.Items)
                .Where(i => i.IsSelected)
                .Select(i => i.SourcePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (selectedPaths.Count == 0)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_SelectFilesBeforeQueue");
                return;
            }

            lock (_importQueueLock)
            {
                _importQueue.Enqueue(new QueuedImportJob
                {
                    SourceDisplay = SelectedSource?.ToString() ?? "Unknown",
                    SourceId = BuildSelectedSourceId(),
                    SelectedSourcePaths = selectedPaths
                });
                QueuedImportCount = _importQueue.Count;
            }

            StatusMessage = $"Queued import job with {selectedPaths.Count} file(s).";
            if (!IsImporting)
            {
                TryStartNextQueuedImport();
            }
        }

        private void TryStartNextQueuedImport()
        {
            if (IsImporting)
            {
                return;
            }

            QueuedImportJob? nextJob = null;
            lock (_importQueueLock)
            {
                if (_importQueue.Count > 0)
                {
                    nextJob = _importQueue.Dequeue();
                }
                QueuedImportCount = _importQueue.Count;
            }

            if (nextJob == null)
            {
                return;
            }

            if (!string.Equals(nextJob.SourceId, BuildSelectedSourceId(), StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = $"Queued import for {nextJob.SourceDisplay} is waiting. Switch to that source and queue again to run.";
                return;
            }

            var selectedSet = new HashSet<string>(nextJob.SelectedSourcePaths, StringComparer.OrdinalIgnoreCase);
            foreach (var group in Groups)
            {
                foreach (var item in group.Items)
                {
                    item.IsSelected = selectedSet.Contains(item.SourcePath);
                }
                group.SyncSelectionFromItems();
            }

            StatusMessage = $"Starting queued import ({selectedSet.Count} file(s)).";
            ExecuteImport();
        }

        private void SaveCurrentPreset()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "presets");
                Directory.CreateDirectory(dir);
                var preset = new UserPreset
                {
                    Name = $"preset-{DateTime.Now:yyyyMMdd-HHmmss}",
                    DestinationRoot = DestinationRoot,
                    NamingTemplate = NamingTemplate,
                    VerificationMode = VerificationMode,
                    DuplicatePolicy = DuplicatePolicy,
                    ThumbnailPerformanceMode = ThumbnailPerformanceMode,
                    GroupRawAndRenderedPairs = GroupRawAndRenderedPairs,
                    ExpandPreviewStacks = ExpandPreviewStacks,
                    EmbedKeywordsOnImport = EmbedKeywordsOnImport,
                    ConfirmBeforeImport = ConfirmBeforeImport
                };
                string path = Path.Combine(dir, preset.Name + ".json");
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(preset, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                StatusMessage = $"Saved preset: {preset.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to save preset: {ex.Message}";
            }
        }

        private void LoadLatestPreset()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "presets");
                if (!Directory.Exists(dir))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPresets");
                    return;
                }

                string? latest = Directory.GetFiles(dir, "*.json")
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
                if (string.IsNullOrWhiteSpace(latest))
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_NoPresets");
                    return;
                }

                var preset = System.Text.Json.JsonSerializer.Deserialize<UserPreset>(File.ReadAllText(latest));
                if (preset == null)
                {
                    StatusMessage = AppLocalizer.Get("Vm_Status_PresetCouldNotLoad");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(preset.DestinationRoot)) DestinationRoot = preset.DestinationRoot;
                if (!string.IsNullOrWhiteSpace(preset.NamingTemplate)) NamingTemplate = preset.NamingTemplate;
                if (!string.IsNullOrWhiteSpace(preset.VerificationMode)) VerificationMode = preset.VerificationMode;
                if (!string.IsNullOrWhiteSpace(preset.DuplicatePolicy)) DuplicatePolicy = preset.DuplicatePolicy;
                if (!string.IsNullOrWhiteSpace(preset.ThumbnailPerformanceMode)) ThumbnailPerformanceMode = preset.ThumbnailPerformanceMode;
                GroupRawAndRenderedPairs = preset.GroupRawAndRenderedPairs;
                ExpandPreviewStacks = preset.ExpandPreviewStacks;
                EmbedKeywordsOnImport = preset.EmbedKeywordsOnImport;
                ConfirmBeforeImport = preset.ConfirmBeforeImport;
                SaveConfig();
                StatusMessage = $"Loaded preset: {preset.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Unable to load preset: {ex.Message}";
            }
        }

        private static void ShowWindowsImportCompletionNotification(int importedCount, int totalCount, int failedCount)
        {
            string title = failedCount > 0 ? "Import completed with warnings" : "Import completed";
            string body = failedCount > 0
                ? $"Imported {importedCount}/{totalCount}. Failed: {failedCount}."
                : $"Imported {importedCount}/{totalCount} successfully.";

            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
                // Ignore local sound playback issues.
            }

            try
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(
                        body,
                        title,
                        MessageBoxButton.OK,
                        failedCount > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);
                }));
            }
            catch
            {
                // Ignore notification failures to avoid interrupting import completion.
            }
        }

        private IngestOptions CreateIngestOptions(ItemGroup group)
        {
            DuplicateHandlingMode duplicateMode = DuplicatePolicy switch
            {
                "Skip" => DuplicateHandlingMode.Skip,
                "OverwriteIfNewer" => DuplicateHandlingMode.OverwriteIfNewer,
                _ => DuplicateHandlingMode.Suffix
            };

            ImportVerificationMode verification = VerificationMode == "Strict"
                ? ImportVerificationMode.Strict
                : ImportVerificationMode.Fast;

            List<string> keywords = KeywordInputParser.Parse(group.KeywordsText);
            bool applyKeywords = EmbedKeywordsOnImport && keywords.Count > 0;

            return new IngestOptions
            {
                DuplicateHandling = duplicateMode,
                VerificationMode = verification,
                ApplyImportKeywords = applyKeywords,
                ImportKeywords = applyKeywords ? keywords : null
            };
        }

        private async Task ExecuteUnifiedImportAsync(List<ItemGroup> selectedGroups, CancellationToken cancellationToken)
        {
            IFileProvider localProvider = _fileProviderFactory.CreateLocalProvider();
            var ftpProviders = new Dictionary<string, IFileProvider>(StringComparer.OrdinalIgnoreCase);
            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var group in selectedGroups)
                {
                    var localItems = group.Items.Where(i => i.IsSelected && !i.IsFtpSource).ToList();
                    if (localItems.Count > 0)
                    {
                        await ImportUnifiedSubsetAsync(group, localItems, localProvider, cancellationToken);
                    }

                    var ftpBatches = group.Items
                        .Where(i => i.IsSelected && i.IsFtpSource)
                        .GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var ftpBatch in ftpBatches)
                    {
                        if (!ftpSourcesByKey.TryGetValue(ftpBatch.Key, out var ftpSource))
                        {
                            foreach (var item in ftpBatch)
                            {
                                ProcessedFilesForImport++;
                                FailedFilesForImport++;
                                CurrentFileBeingImported = ProcessedFilesForImport - FailedFilesForImport;
                                ProgressPercent = TotalFilesForImport > 0 ? (ProcessedFilesForImport * 100) / TotalFilesForImport : 0;
                                StatusMessage = $"Failed {item.FileName} | missing FTP source configuration.";
                            }

                            continue;
                        }

                        if (!ftpProviders.TryGetValue(ftpBatch.Key, out var ftpProvider))
                        {
                            ftpProvider = _fileProviderFactory.CreateFtpProvider(ftpSource.Host, ftpSource.Port, ftpSource.User, ftpSource.Pass);
                            ftpProviders[ftpBatch.Key] = ftpProvider;
                        }

                        await ImportUnifiedSubsetAsync(group, ftpBatch.ToList(), ftpProvider, cancellationToken);
                    }
                }
            }
            finally
            {
                foreach (var provider in ftpProviders.Values)
                {
                    if (provider is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                }
            }
        }

        private async Task ImportUnifiedSubsetAsync(ItemGroup group, List<ImportItem> items, IFileProvider provider, CancellationToken cancellationToken)
        {
            if (items.Count == 0)
            {
                return;
            }

            var subsetGroup = new ItemGroup
            {
                Title = group.Title,
                StartDate = group.StartDate,
                EndDate = group.EndDate,
                AlbumName = group.AlbumName,
                FolderPath = group.FolderPath,
                KeywordsText = group.KeywordsText,
                Items = items
            };

            var engine = _ingestEngineFactory.Create(provider);
            engine.ProgressChanged += (percent, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusMessage = msg;
                });
            };

            engine.ItemProcessed += progress =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProcessedFilesForImport++;
                    if (!progress.Success)
                    {
                        FailedFilesForImport++;
                        FailedImportRecords.Add(new FailedImportRecord
                        {
                            SourcePath = progress.SourcePath,
                            FileName = progress.FileName,
                            ErrorMessage = string.IsNullOrWhiteSpace(progress.ErrorMessage) ? "Import failed." : progress.ErrorMessage
                        });
                    }
                    else
                    {
                        _processedBytesForImport += Math.Max(0, progress.FileSizeBytes);
                    }

                    CurrentFileBeingImported = ProcessedFilesForImport - FailedFilesForImport;
                    CurrentGroupFileBeingImported = progress.GroupCurrent;
                    TotalFilesInCurrentGroup = progress.GroupTotal;
                    CurrentGroupProgressPercent = progress.GroupTotal > 0 ? (progress.GroupCurrent * 100) / progress.GroupTotal : 0;
                    CurrentImportGroupTitle = progress.GroupTitle;
                    ProgressPercent = TotalFilesForImport > 0 ? (ProcessedFilesForImport * 100) / TotalFilesForImport : 0;

                    string state = progress.Success ? "Copying" : "Failed";
                    StatusMessage = $"{state} {progress.FileName} | overall {ProcessedFilesForImport}/{TotalFilesForImport} | group {progress.GroupCurrent}/{progress.GroupTotal}";
                });
            };

            await engine.IngestGroupAsync(subsetGroup, DestinationRoot, NamingTemplate, cancellationToken, CreateIngestOptions(group));

            if (!DeleteAfterImport)
            {
                return;
            }

            foreach (var item in items)
            {
                try
                {
                    await provider.DeleteAsync(item.SourcePath, cancellationToken);
                }
                catch
                {
                    // Ignore source-delete failures so import completion is not blocked.
                }
            }
        }

        // ExecuteBrowseDestination removed along with its UI entry.

        private void ExecuteBrowseScanPath()
        {
            if (SelectedSource is not string localRoot)
            {
                StatusMessage = AppLocalizer.Get("Vm_Status_BrowseOnlyLocal");
                return;
            }

            string initialDirectory = ResolveLocalScanPath(localRoot, ScanPath);
            if (!Directory.Exists(initialDirectory))
            {
                initialDirectory = localRoot;
            }

            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Folder To Scan",
                InitialDirectory = initialDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                ScanPath = dialog.FolderName;
            }
        }

        private void OpenDriveSelectionDialog()
        {
            DriveSelectionItems.Clear();
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed))
                .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var drive in drives)
            {
                string deviceId = GetOrCreateDeviceIdForDrive(drive);
                _driveDeviceIdByPath[drive.Name] = deviceId;
                _drivePathByDeviceId[deviceId] = drive.Name;
                bool selected = _selectedDriveDeviceIds.Count == 0
                    ? drive.DriveType == DriveType.Removable
                    : _selectedDriveDeviceIds.Contains(deviceId);
                DriveSelectionItems.Add(new DriveSelectionOption
                {
                    Name = drive.Name,
                    Label = $"{drive.Name} ({drive.DriveType})",
                    DriveType = drive.DriveType.ToString(),
                    DeviceId = deviceId,
                    IsSelected = selected
                });
            }

            ShowDriveSelectionDialog = true;
        }

        private async Task ExecuteConfirmDriveSelectionAsync()
        {
            _selectedDriveDeviceIds.Clear();
            foreach (var item in DriveSelectionItems.Where(i => i.IsSelected))
            {
                if (!string.IsNullOrWhiteSpace(item.DeviceId))
                {
                    _selectedDriveDeviceIds.Add(item.DeviceId);
                }
            }

            ShowDriveSelectionDialog = false;
            await ScanDrivesAsync();
            await RefreshExclusionManagementListsAsync().ConfigureAwait(true);
            SaveConfig();
        }

        /// <summary>
        /// Skipped-folder rules use the same keys as <see cref="ApplySkippedFolderFilters"/> (not the literal Unified sentinel).
        /// </summary>
        private string ResolveSkipRuleSourceId(string folderPath)
        {
            if (SelectedSource is not UnifiedSourceItem)
            {
                return BuildSelectedSourceId();
            }

            ItemGroup? group = Groups.FirstOrDefault(g =>
                string.Equals(g.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
            ImportItem? item = group?.Items.FirstOrDefault();
            if (item == null)
            {
                _logger.LogWarning("Skip folder: no group matching path {Path}.", folderPath);
                return string.Empty;
            }

            if (item.IsFtpSource)
            {
                return item.SourceId;
            }

            string root = Path.GetPathRoot(item.SourcePath) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(root))
            {
                return string.Empty;
            }

            return BuildLocalSourceRuleKey(root);
        }

        private void ExecuteSkipFolder(string? folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _logger.LogWarning("Skip folder invoked with empty path (UI binding may be broken).");
                return;
            }

            ItemGroup? matchGroup = Groups.FirstOrDefault(g =>
                string.Equals(g.FolderPath, folderPath, StringComparison.OrdinalIgnoreCase));
            bool isFtpFolder = matchGroup?.Items.FirstOrDefault()?.IsFtpSource == true;
            string normalizedFolder = isFtpFolder ? NormalizeFtpPath(folderPath) : folderPath.TrimEnd('\\', '/');

            string sourceId = ResolveSkipRuleSourceId(folderPath);
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                StatusMessage = "Could not attach skip rule to a source. Try again after the list finishes loading.";
                return;
            }
            if (!_skippedFoldersBySource.TryGetValue(sourceId, out var entries))
            {
                entries = new List<string>();
                _skippedFoldersBySource[sourceId] = entries;
            }

            if (!entries.Any(p => FolderPathsMatchForSkipRule(p, normalizedFolder, isFtpFolder)))
            {
                entries.Add(normalizedFolder);
            }

            InvalidateSourceItemsCache(sourceId);

            SaveConfig();

            var toRemove = Groups.Where(g => FolderPathsMatchForSkipRule(g.FolderPath, normalizedFolder, isFtpFolder)).ToList();
            foreach (var group in toRemove)
            {
                Groups.Remove(group);
            }

            _logger.LogInformation("Skip folder rule stored for source key {SourceId}: {FolderPath}", sourceId, normalizedFolder);

            StatusMessage = $"Skipping folder in future scans: {normalizedFolder}";
            _ = RefreshExclusionManagementListsAsync();
        }

        private async Task ExecuteRemoveDriveExclusionAsync(string? deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return;
            }

            if (_selectedDriveDeviceIds.Count == 0)
            {
                // Seed explicit selections from implicit defaults so removing a fixed-drive
                // exclusion does not accidentally exclude all removable drives.
                var removableDrives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
                    .ToList();
                foreach (var d in removableDrives)
                {
                    string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        _selectedDriveDeviceIds.Add(id);
                    }
                }
            }

            _selectedDriveDeviceIds.Add(deviceId);
            await ScanDrivesAsync();
            await RefreshExclusionManagementListsAsync().ConfigureAwait(true);
            SaveConfig();
        }

        private void ExecuteRemoveSkippedFolderRule(SkippedFolderRuleEntry? entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.SourceId) || string.IsNullOrWhiteSpace(entry.FolderPath))
            {
                return;
            }

            if (_skippedFoldersBySource.TryGetValue(entry.SourceId, out var list))
            {
                bool ftpRule = entry.SourceId.StartsWith("ftp|", StringComparison.OrdinalIgnoreCase);
                list.RemoveAll(p => FolderPathsMatchForSkipRule(p, entry.FolderPath, ftpRule));
                if (list.Count == 0)
                {
                    _skippedFoldersBySource.Remove(entry.SourceId);
                }

                _ = RefreshExclusionManagementListsAsync();
                SaveConfig();
                InvalidateSourceItemsCache(entry.SourceId);
            }
        }

        private static bool FolderPathsMatchForSkipRule(string a, string b, bool isFtp)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            {
                return false;
            }

            if (isFtp)
            {
                return string.Equals(NormalizeFtpPath(a), NormalizeFtpPath(b), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }

        private void InvalidateSourceItemsCache(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            _sourceItemsCache.Remove(sourceId);
        }

        /// <summary>
        /// Must run after <see cref="StampItems"/> so FTP items carry the same source key as skip rules.
        /// Local rules use local-device|… keys (see <see cref="BuildLocalSourceRuleKey"/>), not raw stamped keys.
        /// </summary>
        private void ApplySkippedFolderFilters(List<ImportItem> items, HashSet<string> userExcludedFolders)
        {
            int before = items.Count;
            items.RemoveAll(item =>
            {
                string ruleLookupKey = GetSkipRuleLookupKey(item);
                if (string.IsNullOrWhiteSpace(ruleLookupKey) ||
                    !_skippedFoldersBySource.TryGetValue(ruleLookupKey, out var skippedPrefixes) ||
                    skippedPrefixes.Count == 0)
                {
                    return false;
                }

                string folder = item.IsFtpSource
                    ? ExtractFtpFolderPath(item.SourcePath)
                    : (Path.GetDirectoryName(item.SourcePath) ?? string.Empty);

                bool shouldSkip = skippedPrefixes.Any(prefix =>
                {
                    if (string.IsNullOrWhiteSpace(prefix))
                    {
                        return false;
                    }

                    string normalizedPrefix = item.IsFtpSource ? NormalizeFtpPath(prefix) : prefix.TrimEnd('\\', '/');
                    string normalizedFolder = item.IsFtpSource ? NormalizeFtpPath(folder) : folder.TrimEnd('\\', '/');

                    return normalizedFolder.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
                });

                if (shouldSkip)
                {
                    userExcludedFolders.Add(folder);
                }

                return shouldSkip;
            });

            int removed = before - items.Count;
            if (removed > 0)
            {
                _logger.LogInformation(
                    "Skipped-folder blacklist removed {Removed} import item(s) from {Before} scanned (active rule sources: {RuleCount}).",
                    removed,
                    before,
                    _skippedFoldersBySource.Count);
            }
        }

        private string GetSkipRuleLookupKey(ImportItem item)
        {
            if (item.IsFtpSource)
            {
                return item.SourceId ?? string.Empty;
            }

            string root = Path.GetPathRoot(item.SourcePath) ?? string.Empty;
            return string.IsNullOrWhiteSpace(root) ? string.Empty : BuildLocalSourceRuleKey(root);
        }

        /// <summary>
        /// Rebuilds the skipped-folder blacklist UI from in-memory rules (same keys as ingest filtering).
        /// Kept separate from drive enumeration so the list stays accurate even when drive I/O fails.
        /// </summary>
        private void RebuildSkippedFolderRuleEntries()
        {
            SkippedFolderRuleEntries.Clear();
            foreach (var kvp in _skippedFoldersBySource.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var folder in kvp.Value.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
                {
                    SkippedFolderRuleEntries.Add(new SkippedFolderRuleEntry
                    {
                        SourceId = kvp.Key,
                        FolderPath = folder
                    });
                }
            }
        }

        private async Task RefreshExclusionManagementListsAsync()
        {
            await Application.Current.Dispatcher.InvokeAsync(RebuildSkippedFolderRuleEntries).Task.ConfigureAwait(true);

            List<DriveInfo> drives;
            try
            {
                drives = await Task.Run(() =>
                    DriveInfo.GetDrives()
                        .Where(d => d.IsReady && (d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed))
                        .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                        .ToList()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drive enumeration failed for exclusion UI; skipped-folder blacklist was still refreshed.");
                await Application.Current.Dispatcher.InvokeAsync(() => ExcludedDriveEntries.Clear()).Task.ConfigureAwait(true);
                return;
            }

            (DriveInfo drive, string deviceId)[] resolved = await Task.WhenAll(
                drives.Select(async d =>
                {
                    string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                    return (drive: d, deviceId: id);
                })).ConfigureAwait(false);

            HashSet<string> selectedSnapshot = await Application.Current.Dispatcher
                .InvokeAsync(() => new HashSet<string>(_selectedDriveDeviceIds, StringComparer.OrdinalIgnoreCase))
                .Task
                .ConfigureAwait(false);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var pair in resolved)
                {
                    _driveDeviceIdByPath[pair.drive.Name] = pair.deviceId;
                    _drivePathByDeviceId[pair.deviceId] = pair.drive.Name;
                }

                ExcludedDriveEntries.Clear();
                foreach (var pair in resolved)
                {
                    bool isExcluded = selectedSnapshot.Count == 0
                        ? pair.drive.DriveType == DriveType.Fixed
                        : !selectedSnapshot.Contains(pair.deviceId);
                    if (isExcluded)
                    {
                        ExcludedDriveEntries.Add(new ExcludedDriveEntry
                        {
                            DeviceId = pair.deviceId,
                            DriveName = pair.drive.Name,
                            DriveType = pair.drive.DriveType.ToString()
                        });
                    }
                }
            }).Task.ConfigureAwait(true);
        }

        private const int DeviceIdIoTimeoutMs = 2500;

        private static string GetDeviceIdMarkerPath(DriveInfo drive) => Path.Combine(drive.RootDirectory.FullName, ".quickmediaingest-device.id");

        private static string PathFallbackDeviceId(DriveInfo drive) => $"path:{drive.Name.ToUpperInvariant()}";

        /// <summary>
        /// Synchronous per-drive root I/O. Can block indefinitely on a bad volume; use only from
        /// <see cref="GetOrCreateDeviceIdForDrive"/> or <see cref="ResolveDeviceIdWithTimeoutAsync"/> runners.
        /// </summary>
        private static string TryReadOrWriteDeviceMarker(DriveInfo drive)
        {
            string markerPath = GetDeviceIdMarkerPath(drive);
            try
            {
                if (File.Exists(markerPath))
                {
                    string existing = File.ReadAllText(markerPath).Trim();
                    if (!string.IsNullOrWhiteSpace(existing))
                    {
                        return existing;
                    }
                }

                string deviceId = Guid.NewGuid().ToString("N");
                File.WriteAllText(markerPath, deviceId);
                return deviceId;
            }
            catch
            {
                return PathFallbackDeviceId(drive);
            }
        }

        private async Task<string> ResolveDeviceIdWithTimeoutAsync(DriveInfo drive)
        {
            try
            {
                var task = Task.Run(() => TryReadOrWriteDeviceMarker(drive));
                return await task.WaitAsync(TimeSpan.FromMilliseconds(DeviceIdIoTimeoutMs)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Device id I/O timed out for {Drive} after {TimeoutMs} ms; using path fallback.", drive.Name, DeviceIdIoTimeoutMs);
                return PathFallbackDeviceId(drive);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device id I/O failed for {Drive}; using path fallback.", drive.Name);
                return PathFallbackDeviceId(drive);
            }
        }

        private string GetOrCreateDeviceIdForDrive(DriveInfo drive)
        {
            try
            {
                var task = Task.Run(() => TryReadOrWriteDeviceMarker(drive));
                if (task.Wait(TimeSpan.FromMilliseconds(DeviceIdIoTimeoutMs)))
                {
                    return task.Result;
                }

                _logger.LogWarning("Device id I/O timed out for {Drive} after {TimeoutMs} ms; using path fallback.", drive.Name, DeviceIdIoTimeoutMs);
                return PathFallbackDeviceId(drive);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Device id I/O failed for {Drive}; using path fallback.", drive.Name);
                return PathFallbackDeviceId(drive);
            }
        }

        private string ResolveDeviceIdFromLocalPath(string localPath)
        {
            string root = Path.GetPathRoot(localPath) ?? localPath;
            if (_driveDeviceIdByPath.TryGetValue(root, out var known))
            {
                return known;
            }

            try
            {
                var info = new DriveInfo(root);
                if (info.IsReady)
                {
                    string id = GetOrCreateDeviceIdForDrive(info);
                    _driveDeviceIdByPath[info.Name] = id;
                    _drivePathByDeviceId[id] = info.Name;
                    return id;
                }
            }
            catch
            {
                // Fallback below.
            }

            return $"path:{root.ToUpperInvariant()}";
        }

        private string BuildLocalSourceRuleKey(string localPath)
        {
            return $"local-device|{ResolveDeviceIdFromLocalPath(localPath)}";
        }

        private void SaveImportHistoryRecord(TimeSpan duration)
        {
            try
            {
                var record = new ImportHistoryRecord
                {
                    StartedAtLocal = _importStartedAtUtc == DateTime.MinValue ? DateTime.Now : _importStartedAtUtc.ToLocalTime(),
                    DurationSeconds = Math.Max(0, duration.TotalSeconds),
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    Source = SelectedSource?.ToString() ?? "Unknown",
                    Destination = DestinationRoot
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ImportHistoryRecords.Insert(0, record);
                    while (ImportHistoryRecords.Count > 50)
                    {
                        ImportHistoryRecords.RemoveAt(ImportHistoryRecords.Count - 1);
                    }
                });

                string path = GetImportHistoryPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Path.GetTempPath());
                string json = System.Text.Json.JsonSerializer.Serialize(ImportHistoryRecords.ToList());
                File.WriteAllText(path, json);
            }
            catch
            {
                // Ignore history persistence errors.
            }
        }

        private string BuildSelectedSourceId()
        {
            return SelectedSource switch
            {
                FtpSourceItem ftp => BuildSourceKey(ftp),
                string local => BuildLocalSourceRuleKey(local),
                UnifiedSourceItem => "unified",
                _ => "unknown"
            };
        }

        private static string GetPendingImportPlanPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "pending-import.json");
        }

        private void SavePendingImportPlan(List<ItemGroup> selectedGroups)
        {
            try
            {
                var selectedPaths = selectedGroups
                    .SelectMany(g => g.Items)
                    .Where(i => i.IsSelected)
                    .Select(i => i.SourcePath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var plan = new PendingImportPlan
                {
                    CreatedAt = DateTime.Now,
                    SourceId = BuildSelectedSourceId(),
                    SourceDisplay = SelectedSource?.ToString() ?? "Unknown",
                    DestinationRoot = DestinationRoot,
                    NamingTemplate = NamingTemplate,
                    SelectedSourcePaths = selectedPaths
                };
                string json = System.Text.Json.JsonSerializer.Serialize(plan, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetPendingImportPlanPath(), json);
            }
            catch
            {
                // Ignore pending plan persistence failures.
            }
        }

        private void ClearPendingImportPlan()
        {
            try
            {
                string path = GetPendingImportPlanPath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore clear failures.
            }
        }

        private void ExportImportReportArtifact(TimeSpan duration, List<ItemGroup> selectedGroups)
        {
            try
            {
                string reportDir = Path.Combine(DestinationRoot, "_ImportReports");
                Directory.CreateDirectory(reportDir);
                string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var report = new ImportReportArtifact
                {
                    GeneratedAt = DateTime.Now,
                    Source = SelectedSource?.ToString() ?? "Unknown",
                    Destination = DestinationRoot,
                    DurationSeconds = duration.TotalSeconds,
                    FilesSelected = TotalFilesForImport,
                    FilesImported = CurrentFileBeingImported,
                    FailedFiles = FailedFilesForImport,
                    VerificationMode = VerificationMode,
                    DuplicatePolicy = DuplicatePolicy,
                    Failed = FailedImportRecords.ToList()
                };

                string jsonPath = Path.Combine(reportDir, $"import-report-{timestamp}.json");
                File.WriteAllText(jsonPath, System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                var text = new StringBuilder();
                text.AppendLine($"Import Report - {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
                text.AppendLine($"Source: {report.Source}");
                text.AppendLine($"Destination: {report.Destination}");
                text.AppendLine($"DurationSeconds: {report.DurationSeconds:0.##}");
                text.AppendLine($"Imported: {report.FilesImported}/{report.FilesSelected}");
                text.AppendLine($"Failed: {report.FailedFiles}");
                text.AppendLine($"Verification: {report.VerificationMode}");
                text.AppendLine($"DuplicatePolicy: {report.DuplicatePolicy}");
                if (FailedImportRecords.Count > 0)
                {
                    text.AppendLine("Failed Files:");
                    foreach (var failure in FailedImportRecords)
                    {
                        text.AppendLine($"- {failure.FileName} | {failure.ErrorMessage}");
                    }
                }
                string txtPath = Path.Combine(reportDir, $"import-report-{timestamp}.txt");
                File.WriteAllText(txtPath, text.ToString());
            }
            catch
            {
                // Ignore report export errors.
            }
        }

        private void LoadImportHistory()
        {
            try
            {
                string path = GetImportHistoryPath();
                if (!File.Exists(path))
                {
                    return;
                }

                string json = File.ReadAllText(path);
                var records = System.Text.Json.JsonSerializer.Deserialize<List<ImportHistoryRecord>>(json) ?? new List<ImportHistoryRecord>();

                ImportHistoryRecords.Clear();
                foreach (var record in records.OrderByDescending(r => r.StartedAtLocal).Take(50))
                {
                    ImportHistoryRecords.Add(record);
                }
            }
            catch
            {
                // Ignore history load errors.
            }
        }

        private static string GetImportHistoryPath()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest");
            return Path.Combine(folder, "import-history.json");
        }

        public void SaveConfig()
        {
            try
            {
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
                    FtpPass = FtpPass,
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
                    SettingsAdvancedExpanded = SettingsAdvancedExpanded,
                    RibbonTileOrder = _ribbonTileOrder.Count > 0 ? _ribbonTileOrder : null,
                    UpdatePackageType = UpdatePackageType,
                    WindowWidth = _savedWindowWidth,
                    WindowHeight = _savedWindowHeight,
                    WindowMaximized = _savedWindowMaximized,
                    WindowLeft = _savedWindowLeft,
                    WindowTop = _savedWindowTop,
                    IsFirstRun = this.IsFirstRun,
                    SelectedDriveDeviceIds = _selectedDriveDeviceIds.ToList(),
                    SkippedFoldersBySource = _skippedFoldersBySource.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList(), StringComparer.OrdinalIgnoreCase)
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(config);
                File.WriteAllText(path, json);
            } catch { }
        }

        private void LoadConfig()
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "config.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
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
                        if (!string.IsNullOrWhiteSpace(config.FtpPass)) FtpPass = config.FtpPass;
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
                        SuppressExcludedFolderScanReminders = config.SuppressExcludedFolderScanReminders;
                        SettingsAdvancedExpanded = config.SettingsAdvancedExpanded;
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
                        OnPropertyChanged(nameof(SettingsAdvancedExpanded));
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
                    }
                }
            } catch { }
        }

        /// <summary>
        /// <see cref="DriveInfo.IsReady"/> can block on unreachable volumes; never call it synchronously on the UI thread.
        /// </summary>
        private async Task<bool> IsDriveReadyWithTimeoutAsync(DriveInfo drive, int timeoutMs = 1500)
        {
            try
            {
                Task<bool> task = Task.Run(() =>
                {
                    try
                    {
                        return drive.IsReady;
                    }
                    catch
                    {
                        return false;
                    }
                });
                return await task.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Drive IsReady check timed out for {DriveName} ({DriveType}) after {TimeoutMs} ms; treating as not ready.",
                    drive.Name,
                    drive.DriveType,
                    timeoutMs);
                return false;
            }
        }

        /// <summary>
        /// Lists fixed/removable drives off the UI thread, with a bounded wait per volume for IsReady.
        /// </summary>
        private async Task<List<DriveInfo>> EnumerateCandidateDrivesAsync()
        {
            DriveInfo[] all;
            try
            {
                all = DriveInfo.GetDrives();
            }
            catch
            {
                return new List<DriveInfo>();
            }

            var typed = all
                .Where(d => d.DriveType == DriveType.Removable || d.DriveType == DriveType.Fixed)
                .ToList();

            var checks = await Task.WhenAll(
                typed.Select(async d =>
                {
                    bool ok = await IsDriveReadyWithTimeoutAsync(d).ConfigureAwait(false);
                    return (drive: d, ok);
                })).ConfigureAwait(false);

            return checks.Where(x => x.ok).Select(x => x.drive).ToList();
        }

        private async Task ScanDrivesAsync()
        {
            try
            {
                _sourceItemsCache.Clear();

                List<DriveInfo> candidateDrives = await EnumerateCandidateDrivesAsync().ConfigureAwait(false);

                (DriveInfo drive, string deviceId)[] resolved = await Task.WhenAll(
                    candidateDrives.Select(async d =>
                    {
                        string id = await ResolveDeviceIdWithTimeoutAsync(d).ConfigureAwait(false);
                        return (drive: d, deviceId: id);
                    })).ConfigureAwait(false);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var pair in resolved)
                    {
                        _driveDeviceIdByPath[pair.drive.Name] = pair.deviceId;
                        _drivePathByDeviceId[pair.deviceId] = pair.drive.Name;
                    }

                    var activeDrives = resolved
                        .Where(pair =>
                        {
                            bool includeByDefault = _selectedDriveDeviceIds.Count == 0 && pair.drive.DriveType == DriveType.Removable;
                            return includeByDefault ||
                                   _selectedDriveDeviceIds.Contains(pair.deviceId) ||
                                   _selectedDriveDeviceIds.Contains($"path:{pair.drive.Name.ToUpperInvariant()}");
                        })
                        .Select(pair => pair.drive.Name)
                        .ToList();

                    for (int i = Sources.Count - 1; i >= 0; i--)
                    {
                        if (Sources[i] is string s)
                        {
                            if (s.Contains(':') && !activeDrives.Contains(s))
                            {
                                Sources.RemoveAt(i);
                                if (SelectedSource as string == s) SelectedSource = null;
                            }
                        }
                    }

                    foreach (string drive in activeDrives)
                    {
                        if (!Sources.Contains(drive))
                        {
                            Sources.Add(drive);
                        }
                    }

                    if (!Sources.Contains(_unifiedSource))
                    {
                        Sources.Insert(0, _unifiedSource);
                    }
                });
            }
            catch
            {
                // Keep UI/config load stable on drive enumeration errors.
            }
        }

        private static string NormalizeFtpPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";

            string normalized = path.Trim().Replace("\\", "/");
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized;
        }

        private static string ExtractFtpFolderPath(string sourcePath)
        {
            string normalized = NormalizeFtpPath(sourcePath);
            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return "/";
            }

            return normalized.Substring(0, lastSlash);
        }

        private static string? GetParentFtpPath(string path)
        {
            string normalized = NormalizeFtpPath(path);
            if (string.Equals(normalized, "/", StringComparison.Ordinal))
            {
                return null;
            }

            int lastSlash = normalized.LastIndexOf('/');
            if (lastSlash <= 0)
            {
                return "/";
            }

            return normalized.Substring(0, lastSlash);
        }

        private static string ResolveLocalScanPath(string localRoot, string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath))
            {
                return localRoot;
            }

            string trimmed = candidatePath.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                return trimmed;
            }

            return Path.Combine(localRoot, trimmed.TrimStart('\\', '/'));
        }

        public bool IsFirstRun { get; set; } = true;

        public void ShowOnboarding(Window owner, bool markNotFirstRun = true)
        {
            var dialog = new OnboardingDialog { Owner = owner };
            if (dialog.ShowDialog() == true && markNotFirstRun)
            {
                IsFirstRun = false;
                SaveConfig();
            }
        }
    }

    public class FtpSourceItem
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string User { get; set; } = "anonymous";
        public string Pass { get; set; } = "anonymous";
        public string RemoteFolder { get; set; } = "/DCIM";

        public override string ToString() => $"FTP: {Host} ({RemoteFolder})";
    }

    public class UnifiedSourceItem
    {
        public override string ToString() => "Unified (SD + FTP)";
    }

    public class FtpFolderOption
    {
        public string Path { get; set; } = "/";
        public string Label { get; set; } = string.Empty;
    }

    public class TokenInsertPayload
    {
        public string Token { get; set; } = string.Empty;
        public int Index { get; set; } = -1;
        public bool FromSelected { get; set; } = false;
    }

    public partial class MainViewModel : ObservableObject
    {
        [RelayCommand]
        private void RemoveToken(TokenItem? item)
        {
            if (item == null) return;
            if (SelectedTokens.Contains(item))
            {
                SelectedTokens.Remove(item);
                UpdateNamingFromTokens();

                var value = item.Value;
                if (!string.IsNullOrEmpty(value) &&
                    value.StartsWith("[") &&
                    value.EndsWith("]") &&
                    !AvailableTokens.Contains(value))
                {
                    AvailableTokens.Add(value);
                }
            }
        }

        [RelayCommand]
        private void InsertToken(TokenInsertPayload? payload)
        {
            if (payload == null || string.IsNullOrEmpty(payload.Token)) return;

            int insertIndex = payload.Index;
            if (insertIndex < 0 || insertIndex > SelectedTokens.Count) insertIndex = SelectedTokens.Count;

            if (payload.FromSelected)
            {
                int existingIndex = SelectedTokens.Select((item, index) => new { item, index })
                    .FirstOrDefault(x => x.item.Value == payload.Token)?.index ?? -1;

                if (existingIndex >= 0)
                {
                    var movingItem = SelectedTokens[existingIndex];
                    SelectedTokens.RemoveAt(existingIndex);
                    if (existingIndex < insertIndex) insertIndex--;
                    if (insertIndex < 0) insertIndex = 0;
                    if (insertIndex > SelectedTokens.Count) insertIndex = SelectedTokens.Count;
                    SelectedTokens.Insert(insertIndex, movingItem);
                    UpdateNamingFromTokens();
                }
                return;
            }

            // Prevent duplicate placeholders
            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]") && SelectedTokens.Any(t => t.Value == payload.Token))
            {
                return;
            }

            SelectedTokens.Insert(insertIndex, new TokenItem { Value = payload.Token });
            UpdateNamingFromTokens();

            if (payload.Token.StartsWith("[") && payload.Token.EndsWith("]") && AvailableTokens.Contains(payload.Token))
            {
                AvailableTokens.Remove(payload.Token);
            }
        }
    }

    public class ImportHistoryRecord
    {
        public DateTime StartedAtLocal { get; set; } = DateTime.Now;
        public double DurationSeconds { get; set; }
        public int FilesSelected { get; set; }
        public int FilesImported { get; set; }
        public int FailedFiles { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;

        public string StartedAtDisplay => StartedAtLocal.ToString("yyyy-MM-dd HH:mm:ss");
        public string DurationDisplay => TimeSpan.FromSeconds(Math.Max(0, DurationSeconds)).ToString(@"hh\:mm\:ss");
        public string SummaryDisplay =>
            $"{StartedAtDisplay} | Imported {FilesImported}/{FilesSelected} | Failed {FailedFiles} | Duration {DurationDisplay}";
    }

    public class FailedImportRecord
    {
        public string SourcePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    internal sealed class QueuedImportJob
    {
        public string SourceId { get; set; } = string.Empty;
        public string SourceDisplay { get; set; } = string.Empty;
        public List<string> SelectedSourcePaths { get; set; } = new();
    }

    internal sealed class PendingImportPlan
    {
        public DateTime CreatedAt { get; set; }
        public string SourceId { get; set; } = string.Empty;
        public string SourceDisplay { get; set; } = string.Empty;
        public string DestinationRoot { get; set; } = string.Empty;
        public string NamingTemplate { get; set; } = string.Empty;
        public List<string> SelectedSourcePaths { get; set; } = new();
    }

    internal sealed class ImportReportArtifact
    {
        public DateTime GeneratedAt { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public int FilesSelected { get; set; }
        public int FilesImported { get; set; }
        public int FailedFiles { get; set; }
        public string VerificationMode { get; set; } = string.Empty;
        public string DuplicatePolicy { get; set; } = string.Empty;
        public List<FailedImportRecord> Failed { get; set; } = new();
    }

    internal sealed class UserPreset
    {
        public string Name { get; set; } = string.Empty;
        public string DestinationRoot { get; set; } = string.Empty;
        public string NamingTemplate { get; set; } = string.Empty;
        public string VerificationMode { get; set; } = "Fast";
        public string DuplicatePolicy { get; set; } = "Suffix";
        public string ThumbnailPerformanceMode { get; set; } = "Balanced";
        public bool GroupRawAndRenderedPairs { get; set; } = false;
        public bool ExpandPreviewStacks { get; set; } = false;
        public bool EmbedKeywordsOnImport { get; set; }
        public bool ConfirmBeforeImport { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class UpdateIntervalOption
    {
        public string Display { get; set; } = string.Empty;
        public int Hours { get; set; }
    }

    public class DriveSelectionOption
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public bool IsSelected { get; set; } = true;
    }

    public class ExcludedDriveEntry
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DriveName { get; set; } = string.Empty;
        public string DriveType { get; set; } = string.Empty;
        public string Display => $"{DriveName} ({DriveType})";
    }

    public class SkippedFolderRuleEntry
    {
        public string SourceId { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string Display => $"{SourceId} :: {FolderPath}";
    }

    public class AppConfig
    {
        public int UpdateIntervalHours { get; set; } = 24;
        public string UpdatePackageType { get; set; } = "Portable";
        public string DestinationRoot { get; set; } = string.Empty;
        public bool DeleteAfterImport { get; set; }
        public bool DeleteAfterImportPromptDismissed { get; set; }
        public string NamingTemplate { get; set; } = "[Date]_[Time]_[Original]";
        public string NamingPreset { get; set; } = "Recommended (Date + Shoot + Original)";
        public string NamingDateFormat { get; set; } = "yyyy-MM-dd";
        public string NamingTimeFormat { get; set; } = "HH-mm-ss";
        public string NamingSeparator { get; set; } = "_";
        public bool NamingIncludeSequence { get; set; } = false;
        public string NamingShootNameSample { get; set; } = "my-shoot";
        public bool NamingLowercase { get; set; } = true;
        public string ThumbnailPerformanceMode { get; set; } = "Balanced";
        public bool GroupRawAndRenderedPairs { get; set; } = false;
        public string FtpHost { get; set; } = string.Empty;
        public int FtpPort { get; set; } = 21;
        public string FtpUser { get; set; } = string.Empty;
        public string FtpPass { get; set; } = string.Empty;
        public string FtpRemoteFolder { get; set; } = "/DCIM";
        public bool AutoReconnectLastFtp { get; set; } = true;
        public bool SettingsMenuExpanded { get; set; } = true;
        public string ScanPath { get; set; } = string.Empty;
        public bool SelectAll { get; set; } = true;
        public bool? IsDarkTheme { get; set; }
        public double ThumbnailSize { get; set; } = 120;
        public bool ScanIncludeSubfolders { get; set; } = true;
        public int TimeBetweenShootsHours { get; set; } = 4;
        public bool LimitFtpThumbnailLoad { get; set; } = false;
        public int FtpInitialThumbnailCount { get; set; } = 0;
        public bool ExpandPreviewStacks { get; set; } = false;
        public string DuplicatePolicy { get; set; } = "Suffix";
        public string VerificationMode { get; set; } = "Fast";
        public string UiLanguage { get; set; } = string.Empty;
        public bool EmbedKeywordsOnImport { get; set; }
        public bool ConfirmBeforeImport { get; set; }
        public bool SuppressExcludedFolderScanReminders { get; set; }
        public bool SettingsAdvancedExpanded { get; set; }
        public List<string>? RibbonTileOrder { get; set; }
            public double WindowWidth { get; set; } = 960;
            public double WindowHeight { get; set; } = 620;
            public bool WindowMaximized { get; set; } = false;
            public double? WindowLeft { get; set; }
            public double? WindowTop { get; set; }
            public bool IsFirstRun { get; set; } = true;
        public List<string> SelectedDriveDeviceIds { get; set; } = new();
        public List<string> SelectedDrivePaths { get; set; } = new();
        public Dictionary<string, List<string>> SkippedFoldersBySource { get; set; } = new();
    }
}