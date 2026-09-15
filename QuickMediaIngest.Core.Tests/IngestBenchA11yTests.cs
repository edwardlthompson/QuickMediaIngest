#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Chrome;
using QuickMediaIngest.Core.Motion;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchA11yTests
{
    [Fact]
    public void Detect_HonorsEnvAndTokens()
    {
        Assert.Equal(0d, HighContrastTokens.OverlayBlurRadius(true));
        Assert.Equal(TimeSpan.Zero, MotionTimings.Duration(180, reducedMotion: true));
        Assert.False(UiReadingOrder.IsRightToLeft("en"));
        Assert.True(UiReadingOrder.IsRightToLeft("ar"));

        string? priorHc = Environment.GetEnvironmentVariable(IngestBenchA11y.HighContrastEnv);
        string? priorRm = Environment.GetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv);
        string? priorGtk = Environment.GetEnvironmentVariable("GTK_THEME");
        try
        {
            Environment.SetEnvironmentVariable(IngestBenchA11y.HighContrastEnv, "1");
            Environment.SetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv, "1");
            Environment.SetEnvironmentVariable("GTK_THEME", null);
            Assert.True(IngestBenchA11y.DetectHighContrast());
            Assert.True(IngestBenchA11y.DetectReducedMotion());

            Environment.SetEnvironmentVariable(IngestBenchA11y.HighContrastEnv, null);
            Environment.SetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv, null);
            Environment.SetEnvironmentVariable("GTK_THEME", "HighContrast");
            Assert.True(IngestBenchA11y.DetectHighContrast());
        }
        finally
        {
            Environment.SetEnvironmentVariable(IngestBenchA11y.HighContrastEnv, priorHc);
            Environment.SetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv, priorRm);
            Environment.SetEnvironmentVariable("GTK_THEME", priorGtk);
        }
    }

    [Fact]
    public void Apply_SetsLiveRegionRtlAndFlags()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-a11y-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        using IngestBenchHost host = IngestBenchHost.Create(
            InlineUiDispatcher.Instance,
            new A11yPaths(root),
            SilentUserPrompt.Instance);
        host.Model.Prefs.Language = "ar";
        string? priorHc = Environment.GetEnvironmentVariable(IngestBenchA11y.HighContrastEnv);
        string? priorRm = Environment.GetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv);
        try
        {
            Environment.SetEnvironmentVariable(IngestBenchA11y.HighContrastEnv, "1");
            Environment.SetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv, "1");
            IngestBenchA11y.Apply(host);
            Assert.True(host.Model.HighContrast);
            Assert.True(host.Model.ReducedMotion);
            Assert.True(host.Model.IsRightToLeft);
            Assert.Equal(0d, host.Model.OverlayBlurRadius);
            IngestBenchA11y.Announce(host.Model, "Imported 2 files");
            Assert.Equal("Imported 2 files", host.Model.AccessibilityAnnouncement);
            IngestBenchA11y.Announce(host.Model, null);
            Assert.Equal(string.Empty, host.Model.AccessibilityAnnouncement);
        }
        finally
        {
            Environment.SetEnvironmentVariable(IngestBenchA11y.HighContrastEnv, priorHc);
            Environment.SetEnvironmentVariable(IngestBenchA11y.ReducedMotionEnv, priorRm);
        }
    }
}

file sealed class A11yPaths : IAppPaths
{
    public A11yPaths(string root) => AppDataRoot = root;

    public string AppDataRoot { get; }
}
