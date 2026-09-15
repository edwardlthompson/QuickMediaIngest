#nullable enable
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Chrome;
using QuickMediaIngest.Core.Nav;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow : Window
{
    private bool _viewReady;

    private readonly string[] _launchRoots;

    public MainWindow() : this(IngestBenchHost.Create())
    {
    }

    public MainWindow(IngestBenchHost host) : this(host, Array.Empty<string>())
    {
    }

    public MainWindow(IngestBenchHost host, string[] launchRoots)
    {
        Host = host;
        _launchRoots = launchRoots ?? Array.Empty<string>();
        DataContext = host.Model;
        InitializeComponent();
        ApplyWindowIcon();
        ApplyLocalizedChrome();
        Opened += OnWindowOpened;
        PositionChanged += (_, _) => RememberNormalBounds();
        Closing += (_, _) =>
        {
            CaptureWindowLayout();
            Host.Persist();
        };
        Closed += (_, _) =>
        {
            Bench.PropertyChanged -= OnChromePropertyChanged;
            SetPreviewBitmap(null);
            Host.Dispose();
        };
        SizeChanged += OnWindowSizeChanged;
        KeyDown += Feedback_KeyDown;
        ApplyPrefsTheme();
        AppLocalizer.SetCulture(Host.Model.Prefs.Language);
        ApplyLocalizedChrome();
        ApplyA11yChrome();
        ApplyWindowLayout();
    }

    public IngestBenchHost Host { get; }

    private IngestBenchAppModel Bench => Host.Model;

    private void ApplyLocalizedChrome()
    {
        WelcomeTitle.Text = AppLocalizer.Get("Onboarding_Title");
        WelcomeBody.Text = AppLocalizer.Get("Onboarding_Body");
        WelcomeGotIt.Content = AppLocalizer.Get("Onboarding_GotIt");
        CrashTitle.Text = AppLocalizer.Get("Msg_Unhandled_Error_Title");
        CrashClose.Content = AppLocalizer.Get("Btn_Close");
        PromptOk.Content = AppLocalizer.Get("Prompt_Ok");
        PromptCancel.Content = AppLocalizer.Get("Btn_Cancel");
        EmptyState.Text = AppLocalizer.Get("Empty_NoSourcesTitle");
        EmptyBody.Text = AppLocalizer.Get("Empty_NoSourcesBody");
        EmptyRefresh.Content = AppLocalizer.Get("Empty_ScanForCards");
        EmptyRefresh.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("Empty_ScanForCards"));
        SidebarTitle.Text = AppLocalizer.Get("Sidebar_AppName");
        SidebarSources.Text = AppLocalizer.Get("Sidebar_Sources");
        SidebarNoCards.Text = AppLocalizer.Get("Sidebar_NoCardsYet");
        SidebarSettings.Text = AppLocalizer.Get("Sidebar_SettingsHeader");
        AddFtpButton.Content = AppLocalizer.Get("Btn_AddFtpSource");
        PtpButton.Content = AppLocalizer.Get("Ptp_Button");
        PtpTitle.Text = AppLocalizer.Get("Ptp_Title");
        PtpScan.Content = AppLocalizer.Get("Ptp_Scan");
        PtpCancel.Content = AppLocalizer.Get("Btn_Cancel");
        DrivePickerTitle.Text = AppLocalizer.Get("DrivePicker_Title");
        DrivePickerHint.Text = AppLocalizer.Get("DrivePicker_Hint");
        DrivePickerScan.Content = AppLocalizer.Get("DrivePicker_Scan");
        DrivePickerCancel.Content = AppLocalizer.Get("Btn_Cancel");
        GroupByLabel.Text = AppLocalizer.Get("Toolbar_GroupBy");
        HoursSlider.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("A11y_GroupByHours"));
        ToolTip.SetTip(HoursSlider, AppLocalizer.Get("Toolbar_GroupByTooltip"));
        ThumbLabel.Text = AppLocalizer.Get("A11y_ThumbnailSize");
        ThumbZoom.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("A11y_ThumbnailSize"));
        ToolTip.SetTip(ThumbZoom, AppLocalizer.Get("Toolbar_ThumbTooltip"));
        SelectAllButton.Content = AppLocalizer.Get("Toolbar_SelectAll");
        SkipFolderButton.Content = AppLocalizer.Get("Menu_SkipFolderFuture");
        SplitButton.Content = AppLocalizer.Get("Watch_Split");
        MergeButton.Content = AppLocalizer.Get("Watch_Merge");
        RenameButton.Content = AppLocalizer.Get("Watch_Rename");
        CompareButton.Content = AppLocalizer.Get("Media_Compare");
        PrefsPurge.Content = AppLocalizer.Get("Media_PurgeCache");
        CompareClose.Content = AppLocalizer.Get("Btn_Close");
        ShortcutsTitle.Text = AppLocalizer.Get("Help_ShortcutsTitle");
        ShortcutsClose.Content = AppLocalizer.Get("Btn_Close");
        PickButton.Content = AppLocalizer.Get("Cull_Pick");
        RejectButton.Content = AppLocalizer.Get("Cull_Reject");
        ShootFilterBox.Watermark = AppLocalizer.Get("Toolbar_FilterHint");
        FtpTitle.Text = AppLocalizer.Get("Ftp_AddSourceTitle");
        FtpHostLabel.Text = AppLocalizer.Get("Ftp_Host");
        FtpPortLabel.Text = AppLocalizer.Get("Ftp_Port");
        FtpUserLabel.Text = AppLocalizer.Get("Ftp_User");
        FtpPasswordLabel.Text = AppLocalizer.Get("Ftp_Password");
        FtpFolderLabel.Text = AppLocalizer.Get("Ftp_RemoteFolder");
        FtpTest.Content = AppLocalizer.Get("Btn_Test");
        FtpBrowse.Content = AppLocalizer.Get("Ftp_Browse");
        FtpSave.Content = AppLocalizer.Get("Btn_Save");
        FtpCancel.Content = AppLocalizer.Get("Btn_Cancel");
        FtpThrottleTitle.Text = AppLocalizer.Get("Ftp_ThrottleKbps");
        FtpThrottleClose.Content = AppLocalizer.Get("Btn_Close");
        PreferAdbBox.Content = AppLocalizer.Get("Settings_PreferAdbTransfer");
        AfterglowOpen.Content = AppLocalizer.Get("ImportAfterglow_Open");
        AfterglowDismiss.Content = AppLocalizer.Get("Btn_Close");
        OverflowButton.Content = AppLocalizer.Get("Toolbar_ViewSection");
        ToolTip.SetTip(OverflowButton, AppLocalizer.Get("Toolbar_ViewSectionTooltip"));
        ToolTip.SetTip(RefreshButton, AppLocalizer.Get("Toolbar_RefreshTooltip"));
        RefreshButton.Content = AppLocalizer.Get("Empty_ScanForCards");
        RefreshButton.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("Empty_ScanForCards"));
        ToolTip.SetTip(ImportButton, Bench.CanImport ? AppLocalizer.Get("Toolbar_ImportTooltip") : AppLocalizer.Get("Toolbar_ImportDisabledTooltip"));
        DeleteAfterBox.Content = AppLocalizer.Get("Toolbar_DeleteAfterImport");
        ToolTip.SetTip(DeleteAfterBox, AppLocalizer.Get("Toolbar_DeleteAfterImportTooltip"));
        CancelImportButton.Content = AppLocalizer.Get("Btn_CancelImport");
        LiveRegion.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("A11y_StatusAnnouncements"));
        DestChip.Content = AppLocalizer.Get("Toolbar_SaveLocation");
        DestChip.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("Toolbar_SaveLocation"));
        DestSummaryTitle.Text = AppLocalizer.Get("Toolbar_SaveLocation");
        DestSummaryEdit.Content = AppLocalizer.Get("Toolbar_SaveLocation");
        DestSummaryEdit.SetValue(Avalonia.Automation.AutomationProperties.NameProperty, AppLocalizer.Get("Toolbar_SaveLocation"));
        SidebarPrefs.Content = "Preferences";
        SidebarHistory.Content = AppLocalizer.Get("Btn_ImportHistory");
        SidebarExclusions.Content = AppLocalizer.Get("ScanExclusions_Title");
        SidebarFeedback.Content = "Feedback";
        PrefsSaveLocationHint.Text = AppLocalizer.Get("Prefs_SaveLocationMoved");
        NotifyLabel.Text = AppLocalizer.Get("Notify_FlyoutTitle");
        PrefsImport.Content = AppLocalizer.Get("Settings_ImportSettingsFile");
        PrefsThemeLabel.Text = AppLocalizer.Get("Settings_Theme");
        PrefsDateFormatLabel.Text = AppLocalizer.Get("Settings_DateFormat");
        PrefsTimeFormatLabel.Text = AppLocalizer.Get("Settings_TimeFormat");
        PrefsSeparatorLabel.Text = AppLocalizer.Get("Settings_Separator");
        PrefsDestPresetLabel.Text = AppLocalizer.Get("Settings_DestinationPreset");
        PrefsDupLabel.Text = AppLocalizer.Get("Settings_DuplicateHandling");
        PrefsVerifyLabel.Text = AppLocalizer.Get("Settings_VerificationMode");
        PrefsTimeZoneLabel.Text = AppLocalizer.Get("Settings_TimeZone");
        PrefsSecondaryLabel.Text = AppLocalizer.Get("Settings_SecondCopyFolder");
        PrefsSecondary.Watermark = AppLocalizer.Get("Settings_SecondCopyFolder");
        PrefsImportHeader.Text = AppLocalizer.Get("Settings_ImportBlock");
        FtpFailTitle.Text = AppLocalizer.Get("Empty_FtpFailureTitle");
        FtpFailLead.Text = AppLocalizer.Get("Empty_FtpFailureLead");
        FtpFailClose.Content = AppLocalizer.Get("Btn_Close");
        FtpFailRefresh.Content = AppLocalizer.Get("Empty_ScanForCards");
        IngestSheetTitle.Text = AppLocalizer.Get("IngestSheet_Title");
        IngestSheetDestLabel.Text = AppLocalizer.Get("Toolbar_SaveLocation");
        IngestSheetPresetLabel.Text = AppLocalizer.Get("Settings_Preset");
        IngestSheetNamingLabel.Text = AppLocalizer.Get("Settings_FileNamingScheme");
        IngestSheetFolderLabel.Text = "Folder name";
        IngestSheetBrowse.Content = AppLocalizer.Get("Settings_Browse");
        IngestSheetClose.Content = AppLocalizer.Get("Btn_Done");
        OverflowDryRun.Content = AppLocalizer.Get("Toolbar_Preflight");
        OverflowRefresh.Content = AppLocalizer.Get("Empty_ScanForCards");
        OverflowDest.Content = AppLocalizer.Get("Toolbar_SaveLocation");
        OverflowClose.Content = AppLocalizer.Get("Btn_Close");
        OverflowPreferAdb.Content = AppLocalizer.Get("Settings_PreferAdbTransfer");
        NotifyClear.Content = AppLocalizer.Get("Notify_ClearFeed");
        NotifyClose.Content = AppLocalizer.Get("Btn_Close");
        HistoryTitle.Text = AppLocalizer.Get("Btn_ImportHistory");
        HistoryFilterBox.Watermark = AppLocalizer.Get("Toolbar_FilterHint");
        HistoryExport.Content = AppLocalizer.Get("History_Export");
        HistoryClear.Content = AppLocalizer.Get("History_Clear");
        HistoryClose.Content = AppLocalizer.Get("Btn_Close");
        ExclTitle.Text = AppLocalizer.Get("ScanExclusions_Title");
        ExclSubtitle.Text = AppLocalizer.Get("ScanExclusions_Subtitle");
        ExclAdd.Content = AppLocalizer.Get("Dlg_SelectFolderToScan");
        ExclRemove.Content = AppLocalizer.Get("ScanExclusions_RemoveFolder");
        ExclClose.Content = AppLocalizer.Get("Btn_Close");
        FeedbackBug.Content = AppLocalizer.Get("Feedback_BugTitle");
        FeedbackFeature.Content = AppLocalizer.Get("Feedback_FeatureTitle");
        FeedbackPreviewLabel.Text = AppLocalizer.Get("Feedback_Preview");
        FeedbackCopy.Content = AppLocalizer.Get("Feedback_Copy");
        FeedbackGitHub.Content = AppLocalizer.Get("Feedback_OpenGitHub");
        FeedbackDiscard.Content = AppLocalizer.Get("Feedback_Discard");
        PrefsTitle.Text = AppLocalizer.Get("Settings_MainTitle");
        PrefsSearch.Watermark = AppLocalizer.Get("Settings_SearchPlaceholder");
        PrefsNoResults.Text = AppLocalizer.Get("Settings_SearchNoResults");
        PrefsAppearance.Text = AppLocalizer.Get("Settings_Section_Appearance");
        PrefsNaming.Text = AppLocalizer.Get("Settings_FileNamingScheme");
        PrefsNamingPresetLabel.Text = AppLocalizer.Get("Settings_Preset");
        PrefsIncDate.Content = AppLocalizer.Get("Settings_Date");
        PrefsIncTime.Content = AppLocalizer.Get("Settings_Time");
        PrefsIncSeq.Content = AppLocalizer.Get("Settings_Sequence");
        PrefsIncShoot.Content = AppLocalizer.Get("Settings_ShootName");
        PrefsIncOrig.Content = AppLocalizer.Get("Settings_OriginalName");
        PrefsTokensLabel.Text = AppLocalizer.Get("Settings_AvailableTokens");
        PrefsShootSample.Watermark = AppLocalizer.Get("Settings_ShootNameSample");
        PrefsLowercase.Content = AppLocalizer.Get("Settings_ForceLowercase");
        PrefsPreviewLabel.Text = AppLocalizer.Get("Settings_PreviewExamples");
        PrefsFolderTokensLabel.Text = AppLocalizer.Get("Settings_FolderTokens");
        PrefsLanguageLabel.Text = AppLocalizer.Get("Settings_DisplayLanguage");
        PrefsGps.Content = AppLocalizer.Get("Settings_StripGps");
        PrefsChecksum.Content = AppLocalizer.Get("Settings_ChecksumManifest");
        PrefsXmp.Content = AppLocalizer.Get("Settings_WriteXmp");
        PrefsEject.Content = AppLocalizer.Get("Settings_EjectAfterImport");
        PrefsCreator.Watermark = AppLocalizer.Get("Settings_CreatorStamp");
        PrefsCopyright.Watermark = AppLocalizer.Get("Settings_CopyrightStamp");
        PrefsExport.Content = AppLocalizer.Get("History_Export");
        PrefsClose.Content = AppLocalizer.Get("Settings_SaveClose");
        PrefsAbout.Content = AppLocalizer.Get("Settings_OpenAppInfo");
        AboutTitle.Text = AppLocalizer.Get("About_Title");
        AboutBody.Text = AppLocalizer.Get("About_Description");
        AboutVersionLabel.Text = AppLocalizer.Get("About_Version");
        AboutDonate.Content = AppLocalizer.Get("About_DonateVenmo");
        AboutCheck.Content = AppLocalizer.Get("About_CheckUpdatesNow");
        AboutClose.Content = AppLocalizer.Get("Btn_Close");
        QueueButton.Content = AppLocalizer.Get("Toolbar_Queue");
        RetryButton.Content = AppLocalizer.Get("Toolbar_RetryFailed");
        ResumeButton.Content = AppLocalizer.Get("Toolbar_ResumePending");
        EjectButton.Content = AppLocalizer.Get("Toolbar_Eject");
    }

    private static IngestBenchCopy LocCopy { get; } = new(
        AppLocalizer.Get,
        (key, args) => AppLocalizer.Format(key, args));

    private async void Import_Click(object? sender, RoutedEventArgs e)
    {
        if (!Bench.CanImport)
        {
            return;
        }

        await Host.RunImportAsync(dryRun: false, LocCopy);
    }

    private async void CancelImport_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchImportCancel.RequestAsync(Host, LocCopy);

    private void IngestSheet_Click(object? sender, RoutedEventArgs e) => Bench.ShowIngestSheet = true;

    private void IngestSheetClose_Click(object? sender, RoutedEventArgs e) => Bench.ShowIngestSheet = false;

    private void FtpFailClose_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.DismissFailure(Host);

    private async void DryRun_Click(object? sender, RoutedEventArgs e) =>
        await Host.RunImportAsync(dryRun: true, LocCopy);

    private void ShootFilter_LostFocus(object? sender, RoutedEventArgs e) =>
        IngestBenchShoots.ApplyFilter(Host);

    private void SelectAll_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchShoots.ToggleSelectAll(Host);

    private void SkipFolder_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchShoots.SkipVisibleSelected(Host);

    private void Scan_Click(object? sender, RoutedEventArgs e) => IngestBenchScan.BeginPick(Host);

    private void OnWindowOpened(object? sender, System.EventArgs e)
    {
        _viewReady = true;
        ApplyWindowLayout();
        Bench.PropertyChanged += OnChromePropertyChanged;
        if (_launchRoots.Length > 0 && LaunchMediaRoots.Apply(Host, _launchRoots))
        {
            _ = ScanSelectedAsync();
            return;
        }

        IngestBenchScan.BeginPick(Host);
    }

    private void OnChromePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_viewReady)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(IngestBenchAppModel.DeleteAfterImport):
            case nameof(IngestBenchAppModel.ThumbnailSize):
            case nameof(IngestBenchAppModel.TimeBetweenShootsHours):
            case nameof(IngestBenchAppModel.FilterFileType):
            case nameof(IngestBenchAppModel.PreferAdb):
            case nameof(IngestBenchAppModel.AllGroupsExpanded):
                Host.Persist();
                break;
            case nameof(IngestBenchAppModel.SelectedCullItem):
                ShowSelectedPreview();
                break;
        }
    }

    private async Task ScanSelectedAsync()
    {
        await IngestBenchScan.RunSelectedAsync(Host);
        string summary = Bench.ImportStatus;
        IngestBenchActivity.Begin(Host, IngestBenchActivity.Thumbs, "Building thumbnails…");
        ThumbFillResult thumbs = await Task.Run(() => IngestBenchThumbs.FillShootPreviews(Host));
        IngestBenchActivity.Idle(Host);
        string thumbsLine = IngestBenchActivity.Counts("Thumbnails", thumbs.Succeeded, thumbs.Failed);
        Bench.ImportStatus = string.IsNullOrWhiteSpace(summary) ? thumbsLine : summary + "  " + thumbsLine;
        Bench.Notifications.Insert(0, thumbsLine);
        Bench.UnreadNotificationCount++;
        ShowSelectedPreview();
    }

    private async void ConfirmDrive_Click(object? sender, RoutedEventArgs e)
    {
        Host.ConfirmDrivePick();
        await ScanSelectedAsync();
    }

    private void CancelDrive_Click(object? sender, RoutedEventArgs e) => Host.CancelDrivePick();

    private void AddFtp_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.Open(Host);

    private async void FtpTest_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchFtp.TestAsync(Host);

    private async void FtpBrowse_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchFtp.BrowseAsync(Host);

    private void FtpSave_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.Save(Host);

    private void FtpWifi_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.ApplyWifiPreset(Host);

    private void FtpCancel_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.Close(Host);

    private void FtpThrottle_Click(object? sender, RoutedEventArgs e) => IngestBenchFtp.OpenThrottle(Host);

    private void FtpThrottleClose_Click(object? sender, RoutedEventArgs e) =>
        Host.Model.ShowFtpThrottle = false;

    private void Naming_LostFocus(object? sender, RoutedEventArgs e) =>
        Host.SetNamingTemplate(Bench.NamingTemplate);

    private async void BrowseDest_Click(object? sender, RoutedEventArgs e) =>
        await PickDestinationAsync();

    private async Task PickDestinationAsync()
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = AppLocalizer.Get("Dlg_SelectDestinationFolder"),
                AllowMultiple = false,
            });
        if (folders.Count == 0)
        {
            return;
        }

        string? local = folders[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(local))
        {
            IngestBenchDest.RememberBrowse(Host, local);
        }
    }

    private void Overflow_Click(object? sender, RoutedEventArgs e) => Bench.ShowOverflow = true;

    private void OverflowClose_Click(object? sender, RoutedEventArgs e) => Bench.ShowOverflow = false;

    private void Notify_Click(object? sender, RoutedEventArgs e)
    {
        if (Bench.ShowNotifications)
        {
            Bench.ShowNotifications = false;
            return;
        }

        Bench.UnreadNotificationCount = 0;
        Bench.ShowNotifications = true;
    }

    private void NotifyClose_Click(object? sender, RoutedEventArgs e) => Bench.ShowNotifications = false;

    private void NotifyClear_Click(object? sender, RoutedEventArgs e)
    {
        Bench.Notifications.Clear();
        Bench.UnreadNotificationCount = 0;
    }

    private async void AfterglowOpen_Click(object? sender, RoutedEventArgs e)
    {
        string folder = Bench.AfterglowFolder;
        var top = TopLevel.GetTopLevel(this);
        if (top is not null && Directory.Exists(folder))
        {
            try
            {
                string full = Path.GetFullPath(folder);
                if (!full.EndsWith(Path.DirectorySeparatorChar))
                {
                    full += Path.DirectorySeparatorChar;
                }

                if (await top.Launcher.LaunchUriAsync(new Uri(full)))
                {
                    return;
                }
            }
            catch
            {
                // fall through to xdg-open
            }
        }

        IngestBenchScene.OpenFolder(Bench);
    }

    private void AfterglowDismiss_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchScene.Dismiss(Bench);

    private void DismissFirstRun_Click(object? sender, RoutedEventArgs e) => Host.DismissOnboarding();

    private void DismissCrash_Click(object? sender, RoutedEventArgs e) => Host.DismissCrash();

    private void PromptOk_Click(object? sender, RoutedEventArgs e) => Host.CompletePrompt(true);

    private void PromptCancel_Click(object? sender, RoutedEventArgs e) => Host.CompletePrompt(false);

    private void History_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchHistory.ApplyFilter(Host);
        ShowOverlay(OverlayId.History);
    }

    private void HistoryFilter_LostFocus(object? sender, RoutedEventArgs e) =>
        IngestBenchHistory.ApplyFilter(Host);

    private async void HistoryExport_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = AppLocalizer.Get("Vm_ExportImportHistoryTitle"),
                SuggestedFileName = "import-history.csv",
                DefaultExtension = "csv",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("CSV") { Patterns = new[] { "*.csv" } },
                },
            });
        if (file is null)
        {
            return;
        }

        string? local = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(local))
        {
            return;
        }

        await File.WriteAllTextAsync(local, IngestBenchHistory.ExportCsv(Host));
    }

    private async void HistoryClear_Click(object? sender, RoutedEventArgs e) =>
        await IngestBenchHistory.ClearAsync(Host, LocCopy);

    private void ScanExclusions_Click(object? sender, RoutedEventArgs e) => ShowOverlay(OverlayId.ScanExclusions);

    private async void ExclAdd_Click(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = AppLocalizer.Get("ScanExclusions_FoldersHeading"),
                AllowMultiple = false,
            });
        if (folders.Count == 0)
        {
            return;
        }

        string? local = folders[0].TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(local))
        {
            IngestBenchExclusions.Add(Host, local);
        }
    }

    private void ExclRemove_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchExclusions.Remove(Host, Bench.SelectedExclusion);

    private void Feedback_Click(object? sender, RoutedEventArgs e) => OpenFeedback("bug");

    private void FeedbackBug_Click(object? sender, RoutedEventArgs e) => OpenFeedback("bug");

    private void FeedbackFeature_Click(object? sender, RoutedEventArgs e) => OpenFeedback("feature");

    private void OpenFeedback(string kind)
    {
        IngestBenchFeedback.Open(Host, kind, LocCopy);
        RefreshOverlays();
    }

    private async void FeedbackBody_LostFocus(object? sender, RoutedEventArgs e)
    {
        IngestBenchFeedback.Refresh(Host, LocCopy);
        await IngestBenchFeedback.SearchAsync(Host);
    }

    private async void FeedbackCopy_Click(object? sender, RoutedEventArgs e)
    {
        if (Clipboard is not null)
        {
            await Clipboard.SetTextAsync(Bench.FeedbackPreview ?? string.Empty);
        }
    }

    private void FeedbackGitHub_Click(object? sender, RoutedEventArgs e) =>
        IngestBenchFeedback.TryOpenGitHub(Host);

    private void FeedbackDiscard_Click(object? sender, RoutedEventArgs e)
    {
        IngestBenchFeedback.Discard(Host);
        RefreshOverlays();
    }

    private void Feedback_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            Bench.ShowShortcuts = true;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.E && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            e.Handled = true;
            _ = IngestBenchEject.EjectNowAsync(Host, groups: null, LocCopy);
            return;
        }

        if (e.Key == Key.Escape && Bench.ShowShortcuts)
        {
            Bench.ShowShortcuts = false;
            e.Handled = true;
            return;
        }

        if (!Bench.Nav.IsVisible(OverlayId.Feedback))
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            FeedbackDiscard_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (Bench.FeedbackCanOpenGitHub)
            {
                IngestBenchFeedback.TryOpenGitHub(Host);
            }
            else
            {
                FeedbackCopy_Click(sender, e);
            }

            e.Handled = true;
        }
    }

    private void CloseOverlay_Click(object? sender, RoutedEventArgs e)
    {
        Bench.Nav.Pop();
        RefreshOverlays();
    }

    private void ShowOverlay(OverlayId id)
    {
        Bench.Nav.Push(id);
        RefreshOverlays();
    }

    private void RefreshOverlays()
    {
        OverlayNav nav = Bench.Nav;
        HistoryOverlay.IsVisible = nav.IsVisible(OverlayId.History);
        ScanExclusionsOverlay.IsVisible = nav.IsVisible(OverlayId.ScanExclusions);
        FeedbackOverlay.IsVisible = nav.IsVisible(OverlayId.Feedback);
        PrefsOverlay.IsVisible = nav.IsVisible(OverlayId.Settings);
        AboutOverlay.IsVisible = nav.IsVisible(OverlayId.About);
    }
}
