#nullable enable
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class TransportDisplayTests
    {
        [Fact]
        public void TransportDisplay_LocalItems_ReturnsLocal()
        {
            var group = new ItemGroup();
            group.Items.Add(new ImportItem { SourcePath = @"C:\DCIM\100CANON\IMG_001.JPG", IsFtpSource = false });
            Assert.Equal("Local", group.TransportDisplay);
        }

        [Fact]
        public void TransportDisplay_FtpItems_ReturnsFtp()
        {
            var group = new ItemGroup();
            group.Items.Add(new ImportItem { SourcePath = "/DCIM/100CANON/IMG_001.JPG", IsFtpSource = true });
            Assert.Equal("FTP", group.TransportDisplay);
        }

        [Fact]
        public void TransportDisplay_AdbItems_ReturnsAdb()
        {
            var group = new ItemGroup();
            group.Items.Add(new ImportItem { SourcePath = "adb:///sdcard/DCIM/IMG_001.JPG", SourceId = "adb:device1" });
            Assert.Equal("ADB", group.TransportDisplay);
        }
    }
}
