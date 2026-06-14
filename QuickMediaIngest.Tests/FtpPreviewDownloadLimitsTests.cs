using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpPreviewDownloadLimitsTests
    {
        [Theory]
        [InlineData("photo.jpg", FtpPreviewDownloadLimits.ImageBytes)]
        [InlineData("clip.mp4", FtpPreviewDownloadLimits.VideoBytes)]
        [InlineData("frame.CR3", FtpPreviewDownloadLimits.RawBytes)]
        [InlineData("frame.heic", FtpPreviewDownloadLimits.HeicBytes)]
        public void GetMaxPreviewBytes_ReturnsTieredCaps(string fileName, long expected)
        {
            Assert.Equal(expected, FtpPreviewDownloadLimits.GetMaxPreviewBytes(fileName));
        }
    }
}
