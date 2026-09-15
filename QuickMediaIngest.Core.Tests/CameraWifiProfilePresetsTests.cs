#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class CameraWifiProfilePresetsTests
    {
        [Theory]
        [InlineData(CameraBrand.Sony, "/DCIM/100MSDCF")]
        [InlineData(CameraBrand.Canon, "/DCIM/100CANON")]
        [InlineData(CameraBrand.Nikon, "/DCIM/100NC_D")]
        [InlineData(CameraBrand.Fujifilm, "/DCIM/100_FUJI")]
        [InlineData(CameraBrand.Panasonic, "/DCIM/100_PANA")]
        public void FindPresetForBrand_ContainsExpectedDefaultPaths(CameraBrand brand, string expectedSubpath)
        {
            var preset = CameraWifiProfilePresets.FindPresetForBrand(brand);
            Assert.NotNull(preset);
            Assert.Contains(expectedSubpath, preset.StandardScanPaths);
        }
    }
}
