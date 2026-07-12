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
        public void IsAcceptable_RejectsEmptyJpegBytes()
        {
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(new DecodedThumbnail([], 64, 64)));
        }

        [Fact]
        public void IsAcceptable_RejectsSmallDimensions()
        {
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(new DecodedThumbnail([1], 16, 16)));
        }

        [Fact]
        public void IsAcceptable_AcceptsValidPayload()
        {
            Assert.True(ThumbnailPreviewValidator.IsAcceptable(new DecodedThumbnail([1], 64, 48)));
        }

        [Fact]
        public void MinPixelEdge_IsAtLeast32()
        {
            Assert.True(ThumbnailPreviewValidator.MinPixelEdge >= 32);
        }
    }
}
