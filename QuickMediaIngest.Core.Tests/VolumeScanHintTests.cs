#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.AppModel;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class VolumeScanHintTests
{
    [Fact]
    public void GamesDriveWithoutDcim_IsNotSelectedByDefault()
    {
        string parent = Path.Combine(Path.GetTempPath(), "qmi-hint-" + Path.GetRandomFileName());
        string root = Path.Combine(parent, "Games");
        Directory.CreateDirectory(root);
        try
        {
            Assert.True(VolumeScanHint.IsDeniedLabel("Games"));
            Assert.False(VolumeScanHint.ShouldSelectByDefault(root, "Games"));
            Assert.Equal("Disk", MountEnumerator.ListVolumes(new[] { root }).Find(v => v.Path == Path.GetFullPath(root))?.Kind);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void VolumeWithDcim_IsSelectedByDefault()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-card-" + Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(root, "DCIM"));
        try
        {
            Assert.True(VolumeScanHint.HasCameraFolder(root));
            Assert.True(VolumeScanHint.ShouldSelectByDefault(root, "EOS_DIGITAL"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RememberedSelection_OverridesDefault()
    {
        var memory = new ScanSourceStore.FileDto
        {
            SeenPaths = new[] { "/media/me/Games" },
            SelectedPaths = new[] { "/media/me/Games" },
        };
        Assert.True(ScanSourceStore.IsSelected("/media/me/Games", "Games", memory));
        Assert.False(ScanSourceStore.IsSelected("adb:ABC/pictures", "ABC Pictures", new ScanSourceStore.FileDto()));
    }
}
