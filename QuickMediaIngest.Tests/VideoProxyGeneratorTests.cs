#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.Thumbnails;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class VideoProxyGeneratorTests
    {
        [Fact]
        public async Task ExtractFirstFrameAsync_NonExistentFile_ReturnsNull()
        {
            var result = await VideoProxyGenerator.ExtractFirstFrameAsync(@"Z:\fake.mp4", @"C:\temp\thumb.jpg");
            Assert.Null(result);
        }
    }
}
