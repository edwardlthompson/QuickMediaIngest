#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class PtpTetherScannerTests
    {
        [Fact]
        public async Task ScanPtpDeviceAsync_EmptyDevice_ReturnsEmptyList()
        {
            var scanner = new PtpTetherScanner();
            Assert.True(scanner.IsSupportedOnPlatform);
            var result = await scanner.ScanPtpDeviceAsync("mock-ptp-1");
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
