#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

file sealed class TempAppPaths : IAppPaths
{
    public TempAppPaths()
    {
        AppDataRoot = Path.Combine(Path.GetTempPath(), "qmi-l2-" + Path.GetRandomFileName());
        Directory.CreateDirectory(AppDataRoot);
    }

    public string AppDataRoot { get; }
}

public sealed class LinuxAdapterTests
{
    [Fact]
    public void UidGuard_CurrentProcess_IsNotRoot()
    {
        Assert.True(UidGuard.TryRefuseRoot(out string message));
        Assert.Equal(string.Empty, message);
        Assert.False(UidGuard.IsRoot());
    }

    [Fact]
    public void PathWatch_DebounceAndPoll()
    {
        Assert.Equal(500, PathWatchTimings.DebounceMilliseconds);
        Assert.Equal(3000, PathWatchTimings.PollMilliseconds);
        string dir = Path.Combine(Path.GetTempPath(), "qmi-watch-" + Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            using var watcher = new DebouncedPathWatcher(dir, () => { });
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // temp
            }
        }
    }

    [Fact]
    public void FileFtpSecrets_RoundTripAndChmod()
    {
        var paths = new TempAppPaths();
        var store = new FileFtpCredentialStore(paths);
        store.WritePassword("ftp.example.com", 21, "u", "s3cret");
        Assert.True(store.TryReadPassword("ftp.example.com", 21, out string password));
        Assert.Equal("s3cret", password);
        string file = Path.Combine(paths.AppDataRoot, "ftp-secrets", FileFtpCredentialStore.FileName("ftp.example.com", 21));
        Assert.True(File.Exists(file));
        if (!OperatingSystem.IsWindows())
        {
            UnixFileMode mode = File.GetUnixFileMode(file);
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, mode & (UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead));
        }

        store.DeletePassword("ftp.example.com", 21);
        Assert.False(store.TryReadPassword("ftp.example.com", 21, out _));
    }

    [Fact]
    public void SecretService_FallsBackTo0600File()
    {
        var paths = new TempAppPaths();
        var store = new SecretServiceFtpCredentialStore(paths);
        store.WritePassword("ftp.example.com", 21, "u", "from-file");
        Assert.True(store.TryReadPassword("ftp.example.com", 21, out string password));
        Assert.Equal("from-file", password);
    }

    [Fact]
    public void Trash_UnlinkFallback_DeletesFile()
    {
        string path = Path.Combine(Path.GetTempPath(), "qmi-trash-" + Path.GetRandomFileName());
        File.WriteAllText(path, "x");
        var trash = new GioTrashService();
        Assert.True(trash.TryTrash(path));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void XdgAppPaths_EndsWithProductFolder()
    {
        Assert.EndsWith("QuickMediaIngest", new XdgAppPaths().AppDataRoot);
    }

    [Fact]
    public void ShellOpen_Empty_Fails()
    {
        Assert.False(ShellOpen.TryOpen(""));
        Assert.False(ShellOpen.TryOpen(null));
    }

    [Fact]
    public void ShellOpen_LinuxStart_KeepsSpacesInOneArgument()
    {
        var info = ShellOpen.LinuxStart("xdg-open", "/media/edward/8TB-Games/MEDIA/01 - Unedited");
        Assert.Equal("xdg-open", info.FileName);
        Assert.False(info.UseShellExecute);
        Assert.Equal(new[] { "/media/edward/8TB-Games/MEDIA/01 - Unedited" }, info.ArgumentList);
        Assert.DoesNotContain("--", info.ArgumentList);
    }

    [Fact]
    public void ShellOpen_MissingFolder_Fails()
    {
        Assert.False(ShellOpen.TryOpen(Path.Combine(Path.GetTempPath(), "qmi-no-dir-" + Path.GetRandomFileName())));
    }
}
