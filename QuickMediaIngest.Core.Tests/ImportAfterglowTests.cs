#nullable enable
using QuickMediaIngest.Core.ImportUi;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class ImportAfterglowTests
{
    [Fact]
    public void FolderLabel_UsesLeafName()
    {
        Assert.Equal("Shoot", ImportAfterglow.FolderLabel(@"C:\Pictures\Shoot"));
        Assert.Equal("Shoot", ImportAfterglow.FolderLabel("/home/ed/Pictures/Shoot/"));
        Assert.Equal(string.Empty, ImportAfterglow.FolderLabel("  "));
    }
}
