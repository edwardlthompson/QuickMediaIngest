#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class AppDistributionChannelTests
    {
        [Fact]
        public void DetectChannel_ReturnsValidEnum()
        {
            var channel = AppDistributionChannel.DetectChannel();
            var display = AppDistributionChannel.GetChannelDisplay();
            Assert.NotEmpty(display);
        }
    }
}
