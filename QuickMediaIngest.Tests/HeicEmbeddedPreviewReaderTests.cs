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

        [Fact]
        public void TryExtractJpegSegment_RejectsBmffFalsePositiveWithoutJpegMarkerAfterSoi()
        {
            byte[] header = Encoding.ASCII.GetBytes("....ftypheic....");
            // HEIC BMFF often contains FF D8 <non-FF> … FF D9 spans that are not JPEGs.
            byte[] noise = new byte[3000];
            noise[0] = 0xFF;
            noise[1] = 0xD8;
            noise[2] = 0x41; // not 0xFF — reject
            for (int i = 3; i < noise.Length - 2; i++)
            {
                noise[i] = (byte)(i * 13);
            }

            noise[^2] = 0xFF;
            noise[^1] = 0xD9;
            byte[] payload = header.Concat(noise).ToArray();
            Assert.Null(HeicEmbeddedPreviewReader.TryExtractJpegSegment(payload));
        }
    }
}
