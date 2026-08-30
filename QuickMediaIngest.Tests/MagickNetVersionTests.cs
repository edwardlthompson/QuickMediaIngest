#nullable enable
using System;
using ImageMagick;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MagickNetVersionTests
    {
        [Fact]
        public void MagickNet_IsAtLeast_14_16_0()
        {
            Version? ver = typeof(MagickImage).Assembly.GetName().Version;
            Assert.NotNull(ver);
            Assert.True(ver >= new Version(14, 16, 0), $"Magick.NET {ver} is below 14.16.0 (GHSA floor).");
        }
    }
}
