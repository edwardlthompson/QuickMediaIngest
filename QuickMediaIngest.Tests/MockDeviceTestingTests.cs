#nullable enable
using System.IO;
using QuickMediaIngest.Core.Testing;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MockDeviceTestingTests
    {
        [Fact]
        public void MockRemovableVolume_CreatesAndCleansUpDcimFiles()
        {
            string rootPath;
            using (var volume = new MockRemovableVolume("SONY_SD"))
            {
                rootPath = volume.VolumeRoot;
                Assert.True(Directory.Exists(rootPath));
                string dcim = Path.Combine(rootPath, "DCIM", "100CANON");
                Assert.True(Directory.Exists(dcim));
                Assert.True(File.Exists(Path.Combine(dcim, "IMG_0001.JPG")));
            }

            Assert.False(Directory.Exists(rootPath));
        }

        [Fact]
        public void MockFtpListing_GeneratesExpectedItems()
        {
            var mock = MockFtpListing.CreateSampleSonyListing();
            Assert.Equal("192.168.1.50", mock.Host);
            Assert.Equal(2, mock.MockItems.Count);
            Assert.Contains(mock.MockItems, i => i.FileType == "ARW");
        }
    }
}
