using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ThumbnailDiskCacheTests
    {
        [Fact]
        public void GetFtpCachePath_IsStableForSameInputs()
        {
            string path1 = ThumbnailDiskCache.GetFtpCachePath("10.0.0.23", 2221, "/DCIM/IMG_001.HEIC", 6_500_000);
            string path2 = ThumbnailDiskCache.GetFtpCachePath("10.0.0.23", 2221, "/DCIM/IMG_001.HEIC", 6_500_000);

            Assert.Equal(path1, path2);
            Assert.EndsWith(".jpg", path1);
        }

        [Fact]
        public void GetFtpCachePath_ChangesWhenFileSizeChanges()
        {
            string path1 = ThumbnailDiskCache.GetFtpCachePath("10.0.0.23", 2221, "/DCIM/IMG_001.HEIC", 6_500_000);
            string path2 = ThumbnailDiskCache.GetFtpCachePath("10.0.0.23", 2221, "/DCIM/IMG_001.HEIC", 6_600_000);

            Assert.NotEqual(path1, path2);
        }
    }
}
