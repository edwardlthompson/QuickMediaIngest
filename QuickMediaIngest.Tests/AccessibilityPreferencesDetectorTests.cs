#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class AccessibilityPreferencesDetectorTests
    {
        [Fact]
        public void AccessibilityPreferences_DoNotThrow()
        {
            bool hc = AccessibilityPreferencesDetector.IsHighContrastActive();
            bool rm = AccessibilityPreferencesDetector.IsReducedMotionPreferred();
            Assert.True(hc || !hc);
            Assert.True(rm || !rm);
        }
    }
}
