#nullable enable
using QuickMediaIngest.Core.Chrome;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class CommandBarOverflowTests
{
    [Fact]
    public void Compact_Under1100()
    {
        Assert.True(CommandBarOverflow.IsCompact(900));
        Assert.True(CommandBarOverflow.IsCompact(1099));
        Assert.False(CommandBarOverflow.IsCompact(1100));
        Assert.False(CommandBarOverflow.IsCompact(0));
    }
}
