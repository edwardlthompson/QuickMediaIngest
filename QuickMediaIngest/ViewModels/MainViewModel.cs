using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        // List of groups grouping the items
        public ObservableCollection<ItemGroup> Groups { get; set; } = new ObservableCollection<ItemGroup>();

        public ICommand ImportCommand { get; }

        public MainViewModel()
        {
            ImportCommand = new RelayCommand(ExecuteImport);
            LoadMockData();
        }

        private void LoadMockData()
        {
            // Populate mock data for visual preview when booted
            var group = new ItemGroup 
            { 
                Title = "Shoot 1 (SD Card)", 
                StartDate = DateTime.Now.AddDays(-1), 
                EndDate = DateTime.Now,
                AlbumName = "Vacation"
            };

            group.Items.Add(new ImportItem { FileName = "IMG_001.JPG", FileSize = 4050000, DateTaken = DateTime.Now });
            group.Items.Add(new ImportItem { FileName = "IMG_002.CR2", FileSize = 24050000, DateTaken = DateTime.Now, FileType = "CR2" });

            Groups.Add(group);
        }

        private async void ExecuteImport()
        {
            StatusMessage = "Starting Ingestion...";
            ProgressPercent = 10;
            // Wire logic to IngestEngine here
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // Standard RelayCommand helper for WPF
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}
