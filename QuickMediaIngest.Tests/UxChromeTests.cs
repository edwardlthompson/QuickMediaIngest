#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Motion;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests;

[Collection("Wpf")]
public sealed class UxChromeTests
{
    [Fact]
    public void FirstRun_DefaultsTrue()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.True(vm.IsFirstRun);
    }

    [Fact]
    public void ToolbarRetry_HiddenUntilFailedRecords()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.ShowToolbarRetryFailed);
        vm.FailedImportRecords.Add(new FailedImportRecord { SourcePath = @"E:\a.cr3", FileName = "a.cr3" });
        Assert.True(vm.ShowToolbarRetryFailed);
    }

    [Fact]
    public void ToolbarResume_FollowsPendingPlanFlag()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.ShowToolbarResumePending);
        vm.HasPendingImportPlan = true;
        Assert.True(vm.ShowToolbarResumePending);
    }

    [Fact]
    public void ToolbarQueue_HiddenUntilSelectionOrQueue()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.ShowToolbarQueue);
        vm.QueuedImportCount = 1;
        Assert.True(vm.ShowToolbarQueue);
    }

    [Fact]
    public void ToolbarRebuild_HiddenUntilGroupsExist()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.ShowToolbarRebuildPreviews);
    }

    [Fact]
    public void CopyPack_UsesCalmPhotographerVoice()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            AppLocalizer.SetCulture("en");
            Assert.Equal("No sources yet", AppLocalizer.Get("Empty_NoSourcesTitle"));
            Assert.Equal("Plug in an SD card or add a camera FTP share.", AppLocalizer.Get("Empty_NoSourcesBody"));
            Assert.Equal("Dry run", AppLocalizer.Get("Toolbar_Preflight"));
            Assert.Equal("Delete originals after import", AppLocalizer.Get("Toolbar_DeleteAfterImport"));
            Assert.Equal("Select a shoot to import.", AppLocalizer.Get("Toolbar_ImportDisabledTooltip"));
            Assert.Equal("Select a shoot, then Import.", AppLocalizer.Get("Vm_StatusNothingToImport"));
            Assert.Equal("Something went wrong", AppLocalizer.Get("Msg_Unhandled_Error_Title"));
            Assert.DoesNotContain("small utility", AppLocalizer.Get("About_Description"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("blacklist", AppLocalizer.Get("Sidebar_ScanExclusionsTooltip"), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("blacklist", AppLocalizer.Get("ScanExclusions_FoldersHeading"), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void MotionTimings_ReducedMotion_IsZero()
    {
        Assert.Equal(TimeSpan.Zero, MotionTimings.Duration(MotionTimings.OverlayEnterMs, reducedMotion: true));
        Assert.Equal(180, MotionTimings.OverlayEnterMs);
        Assert.Equal(120, MotionTimings.ChevronMs);
        Assert.Equal(260, MotionTimings.SidebarExpandedPx);
        Assert.Equal(64, MotionTimings.SidebarCollapsedPx);
        Assert.Equal(TimeSpan.FromMilliseconds(180), MotionTimings.Duration(180, reducedMotion: false));
    }

    [Fact]
    public async Task OverlayPrompt_Accept_CompletesTrue()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Task<bool> pending = vm.ConfirmAsync("Title", "Body");
        Assert.True(vm.ShowUserPromptDialog);
        Assert.True(vm.UserPromptIsConfirm);
        Assert.Equal("Title", vm.UserPromptTitle);
        vm.AcceptUserPromptCommand.Execute(null);
        Assert.True(await pending);
        Assert.False(vm.ShowUserPromptDialog);
    }

    [Fact]
    public async Task OverlayPrompt_Cancel_CompletesFalse()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Task<bool> pending = vm.ConfirmAsync("Title", "Body");
        vm.CancelUserPromptCommand.Execute(null);
        Assert.False(await pending);
    }

    [Fact]
    public void SettingsSearch_ThemeQuery_HidesNaming()
    {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try
        {
            AppLocalizer.SetCulture("en");
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.SettingsSearchQuery = "theme";
            Assert.True(vm.SettingsSectionAppearanceVisible);
            Assert.False(vm.SettingsSectionNamingVisible);
            Assert.False(vm.SettingsSearchNoResults);
            vm.SettingsSearchQuery = "xyzzy";
            Assert.True(vm.SettingsSearchNoResults);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void ImportAfterglow_StartsHidden()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.ShowImportAfterglow);
        vm.DismissImportAfterglowCommand.Execute(null);
        Assert.False(vm.ShowImportAfterglow);
    }

    [Fact]
    public void DestinationChip_UsesFolderLeaf()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        vm.DestinationRoot = "/tmp/ingest-dest";
        Assert.Equal("ingest-dest", vm.DestinationChipLabel);
    }

    [Fact]
    public void ImportScene_DoesNotDimListWhileImporting()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        Assert.False(vm.DimShootListForImport);
        vm.IsImporting = true;
        Assert.False(vm.DimShootListForImport);
        Assert.False(vm.ShowImportSceneOpenDestination);
        vm.IsImporting = false;
        Assert.True(vm.ShowImportSceneOpenDestination);
    }

    [Fact]
    public void CommandBar_CompactDocksDestination()
    {
        MainViewModel vm = MainViewModelProofTests.CreateViewModel();
        vm.ApplyWindowWidth(1200);
        Assert.True(vm.ShowDestinationChipInPrimaryBar);
        vm.ApplyWindowWidth(900);
        Assert.True(vm.CommandBarCompact);
        Assert.False(vm.ShowDestinationChipInPrimaryBar);
        Assert.Equal("…", vm.ViewOverflowHeader);
    }
}
