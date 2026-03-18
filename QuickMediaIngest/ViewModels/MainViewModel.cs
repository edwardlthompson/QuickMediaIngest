using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private string? _selectedSource;
        private string _destinationRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "QuickMediaIngest");
        private bool _deleteAfterImport = false;
        private bool _selectAll = true;

        private bool _showUpdateBanner = false;
        private string _updateUrl = string.Empty;
        private bool _showAboutDialog = false;

        private readonly DeviceWatcher _watcher;
        private readonly LocalScanner _scanner;
        private readonly GroupBuilder _groupBuilder;

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

        public string? SelectedSource
        {
            get => _selectedSource;
            set 
            { 
                _selectedSource = value; 
                OnPropertyChanged(); 
                if (!string.IsNullOrEmpty(_selectedSource)) 
                    LoadSourceItems(_selectedSource); 
            }
        }

        public string DestinationRoot
        {
            get => _destinationRoot;
            set { _destinationRoot = value; OnPropertyChanged(); }
        }

        public bool DeleteAfterImport
        {
            get => _deleteAfterImport;
            set { _deleteAfterImport = value; OnPropertyChanged(); }
        }

        public bool SelectAll
        {
            get => _selectAll;
            set
            {
                _selectAll = value;
                OnPropertyChanged();
                foreach (var g in Groups)
                {
                    g.IsSelected = value;
                }
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

        public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        public ObservableCollection<string> Sources { get; } = new ObservableCollection<string>();
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();

        public ICommand ImportCommand { get; }
        public ICommand DownloadUpdateCommand { get; }
        public ICommand ToggleAboutCommand { get; }
        public ICommand OpenGitHubCommand { get; }

        public MainViewModel()
        {
            _scanner = new LocalScanner();
            _groupBuilder = new GroupBuilder();

            ImportCommand = new RelayCommand(ExecuteImport);
            DownloadUpdateCommand = new RelayCommand(ExecuteDownloadUpdate);
            ToggleAboutCommand = new RelayCommand(() => ShowAboutDialog = !ShowAboutDialog);
            OpenGitHubCommand = new RelayCommand(() => OpenUrl("https://github.com/edwardlthompson/QuickMediaIngest"));

            // 1. Scan existing drives on Startup
            try 
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady))
                {
                    Sources.Add(drive.Name);
                }
            } catch { /* Suppress Context airgapped issues */ }

            // 2. Setup watcher
            _watcher = new DeviceWatcher();
            _watcher.DeviceConnected += (drive) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!Sources.Contains(drive))
                    {
                        Sources.Add(drive);
                    }
                });
            };
            _watcher.Start();

            // 3. Check for Updates
            CheckUpdates();
        }

        private void CheckUpdates()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                var updater = new UpdateService();
                var url = await updater.CheckForUpdateAsync();
                if (!string.IsNullOrEmpty(url))
                {
                     Application.Current.Dispatcher.Invoke(() =>
                     {
                         UpdateUrl = url;
                         ShowUpdateBanner = true;
                     });
                }
            });
        }

        private void ExecuteDownloadUpdate()
        {
            if (!string.IsNullOrEmpty(UpdateUrl))
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

        private void LoadSourceItems(string drive)
        {
            StatusMessage = $"Scanning {drive}...";
            Groups.Clear();

            try 
            {
                var items = _scanner.Scan(drive);
                var groups = _groupBuilder.BuildGroups(items, TimeSpan.FromHours(2));

                foreach (var g in groups)
                {
                    if (g.Items.Count == 0) continue;

                    g.AlbumName = AlbumName;
                    if (g.Items.Count > 0)
                    {
                        g.FolderPath = Path.GetDirectoryName(g.Items[0].SourcePath) ?? string.Empty;
                    }
                    Groups.Add(g);
                }

                StatusMessage = $"Found {groups.Count} Group(s) from {drive}. Core parsing images...";

                System.Threading.Tasks.Task.Run(() =>
                {
                    var thumbService = new ThumbnailService();
                    foreach (var g in groups)
                    {
                        foreach (var item in g.Items)
                        {
                            var thumb = thumbService.GetThumbnail(item.SourcePath);
                            if (thumb != null)
                            {
                                item.Thumbnail = thumb; 
                            }
                        }
                    }
                    Application.Current.Dispatcher.Invoke(() => StatusMessage = $"Scanning {drive} Complete. Loaded thumbs.");
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error scanning {drive}: {ex.Message}";
            }
        }

        private async void ExecuteImport()
        {
            if (Groups.Count == 0)
            {
                StatusMessage = "Nothing to import.";
                return;
            }

            StatusMessage = "Starting Import...";
            ProgressPercent = 0;

            var engine = new IngestEngine(new LocalFileProvider());
            engine.ProgressChanged += (percent, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ProgressPercent = percent;
                    StatusMessage = msg;
                });
            };

            var cts = new CancellationTokenSource();

            await System.Threading.Tasks.Task.Run(async () =>
            {
                foreach (var group in Groups.ToList())
                {
                    await engine.IngestGroupAsync(group, DestinationRoot, cts.Token);
                    
                    if (DeleteAfterImport)
                    {
                        foreach (var item in group.Items.Where(i => i.IsSelected))
                        {
                            try 
                            {
                                if (File.Exists(item.SourcePath))
                                {
                                    File.Delete(item.SourcePath);
                                }
                            } catch { }
                        }
                    }
                }
            });

            StatusMessage = "Import Completed!";
            ProgressPercent = 100;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}
