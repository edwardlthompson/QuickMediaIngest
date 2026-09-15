#nullable enable
namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>Load/save Preferences; JSON import/export without passwords.</summary>
    public static class IngestBenchPrefs
    {
        public static void Load(IngestBenchHost host)
        {
            PrefsStore.FileDto dto = new PrefsStore(host.Paths).Load();
            ApplyDto(host.Model.Prefs, dto);
            ApplySession(host.Model, dto);
            IngestBenchA11y.Apply(host);
        }

        public static void Save(IngestBenchHost host)
        {
            new PrefsStore(host.Paths).Save(ToDto(host.Model));
            IngestBenchWatch.ApplyWatchFolder(host);
        }

        public static string ExportJson(IngestBenchHost host) =>
            PrefsStore.Format(ToDto(host.Model));

        public static bool ImportJson(IngestBenchHost host, string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                PrefsStore.FileDto dto = PrefsStore.Parse(json);

                ApplyDto(host.Model.Prefs, dto);
                ApplySession(host.Model, dto);
                Save(host);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static PrefsStore.FileDto ToDto(IngestBenchAppModel model)
        {
            PrefsState prefs = model.Prefs;
            return new()
            {
                Theme = prefs.Theme,
                Language = prefs.Language,
                StripGps = prefs.StripGps,
                SecondaryDestination = prefs.SecondaryDestination,
                CreatorStamp = prefs.CreatorStamp,
                CopyrightStamp = prefs.CopyrightStamp,
                WriteChecksumManifest = prefs.WriteChecksumManifest,
                WriteXmpSidecar = prefs.WriteXmpSidecar,
                WatchFolder = prefs.WatchFolder,
                TimeZoneMode = prefs.TimeZoneMode,
                BatchRenameBase = prefs.BatchRenameBase,
                EjectAfterImport = prefs.EjectAfterImport,
                DestinationPreset = prefs.DestinationPreset,
                DuplicatePolicy = prefs.DuplicatePolicy,
                VerificationMode = prefs.VerificationMode,
                GroupRawJpeg = prefs.GroupRawJpeg,
                ExpandStacked = prefs.ExpandStacked,
                ConfirmBeforeImport = prefs.ConfirmBeforeImport,
                DeleteAfterImport = model.DeleteAfterImport,
                ThumbnailSize = model.ThumbnailSize,
                TimeBetweenShootsHours = model.TimeBetweenShootsHours,
                FilterFileType = model.FilterFileType,
                PreferAdb = model.PreferAdb,
                AllGroupsExpanded = model.AllGroupsExpanded,
                WindowWidth = prefs.WindowWidth,
                WindowHeight = prefs.WindowHeight,
                WindowLeft = prefs.WindowLeft,
                WindowTop = prefs.WindowTop,
                WindowMaximized = prefs.WindowMaximized,
                WindowPositionSet = prefs.WindowPositionSet,
                PreviewPaneWidth = prefs.PreviewPaneWidth,
            };
        }

        private static void ApplySession(IngestBenchAppModel model, PrefsStore.FileDto dto)
        {
            model.DeleteAfterImport = dto.DeleteAfterImport;
            if (dto.ThumbnailSize is >= 50 and <= 300)
            {
                model.ThumbnailSize = dto.ThumbnailSize;
            }

            if (dto.TimeBetweenShootsHours is >= 1 and <= 24)
            {
                model.TimeBetweenShootsHours = dto.TimeBetweenShootsHours;
            }

            model.FilterFileType = string.IsNullOrWhiteSpace(dto.FilterFileType) ? "All" : dto.FilterFileType;
            model.PreferAdb = dto.PreferAdb ?? model.AdbAvailable;
            model.AllGroupsExpanded = dto.AllGroupsExpanded ?? true;
        }

        private static void ApplyDto(PrefsState prefs, PrefsStore.FileDto dto)
        {
            prefs.Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "Dark" : dto.Theme;
            prefs.Language = string.IsNullOrWhiteSpace(dto.Language) ? "en" : dto.Language;
            prefs.StripGps = dto.StripGps;
            prefs.SecondaryDestination = dto.SecondaryDestination ?? string.Empty;
            prefs.CreatorStamp = dto.CreatorStamp ?? string.Empty;
            prefs.CopyrightStamp = dto.CopyrightStamp ?? string.Empty;
            prefs.WriteChecksumManifest = dto.WriteChecksumManifest;
            prefs.WriteXmpSidecar = dto.WriteXmpSidecar;
            prefs.WatchFolder = dto.WatchFolder ?? string.Empty;
            prefs.TimeZoneMode = string.IsNullOrWhiteSpace(dto.TimeZoneMode) ? "CameraAsIs" : dto.TimeZoneMode;
            prefs.BatchRenameBase = string.IsNullOrWhiteSpace(dto.BatchRenameBase) ? "Shoot" : dto.BatchRenameBase;
            prefs.EjectAfterImport = dto.EjectAfterImport;
            prefs.DestinationPreset = string.IsNullOrWhiteSpace(dto.DestinationPreset) ? "Custom" : dto.DestinationPreset;
            prefs.DuplicatePolicy = string.IsNullOrWhiteSpace(dto.DuplicatePolicy) ? "Suffix" : dto.DuplicatePolicy;
            prefs.VerificationMode = string.IsNullOrWhiteSpace(dto.VerificationMode) ? "Fast" : dto.VerificationMode;
            prefs.GroupRawJpeg = dto.GroupRawJpeg;
            prefs.ExpandStacked = dto.ExpandStacked;
            prefs.ConfirmBeforeImport = dto.ConfirmBeforeImport;
            prefs.WindowWidth = dto.WindowWidth;
            prefs.WindowHeight = dto.WindowHeight;
            prefs.WindowLeft = dto.WindowLeft;
            prefs.WindowTop = dto.WindowTop;
            prefs.WindowMaximized = dto.WindowMaximized;
            prefs.WindowPositionSet = dto.WindowPositionSet;
            prefs.PreviewPaneWidth = PrefsLayout.ClampPane(dto.PreviewPaneWidth);
            prefs.ApplySearch();
        }
    }
}
