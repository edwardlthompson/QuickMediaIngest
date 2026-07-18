#nullable enable
using System.IO;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class RemovableDriveIoTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void IsOnRemovableDrive_NullOrBlank_ReturnsFalse(string? path)
        {
            Assert.False(RemovableDriveIo.IsOnRemovableDrive(path));
        }

        [Fact]
        public void CapPreviewWorkers_NonRemovable_KeepsRequested()
        {
            string temp = Path.GetTempPath();
            Assert.Equal(8, RemovableDriveIo.CapPreviewWorkers(8, temp));
        }

        [Fact]
        public void CapConcurrentCopies_NonRemovable_KeepsRequestedIncludingZero()
        {
            string temp = Path.GetTempPath();
            Assert.Equal(0, RemovableDriveIo.CapConcurrentCopies(0, temp));
            Assert.Equal(4, RemovableDriveIo.CapConcurrentCopies(4, temp));
        }

        [Fact]
        public void CapPreviewWorkers_UnknownPath_DoesNotThrow()
        {
            int capped = RemovableDriveIo.CapPreviewWorkers(16, @"Z:\does-not-exist\photo.jpg");
            Assert.True(capped >= 1);
            Assert.True(capped <= 16);
        }
    }
}
