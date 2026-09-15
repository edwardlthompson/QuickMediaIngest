"""LX-L3: Avalonia ingest-bench binds IngestBenchAppModel (Import / Dry run / Refresh)."""
from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESK = ROOT / "QuickMediaIngest.Desktop"


def main() -> int:
    csproj = (DESK / "QuickMediaIngest.Desktop.csproj").read_text(encoding="utf-8")
    assert "net8.0" in csproj and "Avalonia.Desktop" in csproj
    assert "QuickMediaIngest.Core" in csproj
    axaml = (DESK / "MainWindow.axaml").read_text(encoding="utf-8")
    assert axaml.count("\n") < 800
    assert 'Content="Import"' in axaml
    assert 'Content="Dry run"' in axaml
    assert 'Content="Scan"' in axaml
    assert "Scan phones" not in axaml
    assert "ShowWaitingForCard" in axaml
    assert "ShowDrivePicker" in axaml
    assert "Pictures folder" in axaml
    assert "DrivePickerHint" in axaml
    assert "SaveLocationNote" in axaml
    assert "Save location" in axaml
    assert "DestinationChipLabel" not in axaml.split('Classes="commandBar"', 1)[1].split("ShowOverflow", 1)[0]
    bar_only = axaml.split('Classes="commandBar"', 1)[1].split("ShowOverflow", 1)[0]
    assert bar_only.find('Name="RefreshButton"') < bar_only.find('Name="DestChip"') < bar_only.find('Name="ImportButton"')
    assert 'Classes="ghost destChip"' not in axaml
    assert "Button.destChip" not in (DESK / "Themes/Studio.axaml").read_text(encoding="utf-8")
    studio = (DESK / "Themes/Studio.axaml").read_text(encoding="utf-8")
    assert "FocusAdorner" in studio
    assert "Button:focus, CheckBox:focus" not in studio
    dest_chip = axaml.split('Name="DestChip"', 1)[1].split("/>", 1)[0]
    assert 'Height="44"' in dest_chip and 'MinWidth="88"' in dest_chip
    assert "Scan phones" not in axaml
    assert "NamingTemplate" in axaml
    assert "Naming.AvailableTokens" in axaml
    head, _, prefs = axaml.partition('Name="PrefsOverlay"')
    assert "Naming.AvailableTokens" not in head
    assert "NamingChipBar" not in axaml
    sheet = axaml.split('Name="IngestSheetTitle"', 1)[1]
    assert "Naming.AvailableTokens" not in sheet
    assert "Naming.IncludeDate" in sheet
    assert 'Tag="Date"' in sheet
    assert "Naming.IncludeFolderShoot" in sheet
    assert "IngestAddFolderField" not in sheet
    assert "Naming.FolderFieldLabels" not in sheet
    assert "Naming.FolderPreview" in sheet
    assert 'Classes="sidebar"' in axaml
    bar, _, overflow = axaml.partition('ShowOverflow')
    assert "HoursSlider" in bar and "ThumbZoom" in bar
    assert "HoursSlider" not in overflow
    assert "ThumbZoom" not in overflow
    assert "GroupByLabel" in axaml and "ThumbLabel" in axaml
    hours_wrap = axaml.split('Name="HoursSlider"', 1)[0][-280:]
    thumb_wrap = axaml.split('Name="ThumbZoom"', 1)[0][-280:]
    assert "ShowShootList" not in hours_wrap
    assert "ShowShootList" not in thumb_wrap
    assert 'IsVisible="{Binding ShowShootList}"' in axaml
    assert "SettingsExpander" not in axaml
    assert 'Name="SidebarPrefs"' in axaml
    assert axaml.find('Classes="sidebar"') < axaml.find('Name="DestSummaryEdit"') < axaml.find('Grid.Column="1"')
    assert "ShowEject" in axaml
    assert 'Content="View"' in axaml
    assert "Scan for cards" not in axaml
    assert "No cards yet" in axaml
    assert "CanImport" in axaml
    assert "CancelImport_Click" in axaml
    assert "PrefsImportExpander" in axaml
    assert "ShowIngestSheet" in axaml
    assert "ShowFtpFailure" in axaml
    assert "#F44336" in (ROOT / "QuickMediaIngest.Desktop/Themes/Studio.axaml").read_text(encoding="utf-8")
    assert "LiveSetting" in axaml
    assert "FileTypeDisplay" in axaml
    assert "MotionAssist" in axaml
    assert "ShowNotifyBadge" in axaml
    assert "Naming.PreviewExamples" in axaml
    assert "DeleteAfterImport" in axaml
    assert "ShowShootList" in axaml
    assert "TransportDisplay" in axaml
    assert "FileCount" in axaml
    assert "KeywordsText" in axaml
    assert "ShootExpand_Click" in axaml
    assert "ShowFtpEditor" in axaml
    assert "ShowFtpThrottle" in axaml
    assert "PreferAdb" in axaml
    assert "HasPreview" in axaml
    assert "PreviewImage" in axaml
    assert "ShowAfterglow" in axaml
    assert "IsImporting" in axaml
    assert "ShowActivityProgress" in axaml
    assert 'Name="ActivityMeter"' in axaml
    assert 'IsVisible="{Binding IsImporting}" Classes="overlayScrim"' not in axaml
    assert 'ShowNotifications}" Classes="overlayScrim"' not in axaml
    assert "IsFirstRun" in axaml
    assert "Onboarding_Body" in (DESK / "MainWindow.axaml.cs").read_text(encoding="utf-8")
    assert "WelcomeBody" in axaml
    assert "Assets/AppIcon.png" in axaml
    assert 'Name="WelcomeTitle"' in axaml and 'FontSize="24"' not in axaml.split('Name="WelcomeTitle"', 1)[1][:200]
    assert "ShowCrashOverlay" in axaml
    assert 'FontSize="14"' in axaml
    assert "#007ACC" in axaml
    assert 'MinHeight="44"' in axaml and 'MinWidth="88"' in axaml
    assert "ShowOverflow" in axaml and "ShowNotifications" in axaml
    assert "CommandBarCompact" in axaml
    assert 'x:DataType="vm:IngestBenchAppModel"' in axaml
    assert "AvaloniaUseCompiledBindingsByDefault>true" in csproj
    assert 'Content="History"' in axaml
    assert "Scan exclusions" in axaml
    assert 'Content="Feedback"' in axaml
    assert 'Content="Preferences"' in axaml
    assert 'Content="Queue"' in axaml
    assert 'Content="Pick"' in axaml
    assert 'Content="Reject"' in axaml
    assert "Prefs.WriteChecksumManifest" in axaml
    assert "Prefs.ConfirmBeforeImport" in axaml
    assert "FilterChips" in axaml
    assert "ThumbnailSize" in axaml
    assert "PreviewCachePath" in axaml
    assert "Prefs.SecondaryDestination" in axaml
    assert "Prefs.CopyrightStamp" in axaml
    assert 'Content="Split"' in axaml
    assert 'Content="Merge"' in axaml
    assert "DestFolderTemplate" in axaml
    assert "Prefs.WatchFolder" in axaml
    assert 'Content="Compare"' in axaml
    assert "ShowCompare" in axaml
    assert 'Content="Purge cache"' in axaml
    assert 'Content="Wi-Fi preset"' in axaml
    assert "Ftp.WifiBrand" in axaml
    assert 'Content="PTP"' in axaml
    assert "ShowPtpPicker" in axaml
    assert "AccessibilityAnnouncement" in axaml
    assert "ShowShortcuts" in axaml
    assert "Keyboard shortcuts" in axaml
    assert 'Content="Eject"' in axaml
    assert "EjectAfterImport" in axaml
    assert "Prefs.Theme" in axaml
    assert "Prefs.StripGps" in axaml
    assert "Prefs.SearchQuery" in axaml
    assert "AboutVersion" in axaml
    assert "AboutDonate" in axaml
    assert "ShowUserPrompt" in axaml
    assert "UserPromptIsConfirm" in axaml
    assert "VisibleHistory" in axaml
    assert "HistoryFilter" in axaml
    assert "HistoryExport" in axaml
    assert "ExcludedFolders" in axaml
    assert "ScanExclusionsOverlay" in axaml
    assert "FeedbackDescription" in axaml
    assert "FeedbackPreview" in axaml
    assert "FeedbackCanOpenGitHub" in axaml
    program = (DESK / "Program.cs").read_text(encoding="utf-8")
    assert "UidGuard.TryRefuseRoot" in program
    assert "NativeLibrarySmoke.TryHandle" in program
    app = (DESK / "App.axaml.cs").read_text(encoding="utf-8")
    assert "IngestBenchDesktop.CreateMainWindow" in app
    model = (ROOT / "QuickMediaIngest/Core/AppModel/IngestBenchAppModel.cs").read_text(encoding="utf-8")
    assert "OverlayNav Nav" in model
    print("OK: Avalonia ingest-bench binds AppModel (Import / Dry run / Refresh, empty, first-run, crash)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
