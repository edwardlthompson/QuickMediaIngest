#nullable enable
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ThumbnailDiskCacheCapTests
    {
        [Fact]
        public void GetCurrentCacheSizeBytes_DoesNotThrow()
        {
            long size = ThumbnailDiskCache.GetCurrentCacheSizeBytes();
            Assert.True(size >= 0);
        }

        [Fact]
        public void PurgeCache_WithCap_ExecutesCleanly()
        {
            ThumbnailDiskCache.PurgeCache(1024 * 1024 * 500);
            Assert.True(true);
        }
    }
}
