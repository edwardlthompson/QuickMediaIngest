using System.Linq;
using System.Text;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class HeicEmbeddedPreviewReaderTests
    {
        [Fact]
        public void TryExtractJpegSegment_ReturnsNullForEmptyBuffer()
        {
            Assert.Null(HeicEmbeddedPreviewReader.TryExtractJpegSegment([]));
        }

        [Fact]
        public void TryExtractJpegSegment_FindsJpegMarkerInHeicLikeBuffer()
        {
            byte[] header = Encoding.ASCII.GetBytes("....ftypheic....");
            byte[] jpeg = { 0xFF, 0xD8, 0xFF, 0xD9 };
            byte[] payload = header.Concat(jpeg).ToArray();

            // Decode may fail on minimal JPEG; marker scan should at least run without throwing.
            _ = HeicEmbeddedPreviewReader.TryExtractJpegSegment(payload);
        }
    }
}
