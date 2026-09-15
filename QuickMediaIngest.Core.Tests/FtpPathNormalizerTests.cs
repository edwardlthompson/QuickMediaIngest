#nullable enable
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpPathNormalizerTests
    {
        [Theory]
        [InlineData(null, "/")]
        [InlineData("", "/")]
        [InlineData("   ", "/")]
        [InlineData("DCIM", "/DCIM")]
        [InlineData("/DCIM/100", "/DCIM/100")]
        [InlineData("\\DCIM\\100", "/DCIM/100")]
        [InlineData("/DCIM/./100", "/DCIM/100")]
        [InlineData("/DCIM/100/../200", "/DCIM/200")]
        [InlineData("/../etc/passwd", "/etc/passwd")]
        [InlineData("/a/../../b", "/b")]
        [InlineData("/a/b/../..", "/")]
        public void Normalize_CollapsesDotSegmentsWithoutLeavingRoot(string? input, string expected)
        {
            Assert.Equal(expected, FtpPathNormalizer.Normalize(input));
            Assert.Equal(expected, FtpListingParser.NormalizeRemotePath(input ?? string.Empty));
        }

        [Fact]
        public void CombineRemotePath_CollapsesTraversalInChildName()
        {
            Assert.Equal("/DCIM/safe", FtpListingParser.CombineRemotePath("/DCIM/100", "../safe"));
            Assert.Equal("/safe", FtpListingParser.CombineRemotePath("/DCIM", "../../safe"));
            Assert.Equal("/", FtpListingParser.CombineRemotePath("/DCIM", "../.."));
        }
    }
}
