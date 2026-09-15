#nullable enable
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickMediaIngest.Core.Settings;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Preferences overlay state (theme, language, GPS strip, search).</summary>
    public sealed partial class PrefsState : ObservableObject
    {
        [ObservableProperty] private string theme = "Dark";
        [ObservableProperty] private string language = "en";
        [ObservableProperty] private bool stripGps;
        [ObservableProperty] private string secondaryDestination = string.Empty;
        [ObservableProperty] private string creatorStamp = string.Empty;
        [ObservableProperty] private string copyrightStamp = string.Empty;
        [ObservableProperty] private bool writeChecksumManifest = true;
        [ObservableProperty] private bool writeXmpSidecar;
        [ObservableProperty] private string watchFolder = string.Empty;
        [ObservableProperty] private string timeZoneMode = "CameraAsIs";
        [ObservableProperty] private string batchRenameBase = "Shoot";
        [ObservableProperty] private bool ejectAfterImport;
        [ObservableProperty] private string destinationPreset = "Pictures";
        [ObservableProperty] private string duplicatePolicy = "Suffix";
        [ObservableProperty] private string verificationMode = "Fast";
        [ObservableProperty] private bool groupRawJpeg;
        [ObservableProperty] private bool expandStacked;
        [ObservableProperty] private bool confirmBeforeImport;
        [ObservableProperty] private string searchQuery = string.Empty;
        [ObservableProperty] private bool showAppearance = true;
        [ObservableProperty] private bool showNaming = true;
        [ObservableProperty] private bool showLanguage = true;
        [ObservableProperty] private bool showImport = true;
        [ObservableProperty] private bool showNoResults;
        [ObservableProperty] private double windowWidth;
        [ObservableProperty] private double windowHeight;
        [ObservableProperty] private double windowLeft;
        [ObservableProperty] private double windowTop;
        [ObservableProperty] private bool windowMaximized;
        [ObservableProperty] private bool windowPositionSet;
        [ObservableProperty] private double previewPaneWidth = PrefsLayout.DefaultPane;

        public IReadOnlyList<string> ThemeChoices { get; } = new[] { "Dark", "Light" };

        public IReadOnlyList<string> LanguageChoices { get; } = new[] { "en", "fr", "es", "de", "ja" };

        public IReadOnlyList<string> TimeZoneChoices { get; } = new[] { "CameraAsIs", "LocalSystem", "Utc", "CustomOffset" };

        public IReadOnlyList<string> DestinationPresetChoices { get; } = new[] { "Pictures", "Documents", "Custom" };

        public IReadOnlyList<string> DuplicatePolicyChoices { get; } = new[] { "Suffix", "Skip", "OverwriteIfNewer" };

        public IReadOnlyList<string> VerificationChoices { get; } = new[] { "Fast", "Strict" };

        public void ApplySearch()
        {
            ShowAppearance = SettingsSearch.Matches(SearchQuery, "Appearance", "Theme", "Dark", "Light");
            ShowNaming = false;
            ShowLanguage = SettingsSearch.Matches(SearchQuery, "Language", "locale", Language);
            ShowImport = SettingsSearch.Matches(SearchQuery, "Import", "GPS", "strip", "checksum", "XMP", "copyright", "3-2-1", "secondary", "watch", "timezone", "rename", "eject", "unmount", "duplicate", "verify", "RAW", "confirm", "preset", CreatorStamp, CopyrightStamp, WatchFolder);
            ShowNoResults = !SettingsSearch.IsBlank(SearchQuery)
                && !ShowAppearance && !ShowNaming && !ShowLanguage && !ShowImport;
        }
    }
}
