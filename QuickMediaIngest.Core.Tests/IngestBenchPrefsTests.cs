#nullable enable
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class IngestBenchPrefsTests
{
    [Fact]
    public void Search_HidesUnmatchedSections()
    {
        var prefs = new PrefsState();
        prefs.SearchQuery = "gps";
        prefs.ApplySearch();
        Assert.False(prefs.ShowAppearance);
        Assert.True(prefs.ShowImport);
        Assert.False(prefs.ShowNoResults);
        prefs.SearchQuery = "zzzz";
        prefs.ApplySearch();
        Assert.True(prefs.ShowNoResults);
    }

    [Fact]
    public void Json_RoundTripsThemeLanguageAndGps()
    {
        string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "qmi-prefs-" + System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(root);
        var paths = new PrefsPaths(root);
        IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        host.Model.Prefs.Theme = "Light";
        host.Model.Prefs.Language = "fr";
        host.Model.Prefs.StripGps = true;
        IngestBenchPrefs.Save(host);
        string json = IngestBenchPrefs.ExportJson(host);
        Assert.Contains("Light", json);
        Assert.DoesNotContain("FtpPass", json);

        IngestBenchHost again = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.Equal("Light", again.Model.Prefs.Theme);
        Assert.Equal("fr", again.Model.Prefs.Language);
        Assert.True(again.Model.Prefs.StripGps);

        host.Model.DeleteAfterImport = true;
        host.Model.ThumbnailSize = 180;
        host.Model.TimeBetweenShootsHours = 6;
        host.Model.FilterFileType = "RAW";
        host.Model.PreferAdb = false;
        IngestBenchPrefs.Save(host);
        string saved = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "prefs.json"));
        Assert.Contains("DeleteAfterImport", saved, System.StringComparison.Ordinal);
        Assert.Contains("ThumbnailSize", saved, System.StringComparison.Ordinal);
        Assert.Contains("TimeBetweenShootsHours", saved, System.StringComparison.Ordinal);
        Assert.True(PrefsStore.Parse(saved).DeleteAfterImport);
        Assert.Equal(180, PrefsStore.Parse(saved).ThumbnailSize);
        Assert.Equal(6, PrefsStore.Parse(saved).TimeBetweenShootsHours);
        IngestBenchHost chrome = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.True(chrome.Model.DeleteAfterImport);
        Assert.Equal(180, chrome.Model.ThumbnailSize);
        Assert.Equal(6, chrome.Model.TimeBetweenShootsHours);
        Assert.Equal("RAW", chrome.Model.FilterFileType);
        Assert.False(chrome.Model.PreferAdb);

        host.Model.AllGroupsExpanded = false;
        IngestBenchPrefs.Save(host);
        IngestBenchHost collapsed = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.False(collapsed.Model.AllGroupsExpanded);

        host.Model.Prefs.WindowWidth = 1440;
        host.Model.Prefs.WindowHeight = 900;
        host.Model.Prefs.WindowLeft = 120;
        host.Model.Prefs.WindowTop = 80;
        host.Model.Prefs.WindowPositionSet = true;
        host.Model.Prefs.WindowMaximized = false;
        host.Model.Prefs.PreviewPaneWidth = 480;
        IngestBenchPrefs.Save(host);
        string layoutJson = System.IO.File.ReadAllText(System.IO.Path.Combine(root, "prefs.json"));
        Assert.Contains("WindowPositionSet", layoutJson, System.StringComparison.Ordinal);
        Assert.Contains("PreviewPaneWidth", layoutJson, System.StringComparison.Ordinal);
        IngestBenchHost placed = IngestBenchHost.Create(InlineUiDispatcher.Instance, paths, SilentUserPrompt.Instance);
        Assert.True(PrefsLayout.HasSavedSize(placed.Model.Prefs.WindowWidth, placed.Model.Prefs.WindowHeight));
        Assert.Equal(1440, placed.Model.Prefs.WindowWidth);
        Assert.Equal(120, placed.Model.Prefs.WindowLeft);
        Assert.True(placed.Model.Prefs.WindowPositionSet);
        Assert.Equal(480, placed.Model.Prefs.PreviewPaneWidth);

        Assert.True(IngestBenchPrefs.ImportJson(again, "{\"Theme\":\"Dark\",\"Language\":\"en\",\"StripGps\":false}"));
        Assert.Equal("Dark", again.Model.Prefs.Theme);
        Assert.False(again.Model.Prefs.StripGps);
    }

    private sealed class PrefsPaths : IAppPaths
    {
        public PrefsPaths(string root) => AppDataRoot = root;

        public string AppDataRoot { get; }
    }
}
