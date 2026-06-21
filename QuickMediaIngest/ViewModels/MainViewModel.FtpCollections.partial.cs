using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data;
using QuickMediaIngest.Localization;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel
    {
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
