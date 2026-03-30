using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        // Stub for DestinationRoot property
        public string DestinationRoot { get; set; } = string.Empty;

        // Stub for SaveConfig method
        public void SaveConfig() { }

        // Stub for ShowSettingsDialog property (bool)
        public bool ShowSettingsDialog { get; set; }

        // Stub for InitializeAsync method
        public System.Threading.Tasks.Task InitializeAsync() => System.Threading.Tasks.Task.CompletedTask;
        // Stub for UpdateNamingFromTokens method
        public void UpdateNamingFromTokens() { }
        // Stub for RemoveTokenCommand
        public System.Windows.Input.ICommand? RemoveTokenCommand { get; set; }

        // Stub for FtpPass property
        public string FtpPass { get; set; } = string.Empty;

        // Stub for IsDarkTheme property
        public bool IsDarkTheme { get; set; }
        // Minimal valid property for build
        private double _thumbnailSize;
        private int _groupingHours;
        private System.Windows.Threading.DispatcherTimer? _thumbnailDebounceTimer;
        private System.Windows.Threading.DispatcherTimer? _groupingDebounceTimer;

        public double ThumbnailSize
        {
            get => _thumbnailSize;
            set
            {
                if (_thumbnailSize != value)
                {
                    _thumbnailDebounceTimer?.Stop();
                    _thumbnailDebounceTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
                    _thumbnailDebounceTimer.Tick += (s, e) =>
                    {
                        _thumbnailDebounceTimer.Stop();
                        SetProperty(ref _thumbnailSize, value, nameof(ThumbnailSize));
                    };
                    _thumbnailDebounceTimer.Start();
                }
            }
        }

        public int GroupingHours
        {
            get => _groupingHours;
            set
            {
                if (_groupingHours != value)
                {
                    _groupingDebounceTimer?.Stop();
                    _groupingDebounceTimer = new System.Windows.Threading.DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(250) };
                    _groupingDebounceTimer.Tick += (s, e) =>
                    {
                        _groupingDebounceTimer.Stop();
                        SetProperty(ref _groupingHours, value, nameof(GroupingHours));
                    };
                    _groupingDebounceTimer.Start();
                }
            }
        }

        // Stub for ClearImportHistoryCommand
        public System.Windows.Input.ICommand? ClearImportHistoryCommand { get; set; }

        // Stub for SelectAllVisible method
        public void SelectAllVisible() { }

        // Stub for SelectedTokens property
        public IList<TokenItem> SelectedTokens { get; set; } = new List<TokenItem>();

        // Stub for InsertTokenCommand
        public System.Windows.Input.ICommand? InsertTokenCommand { get; set; }
    }
}
