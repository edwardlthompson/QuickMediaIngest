#nullable enable
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>File-naming builder state bound by Avalonia Preferences.</summary>
    public sealed partial class NamingState : ObservableObject
    {
        [ObservableProperty] private string preset = FileNamingBuilder.PresetCustom;
        [ObservableProperty] private bool includeDate = true;
        [ObservableProperty] private bool includeTime = true;
        [ObservableProperty] private bool includeSequence;
        [ObservableProperty] private bool includeShootName;
        [ObservableProperty] private bool includeOriginalName = true;
        [ObservableProperty] private string dateFormat = "yyyy-MM-dd";
        [ObservableProperty] private string timeFormat = "HH-mm-ss";
        [ObservableProperty] private string separator = "_";
        [ObservableProperty] private string shootNameSample = "my-shoot";
        [ObservableProperty] private bool lowercase = true;

        public bool Suppress { get; set; }

        public IReadOnlyList<string> PresetOptions => FileNamingBuilder.Presets;

        public IReadOnlyList<string> DateFormatOptions => FileNamingBuilder.DateFormats;

        public IReadOnlyList<string> TimeFormatOptions => FileNamingBuilder.TimeFormats;

        public IReadOnlyList<string> SeparatorOptions => FileNamingBuilder.Separators;

        public IReadOnlyList<string> FolderTokenPalette => FileNamingBuilder.FolderTokens;

        [ObservableProperty] private bool includeFolderShoot;
        [ObservableProperty] private bool includeFolderDate;
        [ObservableProperty] private bool includeFolderYear;
        [ObservableProperty] private bool includeFolderMonth;
        [ObservableProperty] private bool includeFolderDay;
        [ObservableProperty] private bool includeFolderJob;
        [ObservableProperty] private bool includeFolderClient;
        [ObservableProperty] private bool includeFolderCamera;

        public ObservableCollection<string> AvailableTokens { get; } = new();

        public ObservableCollection<string> SelectedTokens { get; } = new();

        public ObservableCollection<string> PreviewExamples { get; } = new();

        public ObservableCollection<string> FolderPreview { get; } = new();

        public NamingParts ToParts() => new(
            IncludeDate, IncludeTime, IncludeSequence, IncludeShootName, IncludeOriginalName,
            DateFormat, TimeFormat, Separator);

        public void FromParts(NamingParts parts)
        {
            IncludeDate = parts.Date;
            IncludeTime = parts.Time;
            IncludeSequence = parts.Sequence;
            IncludeShootName = parts.Shoot;
            IncludeOriginalName = parts.Original;
            DateFormat = parts.DateFormat;
            TimeFormat = parts.TimeFormat;
            Separator = string.IsNullOrEmpty(parts.Separator) ? "_" : parts.Separator;
        }

        public FolderParts ToFolderParts() => new(
            IncludeFolderShoot, IncludeFolderDate, IncludeFolderYear, IncludeFolderMonth,
            IncludeFolderDay, IncludeFolderJob, IncludeFolderClient, IncludeFolderCamera);

        public void FromFolderParts(FolderParts parts)
        {
            IncludeFolderShoot = parts.ShootTitle;
            IncludeFolderDate = parts.Date;
            IncludeFolderYear = parts.Year;
            IncludeFolderMonth = parts.Month;
            IncludeFolderDay = parts.Day;
            IncludeFolderJob = parts.Job;
            IncludeFolderClient = parts.Client;
            IncludeFolderCamera = parts.Camera;
        }

        public void RefreshLists(string fileTemplate, string? folderTemplate = null)
        {
            Replace(AvailableTokens, FileNamingPreview.UnusedFileTokens(fileTemplate));
            Replace(SelectedTokens, FileNamingPreview.UsedFileTokens(fileTemplate));
            Replace(PreviewExamples, FileNamingPreview.Examples(fileTemplate, ToParts(), ShootNameSample, Lowercase));
            Replace(FolderPreview, FileNamingPreview.FolderExamples(folderTemplate));
        }

        private static void Replace(ObservableCollection<string> target, IReadOnlyList<string> next)
        {
            target.Clear();
            foreach (string item in next)
            {
                target.Add(item);
            }
        }
    }
}
