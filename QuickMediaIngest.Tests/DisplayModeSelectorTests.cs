#nullable enable
using QuickMediaIngest.Core.DisplayRefresh;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DisplayModeSelectorTests
    {
        [Fact]
        public void EmptyOrMissing_ReturnsNull()
        {
            Assert.Null(DisplayModeSelector.SelectFastestSameResolution(1920, 1080, null));
            Assert.Null(DisplayModeSelector.SelectFastestSameResolution(1920, 1080, Array.Empty<DisplayModeInfo>()));
        }

        [Fact]
        public void PicksFastestSameResolution()
        {
            var modes = new[]
            {
                new DisplayModeInfo(1920, 1080, 60),
                new DisplayModeInfo(1920, 1080, 144),
                new DisplayModeInfo(2560, 1440, 165)
            };
            DisplayModeInfo? best = DisplayModeSelector.SelectFastestSameResolution(1920, 1080, modes);
            Assert.NotNull(best);
            Assert.Equal(144, best!.Value.RefreshHz);
        }
    }
}
