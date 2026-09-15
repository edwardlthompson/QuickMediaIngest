#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class VolumeScanTests
{
    [Fact]
    public void CountMedia_CountsImages_SkipsTrashAndNonMedia()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-volscan-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "shot.jpg"), new byte[] { 1 });
        File.WriteAllText(Path.Combine(root, "readme.txt"), "x");
        string trash = Path.Combine(root, ".Trash");
        Directory.CreateDirectory(trash);
        File.WriteAllBytes(Path.Combine(trash, "gone.jpg"), new byte[] { 2 });
        try
        {
            Assert.Equal(1, VolumeScan.CountMedia(new[] { root }));
            Assert.Equal(0, VolumeScan.CountMedia(new[] { Path.Combine(root, "missing") }));
            string nested = Path.Combine(root, "skipme");
            Directory.CreateDirectory(nested);
            File.WriteAllBytes(Path.Combine(nested, "hidden.jpg"), new byte[] { 3 });
            Assert.Equal(2, VolumeScan.CountMedia(new[] { root }));
            Assert.Equal(1, VolumeScan.CountMedia(new[] { root }, excludeFolders: new[] { nested }));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
