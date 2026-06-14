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
        /// <summary>Fired before group rows are cleared (scroll snapshot for restore).</summary>
        public event EventHandler? GroupsListRebuildStarting;

        /// <summary>Fired after shoot groups have been rebuilt from items.</summary>
        public event EventHandler? GroupsListRebuildCompleted;

        [RelayCommand]
        private void OpenImportHistory()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ShowSettingsDialog = false;
                    ShowScanExclusionsPanel = false;
                    ShowAboutDialog = false;
                    ShowImportHistoryDialog = true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "OpenImportHistory UI activation failed.");
            }
        }

        [RelayCommand]
        private void CloseImportHistory() => ShowImportHistoryDialog = false;

        [ObservableProperty] private bool showImportHistoryDialog = false;
        [ObservableProperty] private bool showSettingsDialog = false;
        [ObservableProperty] private bool showScanExclusionsPanel = false;

        [RelayCommand]
        private void ToggleSettings()
        {
            ShowScanExclusionsPanel = false;
            ShowImportHistoryDialog = false;
            ShowSettingsDialog = true;
        }

        [RelayCommand]
        private void OpenScanExclusions()
        {
            ShowSettingsDialog = false;
            ShowImportHistoryDialog = false;
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
                Title = AppLocalizer.Get("Sidebar_Section_Import"),
                IsExpanded = true,
                Options =
                {
                    new SidebarOption { Label = AppLocalizer.Get("Sidebar_StartImport"), Command = ImportCommand },
                    new SidebarOption { Label = AppLocalizer.Get("Btn_ImportHistory"), Command = OpenImportHistoryCommand },
                    new SidebarOption { Label = AppLocalizer.Get("Toolbar_SelectAll"), Command = SelectAllCommand },
                    new SidebarOption { Label = AppLocalizer.Get("Sidebar_DeselectAll"), Command = new RelayCommand(DeselectAllShoots) },
                }
            });
            SidebarSections.Add(new SidebarSection
            {
                Title = AppLocalizer.Get("Sidebar_Sources"),
                Options =
                {
                    new SidebarOption { Label = AppLocalizer.Get("Btn_AddFtpSource"), Command = ToggleAddFtpCommand },
                    new SidebarOption { Label = AppLocalizer.Get("Sidebar_RescanDrives"), Command = RescanCommand },
                    new SidebarOption { Label = AppLocalizer.Get("Sidebar_RefreshUnified"), Command = RefreshUnifiedCommand },
                }
            });
        }

        private void ApplyLocalizedShellStrings()
        {
            StatusMessage = AppLocalizer.Get("Vm_Status_Ready");
            AlbumName = AppLocalizer.Get("Vm_DefaultAlbumName");
            ScanDialogTitle = AppLocalizer.Get("Vm_Scan_LoadingImportList");
            ScanProgressMessage = AppLocalizer.Get("Vm_Scan_PreparingScan");
            RefreshDestinationPresetLabels();
        }

        private void RefreshDestinationPresetLabels()
        {
            _refreshingDestinationPresetLabels = true;
            try
            {
                string selected = DestinationPreset;
                DestinationPresetOptions.Clear();
                DestinationPresetOptions.Add(new DestinationPresetOption("Custom", AppLocalizer.Get("DestPreset_Custom")));
                DestinationPresetOptions.Add(new DestinationPresetOption("Pictures", AppLocalizer.Get("DestPreset_Pictures")));
                DestinationPresetOptions.Add(new DestinationPresetOption("LastSession", AppLocalizer.Get("DestPreset_LastSession")));
                if (!DestinationPresetOptions.Any(o => string.Equals(o.Key, selected, StringComparison.OrdinalIgnoreCase)))
                {
                    DestinationPreset = "Custom";
                }
            }
            finally
            {
                _refreshingDestinationPresetLabels = false;
            }
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
            IDatabaseService databaseService,
            IShootFilterService shootFilterService,
            IFtpWorkflowService ftpWorkflowService,
            IUnifiedConcreteSourceScanService unifiedConcreteSourceScanService,
            IFtpCredentialStore ftpCredentialStore,
            IFtpThumbnailService ftpThumbnailService,
            IFileDialogService fileDialogService,
            IShellService shellService,
            ILogger<MainViewModel> logger)
        {
            _scanner = scanner;
            _ftpScanner = ftpScanner;
            _shootFilterService = shootFilterService;
            _ftpWorkflowService = ftpWorkflowService;
            _unifiedConcreteSourceScanService = unifiedConcreteSourceScanService;
            _ftpCredentialStore = ftpCredentialStore;
            _ftpThumbnailService = ftpThumbnailService;
            _fileDialogService = fileDialogService;
            _shellService = shellService;
            _thumbnailService = thumbnailService;
            _updateService = updateService;
            _deviceWatcher = deviceWatcher;
            _fileProviderFactory = fileProviderFactory;
            _ingestEngineFactory = ingestEngineFactory;
            _groupBuilder = groupBuilder;
            _databaseService = databaseService;
            _logger = logger;

            InitializeSidebarSections();
            InitializeIntervalOptions();
            ApplyLocalizedShellStrings();
            ImportHistoryRecords.CollectionChanged += (s, e) => OnPropertyChanged(nameof(RecentImportHistory));
            Sources.CollectionChanged += (_, _) => RefreshUxEmptyStateHints();
            Groups.CollectionChanged += (_, _) => RefreshUxEmptyStateHints();
        }

        private void InitializeIntervalOptions()
        {
            IntervalOptions.Clear();
            IntervalOptions.Add(new UpdateIntervalOption { Display = AppLocalizer.Get("Vm_UpdateInterval_Daily"), Hours = 24 });
            IntervalOptions.Add(new UpdateIntervalOption { Display = AppLocalizer.Get("Vm_UpdateInterval_Weekly"), Hours = 168 });
            IntervalOptions.Add(new UpdateIntervalOption { Display = AppLocalizer.Get("Vm_UpdateInterval_Monthly"), Hours = 720 });
            IntervalOptions.Add(new UpdateIntervalOption { Display = AppLocalizer.Get("Vm_UpdateInterval_Off"), Hours = -1 });
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
        [ObservableProperty] private string albumName = string.Empty;
        [ObservableProperty] private string statusMessage = string.Empty;
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
        [ObservableProperty] private string scanDialogTitle = string.Empty;
        [ObservableProperty] private string scanProgressMessage = string.Empty;
        [ObservableProperty] private string currentScanFolder = "/";
        [ObservableProperty] private int currentScanFolderProcessedFiles = 0;
        [ObservableProperty] private int currentScanFolderTotalFiles = 0;
        [ObservableProperty] private object? selectedSource;
        [ObservableProperty] private string destinationRoot = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "QuickMediaIngest");
        [ObservableProperty] private bool hasUnifiedFtpListingFailures;
        [ObservableProperty] private bool hasLastFtpReconnectFailure;
        [ObservableProperty] private bool showPostDeleteRecoveryBanner;

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
        /// <summary>When true, sidebar shows the narrow icon rail (persisted).</summary>
        [ObservableProperty] private bool sidebarCollapsed = false;
        /// <summary>Sidebar notifications expander (persisted).</summary>
        [ObservableProperty] private bool sidebarNotificationsExpanded = true;
        [ObservableProperty] private bool settingsPrefsDestinationExpanded = true;
    }
}
