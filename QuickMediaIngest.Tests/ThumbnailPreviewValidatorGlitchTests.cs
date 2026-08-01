#nullable enable
using ImageMagick;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ThumbnailPreviewValidatorGlitchTests
    {
        [Fact]
        public void IsAcceptable_AcceptsNormalPhotoJpeg()
        {
            byte[] jpeg = CreateSolidJpeg(80, 80, "#6680A0");
            var thumb = new DecodedThumbnail(jpeg, 80, 80);
            Assert.True(ThumbnailPreviewValidator.IsAcceptable(thumb));
        }

        [Fact]
        public void IsAcceptable_RejectsGreenFlood()
        {
            byte[] jpeg = CreateSolidJpeg(80, 80, "#00F200");
            var thumb = new DecodedThumbnail(jpeg, 80, 80);
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(thumb));
        }

        [Fact]
        public void IsAcceptable_RejectsMagentaFlood()
        {
            byte[] jpeg = CreateSolidJpeg(80, 80, "#E01AE0");
            var thumb = new DecodedThumbnail(jpeg, 80, 80);
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(thumb));
        }

        [Fact]
        public void IsAcceptable_RejectsCorruptJpegSoiNoise()
        {
            // FF D8 + random payload + FF D9 — Magick cannot decode; must not pass as a preview.
            byte[] junk = new byte[4096];
            junk[0] = 0xFF;
            junk[1] = 0xD8;
            junk[2] = 0x89;
            for (int i = 3; i < junk.Length - 2; i++)
            {
                junk[i] = (byte)(i * 37);
            }

            junk[^2] = 0xFF;
            junk[^1] = 0xD9;
            var thumb = new DecodedThumbnail(junk, 64, 64);
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(thumb));
        }

        private static byte[] CreateSolidJpeg(int w, int h, string hex)
        {
            using var image = new MagickImage(new MagickColor(hex), (uint)w, (uint)h);
            image.Format = MagickFormat.Jpeg;
            return image.ToByteArray();
        }
    }
}
