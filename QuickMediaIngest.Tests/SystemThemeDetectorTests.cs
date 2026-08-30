#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class SystemThemeDetectorTests
    {
        [Fact]
        public void ResolveIsDark_ExplicitModes_ReturnsCorrectValue()
        {
            Assert.True(SystemThemeDetector.ResolveIsDark(AppThemeMode.Dark));
            Assert.False(SystemThemeDetector.ResolveIsDark(AppThemeMode.Light));
        }

        [Fact]
        public void IsWindowsDarkThemePreferred_DoesNotThrow()
        {
            bool isDark = SystemThemeDetector.IsWindowsDarkThemePreferred();
            // Just verifying it doesn't throw and returns boolean
            Assert.True(isDark || !isDark);
        }
    }
}
