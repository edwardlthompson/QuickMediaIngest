#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ColorManagementProfileServiceTests
    {
        [Fact]
        public void GetSystemDefaultIccProfilePath_DoesNotThrow()
        {
            var service = new ColorManagementProfileService();
            string? path = service.GetSystemDefaultIccProfilePath();
            // Should either be null (on non-Windows or headless) or a valid file string
            if (path != null)
            {
                Assert.NotEmpty(path);
            }
        }
    }
}
