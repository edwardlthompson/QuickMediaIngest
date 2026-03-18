using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private readonly DeviceWatcher _watcher;
        private readonly LocalScanner _scanner;
        private readonly GroupBuilder _groupBuilder;

        public string AlbumName
        {
            get => _albumName;
            set { _albumName = value; OnPropertyChanged(); UpdateAlbumNameOnGroups(); }
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

        public ObservableCollection<string> Sources { get; } = new ObservableCollection<string>();
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();

        public ICommand ImportCommand { get; }

        public MainViewModel()
        {
            _scanner = new LocalScanner();
            _groupBuilder = new GroupBuilder();

            ImportCommand = new RelayCommand(ExecuteImport);

            // 1. Scan existing drives on Startup
            try 
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady))
                {
                    Sources.Add(drive.Name);
                }
            } catch { /* Handle air-gapped security context errors */ }

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
                    g.AlbumName = AlbumName;
                    Groups.Add(g);
                }
                StatusMessage = $"Found {groups.Count} Group(s) from {drive}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error scanning {drive}: {ex.Message}";
            }
        }

        private void UpdateAlbumNameOnGroups()
        {
            foreach (var g in Groups)
            {
                g.AlbumName = AlbumName;
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
