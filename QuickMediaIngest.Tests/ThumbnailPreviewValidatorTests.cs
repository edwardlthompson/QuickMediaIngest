using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ThumbnailPreviewValidatorTests
    {
        [Fact]
        public void IsAcceptable_RejectsNull()
        {
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(null));
        }

        [Fact]
        public void MinPixelEdge_IsAtLeast32()
        {
            Assert.True(ThumbnailPreviewValidator.MinPixelEdge >= 32);
        }
    }
}
