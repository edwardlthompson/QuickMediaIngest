#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class MountEnumeratorTests
{
    [Fact]
    public void ListVolumes_IncludesExtraRoot()
    {
        string root = Path.Combine(Path.GetTempPath(), "qmi-mount-" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            var list = MountEnumerator.ListVolumes(new[] { root });
            Assert.Contains(list, v => v.Path == Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
