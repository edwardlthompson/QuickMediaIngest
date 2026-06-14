using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpPreviewDownloadTierTests
    {
        [Fact]
        public void GetPreviewByteTiers_Jpg_IncludesBaseTiersUpToCap()
        {
            var tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers("photo.jpg");

            Assert.Equal(3, tiers.Count);
            Assert.Equal(FtpPreviewDownloadLimits.Tier64K, tiers[0]);
            Assert.Equal(FtpPreviewDownloadLimits.ImageBytes, tiers[^1]);
        }

        [Fact]
        public void GetPreviewByteTiers_Heic_CapsAtTwoMegabytes()
        {
            var tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers("photo.heic");

            Assert.Equal(FtpPreviewDownloadLimits.HeicBytes, tiers[^1]);
            Assert.Equal(2 * 1024 * 1024, tiers[^1]);
            Assert.True(tiers[^1] < 8 * 1024 * 1024);
        }
    }
}
