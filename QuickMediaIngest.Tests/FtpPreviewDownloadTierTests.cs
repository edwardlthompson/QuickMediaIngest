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
        public void GetPreviewByteTiers_Heic_CapsAtTwelveMegabytes()
        {
            var tiers = FtpPreviewDownloadLimits.GetPreviewByteTiers("photo.heic");

            Assert.Equal(FtpPreviewDownloadLimits.HeicBytes, tiers[^1]);
            Assert.Equal(12 * 1024 * 1024, tiers[^1]);
        }

        [Fact]
        public void GetFetchTiers_KnownHeicSize_SingleShot()
        {
            long size = 5 * 1024 * 1024;
            var tiers = FtpPreviewDownloadLimits.GetFetchTiers("photo.heic", size);
            Assert.Single(tiers);
            Assert.Equal(size, tiers[0]);
        }

        [Fact]
        public void GetFetchTiers_UnknownHeic_SkipsTinyTiers()
        {
            var tiers = FtpPreviewDownloadLimits.GetFetchTiers("photo.heic", knownFileSize: 0);
            Assert.Single(tiers);
            Assert.Equal(FtpPreviewDownloadLimits.HeicBytes, tiers[0]);
        }

        [Fact]
        public void GetFetchTiers_LargeVideo_SkipsTruncatedTiers()
        {
            long size = 400L * 1024 * 1024;
            var tiers = FtpPreviewDownloadLimits.GetFetchTiers("clip.mp4", size);
            Assert.Empty(tiers);
        }

        [Fact]
        public void ShouldTryVideoCompleteFallback_AllowsUpTo256MB()
        {
            Assert.True(FtpPreviewDownloadLimits.ShouldTryVideoCompleteFallback(174L * 1024 * 1024));
            Assert.True(FtpPreviewDownloadLimits.ShouldTryVideoCompleteFallback(
                FtpPreviewDownloadLimits.VideoCompleteFallbackBytes));
            Assert.False(FtpPreviewDownloadLimits.ShouldTryVideoCompleteFallback(400L * 1024 * 1024));
            Assert.False(FtpPreviewDownloadLimits.ShouldTryVideoCompleteFallback(0));
        }
    }
}
