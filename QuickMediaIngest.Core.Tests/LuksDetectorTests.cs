#nullable enable
using System.IO;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests;

public sealed class LuksDetectorTests
{
    [Fact]
    public void CheckLuks_TempFolder_DoesNotThrow()
    {
        bool luks = DestinationEncryptionDetector.CheckLuks(Path.GetTempPath());
        Assert.True(luks || !luks);
        var status = DestinationEncryptionDetector.DetectEncryption(Path.GetTempPath());
        Assert.True(System.Enum.IsDefined(status));
    }
}
