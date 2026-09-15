#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Prompt;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class LaunchMediaRootsTests
{
    [Fact]
    public void Parse_SkipsFlags_AcceptsFileUriAndFolder()
    {
        string dir = Path.Combine(Path.GetTempPath(), "qmi-launch-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            string uri = new Uri(dir).AbsoluteUri;
            string[] roots = LaunchMediaRoots.Parse(new[] { "--smoke-native", uri, dir });
            Assert.Single(roots);
            Assert.Equal(Path.GetFullPath(dir), roots[0]);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Parse_FileArg_UsesParentDirectory()
    {
        string dir = Path.Combine(Path.GetTempPath(), "qmi-launchf-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        string photo = Path.Combine(dir, "shot.jpg");
        File.WriteAllBytes(photo, new byte[] { 1 });
        try
        {
            string[] roots = LaunchMediaRoots.Parse(new[] { photo });
            Assert.Equal(Path.GetFullPath(dir), Assert.Single(roots));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Apply_SelectsPassedCard_AndClosesPicker()
    {
        string dir = Path.Combine(Path.GetTempPath(), "qmi-card-" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "DCIM"));
        using IngestBenchHost host = IngestBenchHost.Create(InlineUiDispatcher.Instance, new LaunchPaths(), SilentUserPrompt.Instance);
        try
        {
            Assert.True(LaunchMediaRoots.Apply(host, new[] { dir }));
            Assert.False(host.Model.ShowDrivePicker);
            Assert.Contains(host.Model.Volumes, v => v.IsSelected && v.Path == Path.GetFullPath(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ImportDesktop_ClaimsCameraCardMimeTypes()
    {
        string? desktop = FindPackagingDesktop("quick-media-ingest-import.desktop");
        Assert.NotNull(desktop);
        string text = File.ReadAllText(desktop);
        Assert.Contains("x-content/image-dcf", text, StringComparison.Ordinal);
        Assert.Contains("x-content/image-picturecd", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NoDisplay=true", text, StringComparison.Ordinal);
        Assert.Contains("%U", text, StringComparison.Ordinal);
        Assert.Contains("Icon=quick-media-ingest", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MenuDesktop_IsChooserVisibleCameraCardHandler()
    {
        string? desktop = FindPackagingDesktop("quick-media-ingest.desktop");
        Assert.NotNull(desktop);
        string text = File.ReadAllText(desktop);
        Assert.Contains("x-content/image-dcf", text, StringComparison.Ordinal);
        Assert.Contains("%U", text, StringComparison.Ordinal);
        Assert.DoesNotContain("NoDisplay=true", text, StringComparison.Ordinal);
        Assert.Contains("Icon=quick-media-ingest", text, StringComparison.Ordinal);
        Assert.Contains("StartupWMClass=quick-media-ingest", text, StringComparison.Ordinal);
    }

    private static string? FindPackagingDesktop(string fileName)
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            string path = Path.Combine(dir, "packaging", "debian", fileName);
            if (File.Exists(path))
            {
                return path;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}

file sealed class LaunchPaths : IAppPaths
{
    public LaunchPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-launch-app-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}
