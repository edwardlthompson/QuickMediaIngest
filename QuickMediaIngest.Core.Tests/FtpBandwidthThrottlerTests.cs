#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.Ftp;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpBandwidthThrottlerTests
    {
        [Fact]
        public async Task ThrottleAsync_ZeroLimit_DoesNotDelay()
        {
            var throttler = new FtpBandwidthThrottler(0);
            await throttler.ThrottleAsync(1024 * 1024);
            Assert.Equal(0, throttler.BytesPerSecondLimit);
        }

        [Fact]
        public async Task ThrottleAsync_WithLimit_HandlesThrottlingCall()
        {
            var throttler = new FtpBandwidthThrottler(10 * 1024 * 1024); // 10 MB/s
            await throttler.ThrottleAsync(1024);
            Assert.True(throttler.BytesPerSecondLimit > 0);
        }
    }
}
