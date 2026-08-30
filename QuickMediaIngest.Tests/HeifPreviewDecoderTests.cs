#nullable enable
using QuickMediaIngest.Core.Thumbnails;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class HeifPreviewDecoderTests
    {
        [Fact]
        public void TryDecode_NonExistentFile_ReturnsNull()
        {
            var result = HeifPreviewDecoder.TryDecode(@"Z:\does_not_exist\sample.heic");
            Assert.Null(result);
        }
    }
}
