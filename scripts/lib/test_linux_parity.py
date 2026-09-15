"""LP-host: Desktop composition uses IngestBenchHost; no System.Windows in Desktop."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESK = ROOT / "QuickMediaIngest.Desktop"
CORE = ROOT / "QuickMediaIngest/Core"


def main() -> int:
    for path in DESK.rglob("*.cs"):
        text = path.read_text(encoding="utf-8")
        assert "System.Windows" not in text, path
    host = (CORE / "AppModel/IngestBenchHost.cs").read_text(encoding="utf-8")
    assert "XdgAppPaths" in host
    assert "OnboardingStore" in host
    assert "FilePendingCrashStore" in host
    assert "MarkDiscarded" in (CORE / "AppModel/IngestBenchCrash.cs").read_text(encoding="utf-8")
    assert "AppModelUserPrompt" in host
    app = (DESK / "App.axaml.cs").read_text(encoding="utf-8")
    assert "IngestBenchDesktop.CreateMainWindow" in app
    axaml = (DESK / "MainWindow.axaml").read_text(encoding="utf-8")
    assert "ShowUserPrompt" in axaml
    assert "ShowDrivePicker" in axaml
    assert "AddFtpButton" in axaml
    enum_cs = (CORE / "MountEnumerator.cs").read_text(encoding="utf-8")
    assert "/media" in enum_cs and "/run/media" in enum_cs
    scan = (CORE / "VolumeScan.cs").read_text(encoding="utf-8")
    assert "CountMedia" in scan
    tests = (ROOT / "QuickMediaIngest.Core.Tests/IngestBenchHostTests.cs").read_text(encoding="utf-8")
    assert "Create_UsesInjectedPaths_FirstRunUntilDismissed" in tests
    assert "ConfirmDrivePick_CountsMediaInSelectedExtraRoot" in tests
    dest = (CORE / "AppModel/DestSettingsStore.cs").read_text(encoding="utf-8")
    assert "dest.json" in dest
    assert "NamingTemplate" in dest
    model = (CORE / "AppModel/IngestBenchAppModel.cs").read_text(encoding="utf-8")
    assert "DestinationChipLabel" in model
    assert "SaveLocationNote" in model
    assert "namingTemplate" in model
    assert "SetDestination_PersistsChipAndNaming" in tests
    assert "RunImportAsync" in host
    imp = (CORE / "AppModel/IngestBenchImport.cs").read_text(encoding="utf-8")
    post = (CORE / "AppModel/IngestBenchPost.cs").read_text(encoding="utf-8")
    assert "IsDryRun" in post
    assert "DeleteAfterImport" in imp
    assert "ForecastSpace" in imp
    assert "IngestCollisionAnalyzer" in imp
    trash = (CORE / "TrashingFileProvider.cs").read_text(encoding="utf-8")
    assert "ITrashService" in trash
    shoots = (CORE / "AppModel/IngestBenchShoots.cs").read_text(encoding="utf-8")
    assert "SelectAll" in shoots and "SkipVisibleSelected" in shoots
    assert "shootFilter" in (CORE / "AppModel/IngestBenchAppModel.cs").read_text(encoding="utf-8")
    ftp = (CORE / "AppModel/IngestBenchFtp.cs").read_text(encoding="utf-8")
    assert "SecretServiceFtpCredentialStore" in ftp
    assert "FtpBandwidthThrottler" in ftp
    assert "ShowFtpEditor" in (DESK / "MainWindow.axaml").read_text(encoding="utf-8")
    adb = (CORE / "AppModel/IngestBenchAdb.cs").read_text(encoding="utf-8")
    assert "AdbDeviceProbe" in adb
    assert "DeduplicateDualFtpAliases" in adb
    assert "ScanConnectedAsync" in adb
    assert "BeginPick" in (CORE / "AppModel/IngestBenchScan.cs").read_text(encoding="utf-8")
    assert "VolumeScanHint" in (CORE / "VolumeScanHint.cs").read_text(encoding="utf-8")
    assert "DecodeItem" in (CORE / "AppModel/IngestBenchThumbs.cs").read_text(encoding="utf-8")
    assert "TryMaterialize" in (CORE / "AppModel/AdbPreviewPull.cs").read_text(encoding="utf-8")
    assert "TryResolveForSerial" in adb
    thumbs = (CORE / "AppModel/IngestBenchThumbs.cs").read_text(encoding="utf-8")
    assert "MagickThumbnailDecoder" in thumbs
    assert "VipsThumbnailDecoder" in thumbs
    assert "IsHeifFamily" in thumbs
    assert "DefaultCapBytes" in thumbs
    media_ext = (CORE / "MediaExtensions.cs").read_text(encoding="utf-8")
    assert ".avif" in media_ext and ".jxl" in media_ext and ".webm" in media_ext
    scene = (CORE / "AppModel/IngestBenchScene.cs").read_text(encoding="utf-8")
    assert "ImportAfterglow" in scene and "ShellOpen" in scene
    assert "commandBarCompact" in model
    assert "ShowPrimaryExtras" in model
    assert "Notifications" in model
    assert 'FontSize="14"' in axaml and "#007ACC" in axaml
    hist = (CORE / "ImportHistoryCsv.cs").read_text(encoding="utf-8")
    assert "ToCsv" in hist and "Matches" in hist
    assert "VisibleHistory" in axaml
    excl = (CORE / "ScanExclusionMatcher.cs").read_text(encoding="utf-8")
    assert "IsUnder" in excl
    assert "ExcludedFolders" in axaml
    fb = (CORE / "AppModel/IngestBenchFeedback.cs").read_text(encoding="utf-8")
    assert "SearchDuplicatesFailSoftAsync" in fb
    assert "TryOpenGitHub" in fb
    desk_cs = (DESK / "MainWindow.axaml.cs").read_text(encoding="utf-8")
    assert "Key.Escape" in desk_cs and "KeyModifiers.Control" in desk_cs
    prefs = (CORE / "AppModel/IngestBenchPrefs.cs").read_text(encoding="utf-8")
    assert "ExportJson" in prefs and "ImportJson" in prefs
    store = (CORE / "AppModel/PrefsStore.cs").read_text(encoding="utf-8")
    assert "JsonObject" in store and "DeleteAfterImport" in store and "ThumbnailSize" in store
    assert "Prefs.Theme" in axaml
    about = (CORE / "AppModel/IngestBenchAbout.cs").read_text(encoding="utf-8")
    assert "DonationLinks.Venmo" in about
    assert "CheckForUpdateAsync" in about
    assert "Deb" in about
    prompt_ax = axaml
    assert "ShowUserPrompt" in prompt_ax
    assert "CanImport" in (CORE / "AppModel/IngestBenchAppModel.cs").read_text(encoding="utf-8")
    assert "RequestAsync" in (CORE / "AppModel/IngestBenchImportCancel.cs").read_text(encoding="utf-8")
    assert "ShowFtpFailure" in ftp
    assert "OverlayBlurRadius" in (CORE / "AppModel/IngestBenchA11y.cs").read_text(encoding="utf-8")
    assert "FadeOnVisible" in (DESK / "MotionAssist.cs").read_text(encoding="utf-8")
    queue = (CORE / "AppModel/IngestBenchQueue.cs").read_text(encoding="utf-8")
    assert "pending-import.json" in (CORE / "AppModel/PendingPlanStore.cs").read_text(encoding="utf-8")
    assert "EnqueueAsync" in queue and "RetryFailedAsync" in queue and "ResumeAsync" in queue
    assert 'Content="Queue"' in axaml
    assert "RebuildFromItems" in shoots and "ToggleExpanded" in shoots and "IgnoreFolder" in shoots
    assert "FillShootPreviews" in thumbs and "confirmBeforeImport" in (CORE / "AppModel/PrefsState.cs").read_text(encoding="utf-8")
    cull = (CORE / "AppModel/IngestBenchCull.cs").read_text(encoding="utf-8")
    assert "PickSelected" in cull and "RejectSelected" in cull
    assert "CullSelectionPersistence.Restore" in shoots
    assert 'Content="Pick"' in axaml and 'Content="Reject"' in axaml
    post = (CORE / "AppModel/IngestBenchPost.cs").read_text(encoding="utf-8")
    assert "ShootChecksumManifestWriter" in post
    assert "import-hashes.json" in post
    assert "SecondaryDestinationRoot" in post
    assert "CopyrightStamp" in post
    watch = (CORE / "AppModel/IngestBenchWatch.cs").read_text(encoding="utf-8")
    assert "SplitSelected" in watch and "MergeSelected" in watch
    assert "WatchFolderService" in watch
    assert "DateTimeZoneAdjuster" in watch
    assert "ShootTitleBatchRenamer" in watch
    assert "DestinationFolderTemplate" in (CORE / "IngestOptions.cs").read_text(encoding="utf-8")
    thumbs = (CORE / "AppModel/IngestBenchThumbs.cs").read_text(encoding="utf-8")
    assert "HeifPreviewDecoder" in thumbs
    assert "FfmpegVideoThumbnailDecoder" in thumbs
    assert "Purge" in thumbs and "CompareSelected" in thumbs
    assert "IsLinux" in (CORE / "Services/ColorManagementProfileService.cs").read_text(encoding="utf-8")
    ftp_cs = (CORE / "AppModel/IngestBenchFtp.cs").read_text(encoding="utf-8")
    assert "ApplyWifiPreset" in ftp_cs
    assert "CameraWifiProfilePresets" in ftp_cs
    ptp = (CORE / "AppModel/IngestBenchPtp.cs").read_text(encoding="utf-8")
    assert "gphoto2" in ptp and "gvfs" in ptp
    assert "ShowPtpPicker" in axaml
    assert 'Content="PTP"' in axaml
    a11y = (CORE / "AppModel/IngestBenchA11y.cs").read_text(encoding="utf-8")
    assert "GTK_THEME" in a11y and "UiReadingOrder" in a11y
    assert "AccessibilityAnnouncement" in axaml
    assert "ShowShortcuts" in axaml
    desk_a11y = (DESK / "MainWindow.A11y.partial.cs").read_text(encoding="utf-8")
    assert "FlowDirection" in desk_a11y
    assert "Key.F1" in (DESK / "MainWindow.axaml.cs").read_text(encoding="utf-8")
    tests = (ROOT / "QuickMediaIngest.Core.Tests/IngestBenchA11yTests.cs").read_text(encoding="utf-8")
    assert "DetectHighContrast" in tests and "IsRightToLeft" in tests
    eject = (CORE / "AppModel/IngestBenchEject.cs").read_text(encoding="utf-8")
    assert "gio" in eject and "udisksctl" in eject
    assert "HasLeftoverLocalFiles" in eject
    assert 'Content="Eject"' in axaml
    assert "EjectAfterImport" in axaml
    assert "KeyModifiers.Control" in (DESK / "MainWindow.axaml.cs").read_text(encoding="utf-8")
    naming = (CORE / "FileNamingBuilder.cs").read_text(encoding="utf-8")
    assert "[ShootName]" in naming and "PresetRecommended" in naming
    assert "InsertFileToken" in (CORE / "AppModel/IngestBenchNaming.cs").read_text(encoding="utf-8")
    assert "Naming.AvailableTokens" in axaml
    head, _, prefs = axaml.partition('Name="PrefsOverlay"')
    assert "Naming.AvailableTokens" not in head
    assert "NamingChipBar" not in axaml
    sheet = axaml.split('Name="IngestSheetTitle"', 1)[1]
    assert "Naming.IncludeDate" in sheet and "Naming.IncludeFolderShoot" in sheet
    assert "Naming.AvailableTokens" not in sheet
    assert "Naming.PreviewExamples" in axaml
    assert "PrefsPreset" in axaml
    assert "SaveLocationNote" in axaml
    assert "ScanConnectedAsync" in (CORE / "AppModel/IngestBenchAdb.cs").read_text(encoding="utf-8")
    tests = (ROOT / "QuickMediaIngest.Core.Tests/FileNamingBuilderTests.cs").read_text(encoding="utf-8")
    assert "ChipsAndPreset_PersistInDestJson" in tests
    print("OK: LP-host…eject IngestBenchHost + Desktop has no System.Windows")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
