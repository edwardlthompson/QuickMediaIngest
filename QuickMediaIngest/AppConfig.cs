#nullable enable
using System.Collections.Generic;

namespace QuickMediaIngest
{
    /// <summary>Serializable preferences (FTP password is stored in Windows Credential Manager, not in config.json).</summary>
    public sealed class AppConfig
    {
        public int UpdateIntervalHours { get; set; } = 24;
        public string UpdatePackageType { get; set; } = "Portable";
        public string DestinationRoot { get; set; } = string.Empty;
        public bool? OpenDestinationFolderWhenImportCompletes { get; set; }
        public bool? ShowCompactImportSummaryModal { get; set; }
        public bool? ConfirmCancelImportRequest { get; set; }
        public int ImportCooldownBetweenFilesMs { get; set; }
        public bool ImportSingleThreaded { get; set; }
        /// <summary>GitHub release tag for which we already showed the &quot;update available&quot; popup (avoids repeating on every background check).</summary>
        public string LastNotifiedUpdateTag { get; set; } = string.Empty;
        public string DestinationPreset { get; set; } = "Custom";
        public string LastSessionDestinationRoot { get; set; } = string.Empty;
        public bool DeleteAfterImport { get; set; }
        public bool DeleteAfterImportPromptDismissed { get; set; }
        public string NamingTemplate { get; set; } = "[Date]_[Time]_[Original]";
        public string NamingPreset { get; set; } = "Recommended (Date + Shoot + Original)";
        public string NamingDateFormat { get; set; } = "yyyy-MM-dd";
        public string NamingTimeFormat { get; set; } = "HH-mm-ss";
        public string NamingSeparator { get; set; } = "_";
        public bool NamingIncludeSequence { get; set; }
        public string NamingShootNameSample { get; set; } = "my-shoot";
        public bool NamingLowercase { get; set; } = true;
        public string ThumbnailPerformanceMode { get; set; } = "Balanced";
        public bool GroupRawAndRenderedPairs { get; set; }
        public string FtpHost { get; set; } = string.Empty;
        public int FtpPort { get; set; } = 21;
        public string FtpUser { get; set; } = string.Empty;
        /// <summary>Legacy plaintext (migrated to Credential Manager on load). Always empty on save.</summary>
        public string FtpPass { get; set; } = string.Empty;
        public string FtpRemoteFolder { get; set; } = "/DCIM";
        public bool AutoReconnectLastFtp { get; set; } = true;
        public bool SettingsMenuExpanded { get; set; } = true;
        public string ScanPath { get; set; } = string.Empty;
        public bool SelectAll { get; set; } = true;
        public bool? IsDarkTheme { get; set; }
        public double ThumbnailSize { get; set; } = 120;
        public bool ScanIncludeSubfolders { get; set; } = true;
        public int TimeBetweenShootsHours { get; set; } = 4;
        public bool LimitFtpThumbnailLoad { get; set; }
        public int FtpInitialThumbnailCount { get; set; }
        public bool ExpandPreviewStacks { get; set; }
        public string DuplicatePolicy { get; set; } = "Suffix";
        public string VerificationMode { get; set; } = "Fast";
        public string UiLanguage { get; set; } = string.Empty;
        public bool EmbedKeywordsOnImport { get; set; }
        public bool ConfirmBeforeImport { get; set; }
        public bool SuppressExcludedFolderScanReminders { get; set; }
        public bool SettingsAdvancedExpanded { get; set; }
        public List<string>? RibbonTileOrder { get; set; }
        public double WindowWidth { get; set; } = 960;
        public double WindowHeight { get; set; } = 620;
        public bool WindowMaximized { get; set; }
        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }
        public bool IsFirstRun { get; set; } = true;
        public List<string> SelectedDriveDeviceIds { get; set; } = new();
        public List<string> SelectedDrivePaths { get; set; } = new();
        public Dictionary<string, List<string>> SkippedFoldersBySource { get; set; } = new();
    }
}
