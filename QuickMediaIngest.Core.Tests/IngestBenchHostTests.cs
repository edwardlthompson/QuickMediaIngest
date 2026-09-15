#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.CrashCapture;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

file sealed class TempAppPaths : IAppPaths
{
    public TempAppPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-host-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}

public sealed class IngestBenchHostTests
{
    [Fact]
    public void Create_UsesInjectedPaths_FirstRunUntilDismissed()
    {
        var paths = new TempAppPaths();
        string? prior = Environment.GetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv);
        try
        {
            Environment.SetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv, null);
            IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
            Assert.True(host.Model.IsFirstRun);
            host.DismissOnboarding();
            Assert.False(host.Model.IsFirstRun);

            IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
            Assert.False(again.Model.IsFirstRun);
        }
        finally
        {
            Environment.SetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv, prior);
        }
    }

    [Fact]
    public void SkipOnboardingEnv_HidesWelcome()
    {
        var paths = new TempAppPaths();
        string? prior = Environment.GetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv);
        try
        {
            Environment.SetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv, "1");
            IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
            Assert.False(host.Model.IsFirstRun);
        }
        finally
        {
            Environment.SetEnvironmentVariable(IngestBenchHost.SkipOnboardingEnv, prior);
        }
    }

    [Fact]
    public void PendingCrash_ShowsOverlay_UntilDismissed()
    {
        var paths = new TempAppPaths();
        var store = new FilePendingCrashStore(paths);
        store.Replace(new PendingCrash { Fingerprint = "abc", ExceptionType = "Boom", Description = "disk full" });
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.True(host.Model.ShowCrashOverlay);
        Assert.Equal("disk full", host.Model.CrashOverlayText);
        host.DismissCrash();
        Assert.False(host.Model.ShowCrashOverlay);

        IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.False(again.Model.ShowCrashOverlay);
    }

    [Fact]
    public void DiscardedCrash_DoesNotShow()
    {
        var paths = new TempAppPaths();
        var store = new FilePendingCrashStore(paths);
        store.Replace(new PendingCrash { Fingerprint = "skip-me", ExceptionType = "X" });
        store.MarkDiscarded("skip-me");
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.False(host.Model.ShowCrashOverlay);
    }

    [Fact]
    public void DismissCrash_DiscardsFingerprint_ReplaceDoesNotReshow()
    {
        var paths = new TempAppPaths();
        var store = new FilePendingCrashStore(paths);
        store.Replace(new PendingCrash { Fingerprint = "fp-1", ExceptionType = "X", Description = "boom" });
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.True(host.Model.ShowCrashOverlay);
        host.DismissCrash();
        store.Replace(new PendingCrash { Fingerprint = "fp-1", ExceptionType = "X", Description = "again" });
        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.False(again.Model.ShowCrashOverlay);
    }

    [Fact]
    public async System.Threading.Tasks.Task Prompt_NotifyAndConfirm()
    {
        var paths = new TempAppPaths();
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        var notify = host.Prompt.NotifyAsync("t", "b");
        Assert.True(host.Model.ShowUserPrompt);
        Assert.False(host.Model.UserPromptIsConfirm);
        host.CompletePrompt(true);
        await notify;

        var confirm = host.Prompt.ConfirmAsync("c", "d");
        Assert.True(host.Model.UserPromptIsConfirm);
        host.CompletePrompt(false);
        Assert.False(await confirm);
    }

    [Fact]
    public void ConfirmDrivePick_CountsMediaInSelectedExtraRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-scan-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "a.jpg"), new byte[] { 1 });
        File.WriteAllText(Path.Combine(root, "notes.txt"), "no");
        var paths = new TempAppPaths();
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        try
        {
            host.BeginDrivePick(new[] { root });
            Assert.True(host.Model.ShowDrivePicker);
            string full = Path.GetFullPath(root);
            foreach (VolumeChoice volume in host.Model.Volumes)
            {
                volume.IsSelected = string.Equals(volume.Path, full, StringComparison.Ordinal);
            }

            host.ConfirmDrivePick();
            Assert.False(host.Model.ShowDrivePicker);
            Assert.Equal(1, host.Model.SourceCount);
            Assert.False(host.Model.ShowWaitingForCard);
        }
        finally
        {
            host.Dispose();
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // temp
            }
        }
    }

    [Fact]
    public void RefreshScan_OpensPickerWhenNoRoots()
    {
        var paths = new TempAppPaths();
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        host.RefreshScan();
        Assert.True(host.Model.ShowDrivePicker);
    }

    [Fact]
    public void SetDestination_PersistsChipAndNaming()
    {
        var paths = new TempAppPaths();
        using (IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths))
        {
            string dest = Path.Combine(paths.AppDataRoot, "out");
            Directory.CreateDirectory(dest);
            host.SetDestination(dest);
            host.SetNamingTemplate("[Original]");
            Assert.Equal(Path.GetFullPath(dest), host.Model.DestinationRoot);
            Assert.Equal("out", host.Model.DestinationChipLabel);
        }

        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths);
        Assert.EndsWith("out", again.Model.DestinationRoot);
        Assert.Equal("[Original]", again.Model.NamingTemplate);
        Assert.Equal("out", again.Model.DestinationChipLabel);
        Assert.Contains("[Original]", again.Model.Naming.SelectedTokens);
    }

    [Fact]
    public void PicturesPreset_DoesNotOverwriteCustomDestination()
    {
        var paths = new TempAppPaths();
        string dest = Path.Combine(paths.AppDataRoot, "shoots");
        Directory.CreateDirectory(dest);
        File.WriteAllText(
            Path.Combine(paths.AppDataRoot, "dest.json"),
            "{\"DestinationRoot\":\"" + dest.Replace("\\", "/") + "\",\"NamingTemplate\":\"[Original]\",\"DestFolderTemplate\":\"\",\"NamingPreset\":\"Custom\",\"NamingLowercase\":true,\"NamingShootNameSample\":\"my-shoot\"}");
        File.WriteAllText(
            Path.Combine(paths.AppDataRoot, "prefs.json"),
            "{ \"DestinationPreset\": \"Pictures\" }");
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Equal(Path.GetFullPath(dest), Path.GetFullPath(host.Model.DestinationRoot));
        Assert.Equal("Custom", host.Model.Prefs.DestinationPreset);

        string other = Path.Combine(paths.AppDataRoot, "other");
        Directory.CreateDirectory(other);
        IngestBenchDest.RememberBrowse(host, other);
        Assert.Equal("Custom", host.Model.Prefs.DestinationPreset);
        using IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Equal(Path.GetFullPath(other), Path.GetFullPath(again.Model.DestinationRoot));
        Assert.Equal("Custom", again.Model.Prefs.DestinationPreset);
    }
}
