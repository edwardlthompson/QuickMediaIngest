#nullable enable
using System;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class PortableDeviceScannerTests
    {
        [Theory]
        [InlineData("Apple Inc.", "Apple iPhone 15 Pro", PortableDeviceType.AppleIPhone)]
        [InlineData("Apple", "iPad Pro", PortableDeviceType.AppleIPhone)]
        [InlineData("Samsung", "Galaxy S24", PortableDeviceType.AndroidWpd)]
        [InlineData("Google", "Pixel 8 Pro", PortableDeviceType.AndroidWpd)]
        [InlineData("Sony", "Walkman", PortableDeviceType.GenericMtp)]
        [InlineData("", "", PortableDeviceType.GenericMtp)]
        public void DetectDeviceType_ClassifiesCorrectly(string manufacturer, string friendlyName, PortableDeviceType expected)
        {
            var result = PortableDeviceScanner.DetectDeviceType(manufacturer, friendlyName);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void EnumerateConnectedDevices_DoesNotThrow()
        {
            var scanner = new PortableDeviceScanner();
            var devices = scanner.EnumerateConnectedDevices();
            Assert.NotNull(devices);
        }
    }
}
