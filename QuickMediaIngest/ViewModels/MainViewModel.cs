using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _albumName = "New Album";
        private string _statusMessage = "Ready";
        private int _progressPercent = 0;
        private int _totalFilesForImport = 0;
        private int _currentFileBeingImported = 0;
        private int _processedFilesForImport = 0;
        private int _failedFilesForImport = 0;
        private int _currentGroupFileBeingImported = 0;
        private int _totalFilesInCurrentGroup = 0;
        private int _currentGroupProgressPercent = 0;
        private string _currentImportGroupTitle = string.Empty;
        private string _importElapsedText = "00:00:00";
        private string _importEtaText = "--:--:--";
        private string _importDataRateText = "-- MB/s";
        private DateTime _importStartedAtUtc = DateTime.MinValue;
        private long _processedBytesForImport = 0;
        private bool _showImportProgressDialog = false;
        private bool _showScanProgressDialog = false;
        private int _scanProgressPercent = 0;
        private int _totalFoldersToScan = 0;
        private int _scannedFolders = 0;
        private int _totalFilesToScan = 0;
        private int _scannedFiles = 0;
        private string _scanDialogTitle = "Loading Import List...";
        private string _scanProgressMessage = "Preparing scan...";
                        private object? _selectedSource;
        private string _destinationRoot = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "QuickMediaIngest");
        private bool _deleteAfterImport = false;
        private bool _selectAll = true;

        private bool _showUpdateBanner = false;
        private string _updateUrl = string.Empty;
        private bool _showAboutDialog = false;
        private int _updateIntervalHours = 24;
        private string _updatePackageType = "Portable";
        private string _namingTemplate = "[Date]_[Time]_[Original]";
        private double _thumbnailSize = 120; 
        private string _scanPath = string.Empty;
        private bool _scanIncludeSubfolders = true;
        private bool _isImporting = false;
        private bool _showSuccessNotification = false;
        private int _timeBetweenShootsHours = 4;
        private bool _isBrowsingFtpFolders = false;
        private string _selectedFtpPresetFolder = "/DCIM";
        private FtpFolderOption? _selectedBrowsedFtpFolder;
        private string _ftpDialogStatusMessage = "Enter your phone FTP details, then test or browse folders.";
        private bool _limitFtpThumbnailLoad = false;
        private int _ftpInitialThumbnailCount = 0;
        private bool _showSkippedFoldersDialog = false;
        private string _skippedFoldersReportTitle = "FTP Scan: Skipped Folders";
        private string _skippedFoldersReportText = string.Empty;
        private bool _isUpdatingSelectAll;
        private bool _isDarkTheme = true;

        private DeviceWatcher? _watcher;
        private bool _startupInitialized;
    private double _savedWindowWidth = 960;
    private double _savedWindowHeight = 620;
    private bool _savedWindowMaximized = false;

    private readonly LocalScanner _scanner;
        private readonly GroupBuilder _groupBuilder;
    private List<ImportItem> _currentSourceItems = new();
        private readonly Dictionary<string, List<ImportItem>> _sourceItemsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly UnifiedSourceItem _unifiedSource = new();
                public string AlbumName
        {
            get => _albumName;
            set { _albumName = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set { _progressPercent = value; OnPropertyChanged(); }
        }

        public int TotalFilesForImport
        {
            get => _totalFilesForImport;
            set { _totalFilesForImport = value; OnPropertyChanged(); }
        }

        public int CurrentFileBeingImported
        {
            get => _currentFileBeingImported;
            set { _currentFileBeingImported = value; OnPropertyChanged(); }
        }

        public int ProcessedFilesForImport
        {
            get => _processedFilesForImport;
            set { _processedFilesForImport = value; OnPropertyChanged(); }
        }

        public int FailedFilesForImport
        {
            get => _failedFilesForImport;
            set { _failedFilesForImport = value; OnPropertyChanged(); }
        }

        public int CurrentGroupFileBeingImported
        {
            get => _currentGroupFileBeingImported;
            set { _currentGroupFileBeingImported = value; OnPropertyChanged(); }
        }

        public int TotalFilesInCurrentGroup
        {
            get => _totalFilesInCurrentGroup;
            set { _totalFilesInCurrentGroup = value; OnPropertyChanged(); }
        }

        public int CurrentGroupProgressPercent
        {
            get => _currentGroupProgressPercent;
            set { _currentGroupProgressPercent = value; OnPropertyChanged(); }
        }

        public string CurrentImportGroupTitle
        {
            get => _currentImportGroupTitle;
            set { _currentImportGroupTitle = value; OnPropertyChanged(); }
        }

        public string ImportElapsedText
        {
            get => _importElapsedText;
            set { _importElapsedText = value; OnPropertyChanged(); }
        }

        public string ImportEtaText
        {
            get => _importEtaText;
            set { _importEtaText = value; OnPropertyChanged(); }
        }

        public string ImportDataRateText
        {
            get => _importDataRateText;
            set { _importDataRateText = value; OnPropertyChanged(); }
        }

        public bool ShowImportProgressDialog
        {
            get => _showImportProgressDialog;
            set { _showImportProgressDialog = value; OnPropertyChanged(); }
        }

        public bool ShowScanProgressDialog
        {
            get => _showScanProgressDialog;
            set { _showScanProgressDialog = value; OnPropertyChanged(); }
        }

        public int ScanProgressPercent
        {
            get => _scanProgressPercent;
            set { _scanProgressPercent = value; OnPropertyChanged(); }
        }

        public int TotalFoldersToScan
        {
            get => _totalFoldersToScan;
            set { _totalFoldersToScan = value; OnPropertyChanged(); }
        }

        public int ScannedFolders
        {
            get => _scannedFolders;
            set { _scannedFolders = value; OnPropertyChanged(); }
        }

        public int TotalFilesToScan
        {
            get => _totalFilesToScan;
            set { _totalFilesToScan = value; OnPropertyChanged(); }
        }

        public int ScannedFiles
        {
            get => _scannedFiles;
            set { _scannedFiles = value; OnPropertyChanged(); }
        }

        public string ScanProgressMessage
        {
            get => _scanProgressMessage;
            set { _scanProgressMessage = value; OnPropertyChanged(); }
        }

        public string ScanDialogTitle
        {
            get => _scanDialogTitle;
            set { _scanDialogTitle = value; OnPropertyChanged(); }
        }

        public object? SelectedSource
        {
            get => _selectedSource;
            set 
            { 
                _selectedSource = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(HasSelectedSource));
                OnPropertyChanged(nameof(IsLocalSourceSelected));
                OnPropertyChanged(nameof(IsFtpSourceSelected));
                OnPropertyChanged(nameof(IsUnifiedSourceSelected));

                if (_selectedSource is string drive)
                {
                    ScanPath = drive;
                }
                else if (_selectedSource is FtpSourceItem ftp)
                {
                    ScanPath = NormalizeFtpPath(ftp.RemoteFolder);
                }
                else if (_selectedSource is UnifiedSourceItem)
                {
                    ScanPath = string.Empty;
                }

                if (_selectedSource != null)
                {
                    LoadSourceItems(_selectedSource);
                }
            }
        }

        public string DestinationRoot
        {
            get => _destinationRoot;
            set { _destinationRoot = value; OnPropertyChanged(); SaveConfig(); }
        }

        public bool DeleteAfterImport
        {
            get => _deleteAfterImport;
            set { _deleteAfterImport = value; OnPropertyChanged(); SaveConfig(); }
        }

        public bool IsImporting
        {
            get => _isImporting;
            set { _isImporting = value; OnPropertyChanged(); }
        }

        public bool ShowSuccessNotification
        {
            get => _showSuccessNotification;
            set { _showSuccessNotification = value; OnPropertyChanged(); }
        }

        public bool IsBrowsingFtpFolders
        {
            get => _isBrowsingFtpFolders;
            set { _isBrowsingFtpFolders = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFtpBusy)); }
        }

        public bool IsFtpBusy => IsTestingFtp || IsBrowsingFtpFolders;

        public string FtpDialogStatusMessage
        {
            get => _ftpDialogStatusMessage;
            set { _ftpDialogStatusMessage = value; OnPropertyChanged(); }
        }

        public bool ShowSkippedFoldersDialog
        {
            get => _showSkippedFoldersDialog;
            set { _showSkippedFoldersDialog = value; OnPropertyChanged(); }
        }

        public string SkippedFoldersReportTitle
        {
            get => _skippedFoldersReportTitle;
            set { _skippedFoldersReportTitle = value; OnPropertyChanged(); }
        }

        public string SkippedFoldersReportText
        {
            get => _skippedFoldersReportText;
            set { _skippedFoldersReportText = value; OnPropertyChanged(); }
        }

        public bool LimitFtpThumbnailLoad
        {
            get => _limitFtpThumbnailLoad;
            set { _limitFtpThumbnailLoad = value; OnPropertyChanged(); SaveConfig(); }
        }

        public int FtpInitialThumbnailCount
        {
            get => _ftpInitialThumbnailCount;
            set
            {
                _ftpInitialThumbnailCount = Math.Max(20, Math.Min(2000, value));
                OnPropertyChanged();
                SaveConfig();
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

            public double SavedWindowWidth => _savedWindowWidth;
            public double SavedWindowHeight => _savedWindowHeight;
            public bool SavedWindowMaximized => _savedWindowMaximized;

            public void SaveWindowState(double width, double height, bool maximized)
            {
                _savedWindowWidth = width;
                _savedWindowHeight = height;
                _savedWindowMaximized = maximized;
                SaveConfig();
            }

        public int TimeBetweenShootsHours
        {
            get => _timeBetweenShootsHours;
            set
            {
                int clampedValue = Math.Clamp(value, 1, 24);
                if (_timeBetweenShootsHours == clampedValue)
                {
                    return;
                }

                _timeBetweenShootsHours = clampedValue;
                OnPropertyChanged();
                SaveConfig();
                RebuildGroupsFromCurrentItems();
            }
        }

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                _selectAll = value;
                OnPropertyChanged();

                if (_isUpdatingSelectAll)
                {
                    return;
                }

                foreach (var g in Groups)
                {
                    g.IsSelected = value;
                }

                SaveConfig();
            }
        }

        public bool ShowUpdateBanner
        {
            get => _showUpdateBanner;
            set { _showUpdateBanner = value; OnPropertyChanged(); }
        }

        public string UpdateUrl
        {
            get => _updateUrl;
            set { _updateUrl = value; OnPropertyChanged(); }
        }

        public bool ShowAboutDialog
        {
            get => _showAboutDialog;
            set { _showAboutDialog = value; OnPropertyChanged(); }
        }

        private bool _showAddFtpDialog = false;
        public bool ShowAddFtpDialog
        {
            get => _showAddFtpDialog;
            set { _showAddFtpDialog = value; OnPropertyChanged(); }
        }

        private bool _isTestingFtp = false;
        public bool IsTestingFtp
        {
            get => _isTestingFtp;
            set { _isTestingFtp = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsFtpBusy)); }
        }

        private string _ftpHost = "10.0.0.9";
        public string FtpHost { get => _ftpHost; set { _ftpHost = value; OnPropertyChanged(); } }

        private int _ftpPort = 1024;
        public int FtpPort { get => _ftpPort; set { _ftpPort = value; OnPropertyChanged(); } }

        private string _ftpUser = "android";
        public string FtpUser { get => _ftpUser; set { _ftpUser = value; OnPropertyChanged(); } }

        private string _ftpPass = "android";
        public string FtpPass { get => _ftpPass; set { _ftpPass = value; OnPropertyChanged(); } }

        private string _ftpRemoteFolder = "/DCIM";
        public string FtpRemoteFolder { get => _ftpRemoteFolder; set { _ftpRemoteFolder = value; OnPropertyChanged(); } }

        public string SelectedFtpPresetFolder
        {
            get => _selectedFtpPresetFolder;
            set
            {
                _selectedFtpPresetFolder = value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    FtpRemoteFolder = value;
                }
                OnPropertyChanged();
            }
        }

        public FtpFolderOption? SelectedBrowsedFtpFolder
        {
            get => _selectedBrowsedFtpFolder;
            set { _selectedBrowsedFtpFolder = value; OnPropertyChanged(); }
        }

        public ICommand ToggleAddFtpCommand { get; }
        public ICommand SaveFtpCommand { get; }
        public ICommand TestFtpConnectionCommand { get; }
        public ICommand BrowseFtpFoldersCommand { get; }
        public ICommand UseBrowsedFtpFolderCommand { get; }
        public ICommand CopySkippedFoldersReportCommand { get; }
        public ICommand CloseSkippedFoldersReportCommand { get; }

        public int UpdateIntervalHours
        {
            get => _updateIntervalHours;
            set { _updateIntervalHours = value; OnPropertyChanged(); SaveConfig(); CheckUpdates(); }
        }

        public string UpdatePackageType
        {
            get => _updatePackageType;
            set { _updatePackageType = value; OnPropertyChanged(); SaveConfig(); }
        }

        public string NamingTemplate
        {
            get => _namingTemplate;
            set { _namingTemplate = value; OnPropertyChanged(); SaveConfig(); }
        }

        public double ThumbnailSize
        {
            get => _thumbnailSize;
            set { _thumbnailSize = value; OnPropertyChanged(); SaveConfig(); }
        }

        public string ScanPath
        {
            get => _scanPath;
            set
            {
                _scanPath = value;
                if (SelectedSource is FtpSourceItem ftp)
                {
                    ftp.RemoteFolder = NormalizeFtpPath(value);
                }
                _sourceItemsCache.Clear();
                OnPropertyChanged();
                SaveConfig();
            }
        }

        public bool ScanIncludeSubfolders
        {
            get => _scanIncludeSubfolders;
            set
            {
                _scanIncludeSubfolders = value;
                _sourceItemsCache.Clear();
                OnPropertyChanged();
                SaveConfig();
            }
        }

        public bool HasSelectedSource => SelectedSource != null;
        public bool IsLocalSourceSelected => SelectedSource is string;
        public bool IsFtpSourceSelected => SelectedSource is FtpSourceItem;
        public bool IsUnifiedSourceSelected => SelectedSource is UnifiedSourceItem;

        public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (_isDarkTheme != value)
                {
                    _isDarkTheme = value;
                    OnPropertyChanged();
                    // Apply the theme when changed
                    App.ApplyTheme(!value);
                    SaveConfig();
                }
            }
        }

        public ObservableCollection<object> Sources { get; } = new ObservableCollection<object>();
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();
        public ObservableCollection<ImportHistoryRecord> ImportHistoryRecords { get; } = new ObservableCollection<ImportHistoryRecord>();
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

        public ObservableCollection<string> AvailableTokens { get; } = new ObservableCollection<string> 
        { 
            "[Date]", "[Time]", "[YYYY]", "[MM]", "[DD]", "[HH]", "[mm]", "[ss]", "[ShootName]", "[Original]", "[Ext]", "_", "-" 
        };
        
        public ObservableCollection<TokenItem> SelectedTokens { get; } = new ObservableCollection<TokenItem>();
        
                public void UpdateNamingFromTokens()
        {
            _namingTemplate = string.Join("", SelectedTokens.Select(t => t.Value));
            OnPropertyChanged("NamingTemplate");
            SaveConfig();
        }
       
        public ICommand ImportCommand { get; }
        public ICommand DownloadUpdateCommand { get; }
        public ICommand ToggleAboutCommand { get; }
        public ICommand OpenGitHubCommand { get; }
        public ICommand RefreshUpdateCommand { get; }
        public ICommand BrowseDestinationCommand { get; }
        public ICommand RescanCommand { get; }
        public ICommand BrowseScanPathCommand { get; }
        public ICommand BuildSelectedPreviewsCommand { get; }
        public ICommand SelectAllShootsCommand { get; }
        public ICommand DeselectAllShootsCommand { get; }

        public MainViewModel()
        {
            _scanner = new LocalScanner();
            _groupBuilder = new GroupBuilder();
            _isDarkTheme = App.CurrentIsDarkTheme;

            Sources.Add(_unifiedSource);

            ImportCommand = new RelayCommand(ExecuteImport);
            DownloadUpdateCommand = new RelayCommand(ExecuteDownloadUpdate);
            ToggleAboutCommand = new RelayCommand(() => ShowAboutDialog = !ShowAboutDialog);
            OpenGitHubCommand = new RelayCommand(() => OpenUrl("https://github.com/edwardlthompson/QuickMediaIngest"));
            RefreshUpdateCommand = new RelayCommand(() => CheckUpdates(force: true));
            BrowseDestinationCommand = new RelayCommand(ExecuteBrowseDestination);
            RescanCommand = new RelayCommand(ScanDrives);
            BrowseScanPathCommand = new RelayCommand(ExecuteBrowseScanPath);
            BuildSelectedPreviewsCommand = new RelayCommand(ExecuteBuildSelectedPreviews);
            SelectAllShootsCommand = new RelayCommand(() => SetAllShootsSelected(true));
            DeselectAllShootsCommand = new RelayCommand(() => SetAllShootsSelected(false));
            ToggleAddFtpCommand = new RelayCommand(() => ShowAddFtpDialog = !ShowAddFtpDialog);
            SaveFtpCommand = new RelayCommand(ExecuteSaveFtp);
            TestFtpConnectionCommand = new RelayCommand(ExecuteTestFtpConnection);
            BrowseFtpFoldersCommand = new RelayCommand(ExecuteBrowseFtpFolders);
            UseBrowsedFtpFolderCommand = new RelayCommand(ExecuteUseBrowsedFtpFolder);
            CopySkippedFoldersReportCommand = new RelayCommand(ExecuteCopySkippedFoldersReport);
            CloseSkippedFoldersReportCommand = new RelayCommand(ExecuteCloseSkippedFoldersReport);
        }

        public async Task InitializeAsync()
        {
            if (_startupInitialized)
            {
                return;
            }

            _startupInitialized = true;
            await Task.Yield();

            LoadConfig();
            LoadImportHistory();
            ScanDrives();

            if (_watcher == null)
            {
                _watcher = new DeviceWatcher();
                _watcher.DeviceConnected += (drive) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!Sources.Contains(drive)) Sources.Add(drive);
                    });
                };
                _watcher.DeviceDisconnected += (drive) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Sources.Contains(drive))
                        {
                            Sources.Remove(drive);
                            if (SelectedSource is string selectedDrive && string.Equals(selectedDrive, drive, StringComparison.Ordinal))
                            {
                                SelectedSource = null;
                            }
                        }
                    });
                };
                _watcher.Start();
            }

            // Run update check shortly after startup work finishes.
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                CheckUpdates();
            });
        }

        private void CheckUpdates(bool force = false)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                var updater = new UpdateService();
                var url = await updater.CheckForUpdateAsync(UpdateIntervalHours, force, UpdatePackageType);
                if (!string.IsNullOrEmpty(url))
                {
                     Application.Current.Dispatcher.Invoke(() =>
                     {
                         UpdateUrl = url;
                         ShowUpdateBanner = true;
                     });
                }
                else if (force)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                         StatusMessage = "No updates found. App is up to date.";
                    });
                }
            });
        }

        private async void ExecuteDownloadUpdate()
        {
            if (string.IsNullOrEmpty(UpdateUrl)) return;

            if (UpdateUrl.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Downloading update installer (Auto)...";
                ShowUpdateBanner = false;

                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "QuickMediaIngest_Update.msi");

                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = await client.GetAsync(UpdateUrl);
                        using (var fs = new FileStream(tempPath, FileMode.Create))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }

                    StatusMessage = "Installing update... App will close for restart.";
                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Update download failed: {ex.Message}";
                    ShowUpdateBanner = true;
                }
            }
            else if (UpdateUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                StatusMessage = "Downloading update...";
                ShowUpdateBanner = false;

                try
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), "QuickMediaIngest_Update.exe");

                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = await client.GetAsync(UpdateUrl);
                        using (var fs = new FileStream(tempPath, FileMode.Create))
                        {
                            await response.Content.CopyToAsync(fs);
                        }
                    }

                    StatusMessage = "Launching updated version... App will close.";
                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Update download failed: {ex.Message}";
                    ShowUpdateBanner = true;
                }
            }
            else
            {
                OpenUrl(UpdateUrl);
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
                StatusMessage = "Enter an FTP host before testing.";
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = "FTP port must be between 1 and 65535.";
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(FtpRemoteFolder);
            IsTestingFtp = true;
            StatusMessage = $"Testing FTP connection to {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var result = await Task.Run(async () =>
                    await new FtpScanner().TestConnectionAsync(
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
                StatusMessage = "Enter an FTP host before browsing folders.";
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            if (FtpPort <= 0 || FtpPort > 65535)
            {
                StatusMessage = "FTP port must be between 1 and 65535.";
                FtpDialogStatusMessage = StatusMessage;
                return;
            }

            string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(FtpRemoteFolder) ? "/DCIM" : FtpRemoteFolder);
            IsBrowsingFtpFolders = true;
            StatusMessage = $"Browsing FTP folders at {FtpHost}:{FtpPort}{remotePath}...";
            FtpDialogStatusMessage = StatusMessage;

            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                var folders = await Task.Run(async () =>
                    await new FtpScanner().ListDirectoriesAsync(
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
                StatusMessage = $"Connected to FTP, but browsing {remotePath} timed out. Try /DCIM or /DCIM/Camera.";
                FtpDialogStatusMessage = StatusMessage;
            }
            catch (Exception ex)
            {
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
                StatusMessage = "Choose a browsed FTP folder first.";
                return;
            }

            FtpRemoteFolder = SelectedBrowsedFtpFolder.Path;
            SelectedFtpPresetFolder = SelectedBrowsedFtpFolder.Path;
            StatusMessage = $"FTP folder selected: {SelectedBrowsedFtpFolder.Path}";
            FtpDialogStatusMessage = StatusMessage;
        }

        private async void LoadSourceItems(object source)
        {
            foreach (var existing in Groups)
            {
                existing.PropertyChanged -= Group_PropertyChanged;
            }
            Groups.Clear();

            if (source is UnifiedSourceItem)
            {
                await LoadUnifiedSourceItemsAsync();
                return;
            }

            string sourceLabel = source.ToString() ?? "source";
            var skippedFolderDetails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string sourceKey = string.Empty;
            try 
            {
                List<QuickMediaIngest.Core.Models.ImportItem> items;
                ShowScanProgressDialog = true;
                ScanDialogTitle = "Loading Import List...";
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = 0;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = "Preparing scan...";

                if (source is FtpSourceItem ftp)
                {
                    string remotePath = NormalizeFtpPath(string.IsNullOrWhiteSpace(ScanPath) ? ftp.RemoteFolder : ScanPath);
                    ftp.RemoteFolder = remotePath;
                    sourceLabel = $"{ftp.Host}{remotePath}";
                    sourceKey = BuildSourceKey(ftp);

                    if (_sourceItemsCache.TryGetValue(sourceKey, out var cachedFtpItems))
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

                    items = await new FtpScanner().ScanAsync(
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
                                    skippedFolderDetails.Add($"{progress.CurrentFolder} - {progress.Note}");
                                }
                            });
                        });
                }
                else if (source is string drive)
                {
                    string localPath = ResolveLocalScanPath(drive, ScanPath);
                    sourceLabel = localPath;
                    sourceKey = BuildSourceKey(localPath);

                    if (_sourceItemsCache.TryGetValue(sourceKey, out var cachedLocalItems))
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
                        });
                    }));
                }
                else return;

BuildGroups:
                StampItems(items, sourceKey, source is FtpSourceItem);
                _sourceItemsCache[sourceKey] = CloneItems(items);

                _currentSourceItems = items;
                RebuildGroupsFromCurrentItems();

                if (Groups.Count > 0)
                {
                    await LoadThumbnailsAsync(Groups.ToList(), source, sourceLabel);
                }
                else
                {
                    StatusMessage = $"Found 0 group(s) from {sourceLabel}.";
                }

                if (source is FtpSourceItem && skippedFolderDetails.Count > 0)
                {
                    ShowSkippedFoldersReport(sourceLabel, skippedFolderDetails);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = $"FTP scan was canceled while scanning {sourceLabel}.";
            }
            catch (Exception ex)
            {
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

        private void Group_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ItemGroup.IsSelected))
            {
                UpdateSelectAllFromGroups();
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

            Groups.Clear();

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
                group.FolderPath = Path.GetDirectoryName(group.Items[0].SourcePath) ?? string.Empty;
                group.SyncSelectionFromItems();
                group.PropertyChanged += Group_PropertyChanged;
                Groups.Add(group);
            }

            UpdateSelectAllFromGroups();
            StatusMessage = $"Updated folder separation to {TimeBetweenShootsHours} hour{(TimeBetweenShootsHours == 1 ? string.Empty : "s")}.";
        }

        private void ShowSkippedFoldersReport(string sourceLabel, HashSet<string> skippedFolderDetails)
        {
            var details = skippedFolderDetails.OrderBy(s => s).ToList();
            int maxToShow = 15;
            var lines = details.Take(maxToShow).ToList();
            string remainingText = details.Count > maxToShow
                ? $"\n...and {details.Count - maxToShow} more."
                : string.Empty;

            string message =
                $"FTP scan completed for {sourceLabel}, but some folders were skipped due to listing errors.\n\n" +
                string.Join("\n", lines) +
                remainingText +
                "\n\nTip: Retry the scan. If the same folder keeps failing, scan that folder directly to verify server behavior.";

            StatusMessage = $"FTP scan completed with {details.Count} skipped folder(s).";
            SkippedFoldersReportTitle = $"FTP Scan: Skipped Folders ({details.Count})";
            SkippedFoldersReportText = message;
            ShowSkippedFoldersDialog = true;
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
                StatusMessage = "Skipped-folder report copied to clipboard.";
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
                var thumbService = new ThumbnailService();
                var allItems = groups.SelectMany(g => g.Items).Where(i => !i.IsVideo).ToList();
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

                // Load up to 4 thumbnails in parallel — safely bounded for SD card I/O.
                Parallel.ForEach(allItems, new ParallelOptions { MaxDegreeOfParallelism = 4 }, item =>
                {
                    var thumb = thumbService.GetThumbnail(item.SourcePath);
                    int c = Interlocked.Increment(ref current);

                    Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (thumb != null) item.Thumbnail = thumb;
                        ScannedFiles = c;
                        ScanProgressPercent = total > 0 ? (c * 100) / total : 0;
                        ScanProgressMessage = $"Loading previews: {c}/{total}";
                    });
                });
            });

            StatusMessage = $"Scanning {sourceLabel} complete. Loaded previews automatically.";
        }

        private async Task LoadFtpThumbnailsAsync(List<ItemGroup> groups, FtpSourceItem ftp, string sourceLabel, bool preferBackgroundBatch = true)
        {
            var allItems = groups
                .SelectMany(g => g.Items)
                .Where(i => !i.IsVideo)
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

            var thumbService = new ThumbnailService();
            int loadedCount = 0;
            int skippedCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int overallIndex = startIndex + i + 1;

                if (overallIndex == 1 || overallIndex % 10 == 0 || overallIndex == totalItemCount)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        ScannedFiles = overallIndex;
                        TotalFilesToScan = totalItemCount;
                        ScanProgressPercent = totalItemCount > 0 ? (overallIndex * 100) / totalItemCount : 0;
                        if (updateScanProgressMessage)
                        {
                            ScanProgressMessage = $"Loading FTP previews: {overallIndex}/{totalItemCount}";
                        }
                    });
                }

                string ext = Path.GetExtension(item.FileName);
                if (string.IsNullOrWhiteSpace(ext))
                {
                    ext = ".jpg";
                }

                string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                try
                {
                    bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, 30);
                    if (!downloaded)
                    {
                        skippedCount++;
                        continue;
                    }

                    var thumb = await Task.Run(() => thumbService.GetThumbnail(tempPath));
                    if (thumb != null)
                    {
                        loadedCount++;
                        await Application.Current.Dispatcher.InvokeAsync(() => item.Thumbnail = thumb);
                    }
                }
                catch
                {
                    skippedCount++;
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
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (skippedCount > 0)
                {
                    ScanProgressMessage = $"Loading FTP previews: {Math.Min(totalItemCount, startIndex + items.Count)}/{totalItemCount} (skipped {skippedCount} stalled/invalid file(s))";
                }
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

        private async Task LoadUnifiedSourceItemsAsync()
        {
            var skippedFolderDetails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var concreteSources = Sources
                .Where(s => s is string || s is FtpSourceItem)
                .ToList();

            if (concreteSources.Count == 0)
            {
                _currentSourceItems = new List<ImportItem>();
                StatusMessage = "No SD card or FTP sources available for Unified view.";
                return;
            }

            try
            {
                ShowScanProgressDialog = true;
                ScanDialogTitle = "Loading Unified Import List...";
                ScanProgressPercent = 0;
                ScannedFolders = 0;
                TotalFoldersToScan = concreteSources.Count;
                ScannedFiles = 0;
                TotalFilesToScan = 0;
                ScanProgressMessage = "Merging SD and FTP sources...";

                var results = new List<List<ImportItem>>();

                foreach (var src in concreteSources)
                {
                    List<ImportItem> sourceItems;

                    if (src is string drive)
                    {
                        string localPath = drive;
                        string localKey = BuildSourceKey(localPath);

                        if (_sourceItemsCache.TryGetValue(localKey, out var cachedLocal))
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
                                sourceItems = await Task.Run(() => _scanner.Scan(localPath, ScanIncludeSubfolders));
                            }

                            StampItems(sourceItems, localKey, false);
                            _sourceItemsCache[localKey] = CloneItems(sourceItems);
                        }
                    }
                    else if (src is FtpSourceItem ftp)
                    {
                        string ftpKey = BuildSourceKey(ftp);

                        if (_sourceItemsCache.TryGetValue(ftpKey, out var cachedFtp))
                        {
                            sourceItems = CloneItems(cachedFtp);
                        }
                        else
                        {
                            sourceItems = await new FtpScanner().ScanAsync(
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
                                        lock (skippedFolderDetails)
                                        {
                                            skippedFolderDetails.Add($"{progress.CurrentFolder} - {progress.Note}");
                                        }
                                    }
                                });

                            StampItems(sourceItems, ftpKey, true);
                            _sourceItemsCache[ftpKey] = CloneItems(sourceItems);
                        }
                    }
                    else
                    {
                        sourceItems = new List<ImportItem>();
                    }

                    results.Add(sourceItems);
                    ScannedFolders++;
                    ScannedFiles = results.Sum(r => r.Count);
                    TotalFilesToScan = ScannedFiles;
                    ScanProgressPercent = TotalFoldersToScan > 0 ? (ScannedFolders * 100) / TotalFoldersToScan : 0;
                    ScanProgressMessage = $"Merged {ScannedFolders}/{TotalFoldersToScan} sources...";
                }

                var unifiedItems = results.SelectMany(r => r).ToList();

                _currentSourceItems = unifiedItems;
                RebuildGroupsFromCurrentItems();

                if (Groups.Count > 0)
                {
                    await LoadThumbnailsAsync(Groups.ToList(), _unifiedSource, "Unified");
                }
                else
                {
                    StatusMessage = "Unified view contains no media files.";
                }

                if (skippedFolderDetails.Count > 0)
                {
                    ShowSkippedFoldersReport("Unified", skippedFolderDetails);
                }
            }
            catch (Exception ex)
            {
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
                .Where(i => !i.IsVideo)
                .ToList();

            if (allItems.Count == 0)
            {
                StatusMessage = $"Scanning {sourceLabel} complete. No previewable images found.";
                return;
            }

            int total = allItems.Count;
            int processed = 0;

            var ftpSourcesByKey = Sources
                .OfType<FtpSourceItem>()
                .ToDictionary(BuildSourceKey, ftp => ftp, StringComparer.OrdinalIgnoreCase);

            var thumbService = new ThumbnailService();
            string tempDir = Path.Combine(Path.GetTempPath(), "QuickMediaIngest", "ftp-thumbs");
            Directory.CreateDirectory(tempDir);

            foreach (var item in allItems)
            {
                if (item.IsFtpSource)
                {
                    if (ftpSourcesByKey.TryGetValue(item.SourceId, out var ftp))
                    {
                        string ext = Path.GetExtension(item.FileName);
                        if (string.IsNullOrWhiteSpace(ext))
                        {
                            ext = ".jpg";
                        }

                        string tempPath = Path.Combine(tempDir, $"{Guid.NewGuid():N}{ext}");

                        try
                        {
                            bool downloaded = await DownloadFtpFileWithTimeoutAsync(ftp, item.SourcePath, tempPath, 30);
                            if (downloaded)
                            {
                                var thumb = await Task.Run(() => thumbService.GetThumbnail(tempPath));
                                if (thumb != null)
                                {
                                    item.Thumbnail = thumb;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore thumbnail failures in unified mode.
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
                    }
                }
                else
                {
                    var thumb = await Task.Run(() => thumbService.GetThumbnail(item.SourcePath));
                    if (thumb != null)
                    {
                        item.Thumbnail = thumb;
                    }
                }

                processed++;
                ScannedFiles = processed;
                TotalFilesToScan = total;
                ScanProgressPercent = total > 0 ? (processed * 100) / total : 0;
                ScanProgressMessage = $"Loading Unified previews: {processed}/{total}";
            }

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
                IsSelected = i.IsSelected
            }).ToList();
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
                StatusMessage = "Scan a source first, then build previews.";
                return;
            }

            var selectedGroups = Groups.Where(g => g.IsSelected).ToList();
            if (selectedGroups.Count == 0)
            {
                StatusMessage = "Select at least one folder group first.";
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
                TotalFilesToScan = selectedGroups.SelectMany(g => g.Items).Count(i => !i.IsVideo);
                ScanProgressMessage = $"Building previews for {selectedGroups.Count} selected folder(s)...";
                StatusMessage = "Building previews for selected folders...";

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
                StatusMessage = "Nothing to import.";
                return;
            }

            var selectedGroups = Groups.Where(g => g.Items.Any(i => i.IsSelected)).ToList();
            int totalFiles = selectedGroups.Sum(g => g.Items.Count(i => i.IsSelected));
            if (totalFiles == 0)
            {
                StatusMessage = "No files selected to import.";
                return;
            }

            IsImporting = true;
            TotalFilesForImport = totalFiles;
            CurrentFileBeingImported = 0;
            ProcessedFilesForImport = 0;
            FailedFilesForImport = 0;
            CurrentGroupFileBeingImported = 0;
            TotalFilesInCurrentGroup = 0;
            CurrentGroupProgressPercent = 0;
            CurrentImportGroupTitle = string.Empty;
            ImportElapsedText = "00:00:00";
            ImportEtaText = "--:--:--";
            ImportDataRateText = "-- MB/s";
            ShowImportProgressDialog = true;
            StatusMessage = "Starting Import...";
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
                    IFileProvider provider = new LocalFileProvider();
                    if (SelectedSource is FtpSourceItem ftp)
                    {
                        provider = new FtpFileProvider(ftp.Host, ftp.Port, ftp.User, ftp.Pass);
                    }

                    try
                    {
                        var engine = new IngestEngine(provider);

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
                                await engine.IngestGroupAsync(group, DestinationRoot, NamingTemplate, importCts.Token);

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

                // Show success notification
                StatusMessage = FailedFilesForImport > 0
                    ? $"✓ Import completed with warnings. Imported {CurrentFileBeingImported}/{TotalFilesForImport}, failed {FailedFilesForImport}."
                    : $"✓ Import completed successfully! Imported {CurrentFileBeingImported}/{TotalFilesForImport}.";
                ProgressPercent = 100;
                CurrentGroupProgressPercent = 100;
                ImportEtaText = "00:00:00";
                if (stopwatch.Elapsed.TotalSeconds > 0.25 && _processedBytesForImport > 0)
                {
                    double bytesPerSecond = _processedBytesForImport / stopwatch.Elapsed.TotalSeconds;
                    ImportDataRateText = $"{(bytesPerSecond / (1024d * 1024d)):0.00} MB/s";
                }
                ShowSuccessNotification = true;

                SaveImportHistoryRecord(stopwatch.Elapsed);
                
                // Hide progress dialog after a brief moment to show completion
                await System.Threading.Tasks.Task.Delay(1000);
                ShowImportProgressDialog = false;

                // Auto-hide success notification after 3 seconds
                _ = System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ShowSuccessNotification = false;
                    });
                });

                // Clear the imported groups and refresh scan to show updated/deleted state
                Groups.Clear();
                _sourceItemsCache.Clear();
                if (SelectedSource != null)
                {
                    LoadSourceItems(SelectedSource);
                }
            }
            catch (Exception ex)
            {
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
                SystemSounds.Asterisk.Play();
                IsImporting = false;
                ShowImportProgressDialog = false;
            }
        }

        private async Task ExecuteUnifiedImportAsync(List<ItemGroup> selectedGroups, CancellationToken cancellationToken)
        {
            IFileProvider localProvider = new LocalFileProvider();
            var ftpProviders = new Dictionary<string, FtpFileProvider>(StringComparer.OrdinalIgnoreCase);
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
                            ftpProvider = new FtpFileProvider(ftpSource.Host, ftpSource.Port, ftpSource.User, ftpSource.Pass);
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
                    await provider.DisposeAsync();
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
                Items = items
            };

            var engine = new IngestEngine(provider);
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

            await engine.IngestGroupAsync(subsetGroup, DestinationRoot, NamingTemplate, cancellationToken);

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

        private void ExecuteBrowseDestination()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Destination Folder",
                InitialDirectory = DestinationRoot
            };
            
            if (dialog.ShowDialog() == true)
            {
                DestinationRoot = dialog.FolderName;
                SaveConfig();
            }
        }

        private void ExecuteBrowseScanPath()
        {
            if (SelectedSource is not string localRoot)
            {
                StatusMessage = "Browse is available for local sources. For FTP, type a remote path (for example: /DCIM/Camera).";
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

        private void SaveConfig()
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
                    NamingTemplate = NamingTemplate,
                    ScanPath = ScanPath,
                    SelectAll = SelectAll,
                    IsDarkTheme = IsDarkTheme,
                    ThumbnailSize = ThumbnailSize,
                    ScanIncludeSubfolders = ScanIncludeSubfolders,
                    TimeBetweenShootsHours = TimeBetweenShootsHours,
                    LimitFtpThumbnailLoad = LimitFtpThumbnailLoad,
                    FtpInitialThumbnailCount = FtpInitialThumbnailCount,
                    RibbonTileOrder = _ribbonTileOrder.Count > 0 ? _ribbonTileOrder : null,
                    UpdatePackageType = UpdatePackageType,
                    WindowWidth = _savedWindowWidth,
                    WindowHeight = _savedWindowHeight,
                    WindowMaximized = _savedWindowMaximized
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
                        _updateIntervalHours = config.UpdateIntervalHours;
                        if (!string.IsNullOrEmpty(config.DestinationRoot)) _destinationRoot = config.DestinationRoot;
                        _deleteAfterImport = config.DeleteAfterImport;
                        if (!string.IsNullOrEmpty(config.NamingTemplate)) _namingTemplate = config.NamingTemplate;
                        if (!string.IsNullOrWhiteSpace(config.ScanPath)) _scanPath = config.ScanPath;
                        _selectAll = config.SelectAll;
                        if (config.IsDarkTheme.HasValue)
                        {
                            _isDarkTheme = config.IsDarkTheme.Value;
                            App.ApplyTheme(!_isDarkTheme);
                        }
                        if (config.ThumbnailSize > 0) _thumbnailSize = config.ThumbnailSize;
                        _scanIncludeSubfolders = config.ScanIncludeSubfolders;
                        _timeBetweenShootsHours = Math.Clamp(config.TimeBetweenShootsHours <= 0 ? 4 : config.TimeBetweenShootsHours, 1, 24);
                        _limitFtpThumbnailLoad = false;
                        _ftpInitialThumbnailCount = 0;
                        if (config.RibbonTileOrder is { Count: > 0 })
                            _ribbonTileOrder = config.RibbonTileOrder;
                        if (!string.IsNullOrEmpty(config.UpdatePackageType)) _updatePackageType = config.UpdatePackageType;
                            if (config.WindowWidth >= 400) _savedWindowWidth = config.WindowWidth;
                            if (config.WindowHeight >= 300) _savedWindowHeight = config.WindowHeight;
                            _savedWindowMaximized = config.WindowMaximized;

                        OnPropertyChanged("UpdateIntervalHours");
                        OnPropertyChanged("UpdatePackageType");
                        OnPropertyChanged("DestinationRoot");
                        OnPropertyChanged("DeleteAfterImport");
                        OnPropertyChanged("NamingTemplate");
                        OnPropertyChanged("ScanPath");
                        OnPropertyChanged("SelectAll");
                        OnPropertyChanged("IsDarkTheme");
                        OnPropertyChanged("ThumbnailSize");
                        OnPropertyChanged("ScanIncludeSubfolders");
                        OnPropertyChanged("TimeBetweenShootsHours");
                        OnPropertyChanged("LimitFtpThumbnailLoad");
                        OnPropertyChanged("FtpInitialThumbnailCount");

                        // Parse NamingTemplate to SelectedTokens
                        Application.Current.Dispatcher.Invoke(() => {
                            SelectedTokens.Clear();
                            if (!string.IsNullOrEmpty(_namingTemplate))
                            {
                                var matches = System.Text.RegularExpressions.Regex.Matches(_namingTemplate, @"\[[^\]]+\]|[^\[\]]+");
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

                private void ScanDrives()
        {
            try
            {
                        _sourceItemsCache.Clear();

                var activeDrives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.DriveType == System.IO.DriveType.Removable && d.IsReady)
                    .Select(d => d.Name)
                    .ToList();

                                for (int i = Sources.Count - 1; i >= 0; i--)
                {
                    if (Sources[i] is string s)
                    {
                        if (s.Contains(":") && !activeDrives.Contains(s))
                        {
                            Sources.RemoveAt(i);
                            if (SelectedSource as string == s) SelectedSource = null;
                        }
                    }
                }

                foreach (var drive in activeDrives)
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
            } catch { }
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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

    public class TokenItem
    {
        public string Value { get; set; } = string.Empty;
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

    public class AppConfig
    {
        public int UpdateIntervalHours { get; set; } = 24;
        public string UpdatePackageType { get; set; } = "Portable";
        public string DestinationRoot { get; set; } = string.Empty;
        public bool DeleteAfterImport { get; set; } = false;
        public string NamingTemplate { get; set; } = "[Date]_[Time]_[Original]";
        public string ScanPath { get; set; } = string.Empty;
        public bool SelectAll { get; set; } = true;
        public bool? IsDarkTheme { get; set; }
        public double ThumbnailSize { get; set; } = 120;
        public bool ScanIncludeSubfolders { get; set; } = true;
        public int TimeBetweenShootsHours { get; set; } = 4;
        public bool LimitFtpThumbnailLoad { get; set; } = false;
        public int FtpInitialThumbnailCount { get; set; } = 0;
        public List<string>? RibbonTileOrder { get; set; }
            public double WindowWidth { get; set; } = 960;
            public double WindowHeight { get; set; } = 620;
            public bool WindowMaximized { get; set; } = false;
    }
}