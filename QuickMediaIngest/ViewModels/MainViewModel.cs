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
        private int _updateIntervalHours = 24; // Default
        private string _namingTemplate = "[Date]_[Time]_[Original]";
        private double _thumbnailSize = 120; // Default

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

        public int UpdateIntervalHours
        {
            get => _updateIntervalHours;
            set { _updateIntervalHours = value; OnPropertyChanged(); SaveConfig(); CheckUpdates(); }
        }

        public string NamingTemplate
        {
            get => _namingTemplate;
            set { _namingTemplate = value; OnPropertyChanged(); SaveConfig(); }
        }

        public double ThumbnailSize
        {
            get => _thumbnailSize;
            set { _thumbnailSize = value; OnPropertyChanged(); }
        }

        public string AppVersion => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        public ObservableCollection<string> Sources { get; } = new ObservableCollection<string>();
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();
        public ObservableCollection<UpdateIntervalOption> IntervalOptions { get; } = new ObservableCollection<UpdateIntervalOption>
        {
            new UpdateIntervalOption { Display = "Daily", Hours = 24 },
            new UpdateIntervalOption { Display = "Weekly", Hours = 168 },
            new UpdateIntervalOption { Display = "Monthly", Hours = 720 },
            new UpdateIntervalOption { Display = "Off", Hours = -1 }
        };
       
        public ICommand ImportCommand { get; }
        public ICommand DownloadUpdateCommand { get; }
        public ICommand ToggleAboutCommand { get; }
        public ICommand OpenGitHubCommand { get; }
        public ICommand RefreshUpdateCommand { get; }
        public ICommand BrowseDestinationCommand { get; }\r\n        public ICommand RescanCommand { get; }

        public MainViewModel()
        {
            _scanner = new LocalScanner();
            _groupBuilder = new GroupBuilder();

            ImportCommand = new RelayCommand(ExecuteImport);
            DownloadUpdateCommand = new RelayCommand(ExecuteDownloadUpdate);
            ToggleAboutCommand = new RelayCommand(() => ShowAboutDialog = !ShowAboutDialog);
            OpenGitHubCommand = new RelayCommand(() => OpenUrl("https://github.com/edwardlthompson/QuickMediaIngest"));
            RefreshUpdateCommand = new RelayCommand(() => CheckUpdates(force: true));
            BrowseDestinationCommand = new RelayCommand(ExecuteBrowseDestination);\r\n            RescanCommand = new RelayCommand(ScanDrives);

            LoadConfig();

            // 1. Scan existing drives on Startup
            try 
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady))
                {
                    Sources.Add(drive.Name);
                }
            } catch { }

            // 2. Setup watcher
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
                        if (SelectedSource == drive) SelectedSource = null;
                    }
                });
            };
            _watcher.Start();

            // 3. Check for Updates
            CheckUpdates();
        }

        private void CheckUpdates(bool force = false)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                var updater = new UpdateService();
                var url = await updater.CheckForUpdateAsync(UpdateIntervalHours, force);
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
                    await engine.IngestGroupAsync(group, DestinationRoot, NamingTemplate, cts.Token);
                    
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
                    NamingTemplate = NamingTemplate,
                    ThumbnailSize = ThumbnailSize
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
                        if (!string.IsNullOrEmpty(config.NamingTemplate)) _namingTemplate = config.NamingTemplate;
                        if (config.ThumbnailSize > 0) _thumbnailSize = config.ThumbnailSize;

                        OnPropertyChanged("UpdateIntervalHours");
                        OnPropertyChanged("DestinationRoot");
                        OnPropertyChanged("NamingTemplate");
                        OnPropertyChanged("ThumbnailSize");
                    }
                }
            } catch { }
        }

                private void ScanDrives()
        {
            try
            {
                var activeDrives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.DriveType == System.IO.DriveType.Removable && d.IsReady)
                    .Select(d => d.Name)
                    .ToList();

                for (int i = Sources.Count - 1; i >= 0; i--)
                {
                    string s = Sources[i];
                    if (s.Contains(":") && !activeDrives.Contains(s))
                    {
                        Sources.RemoveAt(i);
                        if (SelectedSource == s) SelectedSource = null;
                    }
                }

                foreach (var drive in activeDrives)
                {
                    if (!Sources.Contains(drive))
                    {
                        Sources.Add(drive);
                    }
                }
            } catch { }
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

    public class UpdateIntervalOption
    {
        public string Display { get; set; } = string.Empty;
        public int Hours { get; set; }
    }

    public class AppConfig
    {
        public int UpdateIntervalHours { get; set; } = 24;
        public string DestinationRoot { get; set; } = string.Empty;
        public string NamingTemplate { get; set; } = "[Date]_[Time]_[Original]";
        public double ThumbnailSize { get; set; } = 120;
    }
}