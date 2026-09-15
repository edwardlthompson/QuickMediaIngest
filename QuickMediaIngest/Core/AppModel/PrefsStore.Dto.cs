#nullable enable
namespace QuickMediaIngest.Core.AppModel
{
    public sealed partial class PrefsStore
    {
        public sealed class FileDto
        {
            public string Theme { get; set; } = "Dark";
            public string Language { get; set; } = "en";
            public bool StripGps { get; set; }
            public string SecondaryDestination { get; set; } = string.Empty;
            public string CreatorStamp { get; set; } = string.Empty;
            public string CopyrightStamp { get; set; } = string.Empty;
            public bool WriteChecksumManifest { get; set; } = true;
            public bool WriteXmpSidecar { get; set; }
            public string WatchFolder { get; set; } = string.Empty;
            public string TimeZoneMode { get; set; } = "CameraAsIs";
            public string BatchRenameBase { get; set; } = "Shoot";
            public bool EjectAfterImport { get; set; }
            public string DestinationPreset { get; set; } = "Custom";
            public string DuplicatePolicy { get; set; } = "Suffix";
            public string VerificationMode { get; set; } = "Fast";
            public bool GroupRawJpeg { get; set; }
            public bool ExpandStacked { get; set; }
            public bool ConfirmBeforeImport { get; set; }
            public bool DeleteAfterImport { get; set; }
            public double ThumbnailSize { get; set; }
            public int TimeBetweenShootsHours { get; set; }
            public string FilterFileType { get; set; } = "All";
            public bool? PreferAdb { get; set; }
            public bool? AllGroupsExpanded { get; set; }
            public double WindowWidth { get; set; }
            public double WindowHeight { get; set; }
            public double WindowLeft { get; set; }
            public double WindowTop { get; set; }
            public bool WindowMaximized { get; set; }
            public bool WindowPositionSet { get; set; }
            public double PreviewPaneWidth { get; set; }
        }
    }
}
