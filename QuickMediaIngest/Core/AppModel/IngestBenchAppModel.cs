#nullable enable
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickMediaIngest.Core.ImportUi;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Nav;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Shared ingest-bench state for WPF and Avalonia. Desktop binds this type.</summary>
    public sealed partial class IngestBenchAppModel : ObservableObject
    {
        public OverlayNav Nav { get; } = new();

        [ObservableProperty] private bool isFirstRun = true;
        [ObservableProperty] private bool deleteAfterImport;
        [ObservableProperty] private bool isImporting;
        [ObservableProperty] private int sourceCount;
        [ObservableProperty] private string destinationRoot = string.Empty;
        [ObservableProperty] private string namingTemplate = "[Date]_[Time]_[Original]";
        [ObservableProperty] private string destFolderTemplate = string.Empty;
        [ObservableProperty] private bool showCrashOverlay;
        [ObservableProperty] private string crashOverlayText = string.Empty;
        [ObservableProperty] private string crashFingerprint = string.Empty;
        [ObservableProperty] private bool showUserPrompt;
        [ObservableProperty] private string userPromptTitle = string.Empty;
        [ObservableProperty] private string userPromptBody = string.Empty;
        [ObservableProperty] private bool userPromptIsConfirm;
        [ObservableProperty] private bool showDrivePicker;
        [ObservableProperty] private int collisionCount;
        [ObservableProperty] private string shootFilter = string.Empty;
        [ObservableProperty] private bool showFtpEditor;
        [ObservableProperty] private bool showFtpThrottle;
        [ObservableProperty] private bool preferAdb;
        [ObservableProperty] private bool adbAvailable;
        [ObservableProperty] private string previewPath = string.Empty;
        [ObservableProperty] private int importPercent;
        [ObservableProperty] private string importStatus = "Ready.";
        [ObservableProperty] private string activityKind = string.Empty;
        [ObservableProperty] private bool activityIndeterminate;
        [ObservableProperty] private bool showAfterglow;
        [ObservableProperty] private string afterglowText = string.Empty;
        [ObservableProperty] private string afterglowFolder = string.Empty;
        [ObservableProperty] private bool commandBarCompact;
        [ObservableProperty] private bool showOverflow;
        [ObservableProperty] private bool showNotifications;
        [ObservableProperty] private string historyFilter = string.Empty;
        [ObservableProperty] private string selectedExclusion = string.Empty;
        [ObservableProperty] private string feedbackKind = "bug";
        [ObservableProperty] private string feedbackTitle = string.Empty;
        [ObservableProperty] private string feedbackDescription = string.Empty;
        [ObservableProperty] private string feedbackPreview = string.Empty;
        [ObservableProperty] private string feedbackGitHubReason = string.Empty;
        [ObservableProperty] private bool feedbackCanOpenGitHub;
        [ObservableProperty] private string aboutVersion = string.Empty;
        [ObservableProperty] private string aboutUpdateStatus = string.Empty;
        [ObservableProperty] private bool hasPendingImportPlan;
        [ObservableProperty] private int queuedImportCount;
        [ObservableProperty] private int failedImportCount;
        [ObservableProperty] private ItemGroup? selectedShoot;
        [ObservableProperty] private ImportItem? selectedCullItem;
        [ObservableProperty] private string compareLeftPath = string.Empty;
        [ObservableProperty] private string compareRightPath = string.Empty;
        [ObservableProperty] private bool showCompare;
        [ObservableProperty] private string iccProfilePath = string.Empty;
        [ObservableProperty] private bool showPtpPicker;
        [ObservableProperty] private bool highContrast;
        [ObservableProperty] private bool reducedMotion;
        [ObservableProperty] private bool isRightToLeft;
        [ObservableProperty] private double overlayBlurRadius = 8d;
        [ObservableProperty] private string accessibilityAnnouncement = string.Empty;
        [ObservableProperty] private bool showShortcuts;
        [ObservableProperty] private double thumbnailSize = 120;
        [ObservableProperty] private int timeBetweenShootsHours = 1;
        [ObservableProperty] private string filterFileType = "All";
        [ObservableProperty] private bool allGroupsExpanded = true;
        [ObservableProperty] private int unreadNotificationCount;
        [ObservableProperty] private bool showFtpFailure;
        [ObservableProperty] private bool showIngestSheet;
        [ObservableProperty] private bool destChipFlash;

        public IReadOnlyList<string> CameraWifiNames { get; } = new[] { "Sony", "Canon", "Nikon", "Fujifilm", "Panasonic" };

        public FtpDraft Ftp { get; } = new();

        public ObservableCollection<VolumeChoice> Volumes { get; } = new();

        public ObservableCollection<ItemGroup> Shoots { get; } = new();

        public ObservableCollection<SavedFtpSource> FtpSources { get; } = new();

        public ObservableCollection<string> Notifications { get; } = new();

        public ObservableCollection<ImportHistoryRecord> HistoryRecords { get; } = new();

        public ObservableCollection<ImportHistoryRecord> VisibleHistory { get; } = new();

        public ObservableCollection<string> ExcludedFolders { get; } = new();

        public ObservableCollection<string> FeedbackDuplicates { get; } = new();

        public ObservableCollection<string> PtpDevices { get; } = new();

        public ObservableCollection<FilterChip> FilterChips { get; } = new();

        public PrefsState Prefs { get; } = new();

        public NamingState Naming { get; } = new();

        public bool ShowPrimaryExtras => !CommandBarCompact;

        public bool ShowWaitingForCard => SourceCount <= 0;

        public bool ShowShootList => SourceCount > 0;

        public bool ShowEject => SourceCount > 0;

        public bool ShowRetryFailed => FailedImportCount > 0;

        public bool ShowResumePending => HasPendingImportPlan;

        public bool ShowCullTools => SelectedCullItem is not null;

        public bool CanImport => SelectedShoot is not null && !IsImporting;

        public bool ShowNotifyBadge => UnreadNotificationCount > 0;

        public bool ShowActivityProgress => ActivityKind.Length > 0;

        public IReadOnlyList<int> CullRatings { get; } = IngestBenchCull.Ratings;

        public IReadOnlyList<string> CullColors { get; } = IngestBenchCull.ColorNames;

        public IReadOnlyList<string> FileTypeChoices => IngestBenchFilters.FileTypes;

        public bool HasPreview => !string.IsNullOrWhiteSpace(PreviewPath);

        public string DestinationChipLabel => ImportAfterglow.FolderLabel(DestinationRoot);

        public string SaveLocationNote =>
            string.IsNullOrWhiteSpace(DestinationRoot) ? "Choose a save location" : "Saving to " + DestinationRoot;

        public void DismissOnboarding() => IsFirstRun = false;

        public void NoteSources(int count)
        {
            SourceCount = System.Math.Max(0, count);
            if (SourceCount > 0)
            {
                IsFirstRun = false;
            }
        }

        partial void OnSourceCountChanged(int value)
        {
            OnPropertyChanged(nameof(ShowWaitingForCard));
            OnPropertyChanged(nameof(ShowShootList));
            OnPropertyChanged(nameof(ShowEject));
        }

        partial void OnDestinationRootChanged(string value)
        {
            OnPropertyChanged(nameof(DestinationChipLabel));
            OnPropertyChanged(nameof(SaveLocationNote));
            DestChipFlash = true;
        }

        partial void OnPreviewPathChanged(string value) => OnPropertyChanged(nameof(HasPreview));

        partial void OnCommandBarCompactChanged(bool value) => OnPropertyChanged(nameof(ShowPrimaryExtras));

        partial void OnFailedImportCountChanged(int value) => OnPropertyChanged(nameof(ShowRetryFailed));

        partial void OnHasPendingImportPlanChanged(bool value) => OnPropertyChanged(nameof(ShowResumePending));

        partial void OnUnreadNotificationCountChanged(int value) => OnPropertyChanged(nameof(ShowNotifyBadge));

        partial void OnActivityKindChanged(string value) => OnPropertyChanged(nameof(ShowActivityProgress));

        partial void OnIsImportingChanged(bool value) => OnPropertyChanged(nameof(CanImport));

        partial void OnSelectedCullItemChanged(ImportItem? value) => OnPropertyChanged(nameof(ShowCullTools));

        partial void OnSelectedShootChanged(ItemGroup? value)
        {
            OnPropertyChanged(nameof(CanImport));
            if (value is null)
            {
                SelectedCullItem = null;
                return;
            }

            if (SelectedCullItem is null || !value.Items.Contains(SelectedCullItem))
            {
                SelectedCullItem = value.Items.Count > 0 ? value.Items[0] : null;
            }
        }
    }
}
