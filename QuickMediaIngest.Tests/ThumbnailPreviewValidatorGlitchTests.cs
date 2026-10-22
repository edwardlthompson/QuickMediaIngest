#nullable enable
using System;
using System.IO;
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

        [Fact]
        public void IsAcceptable_RejectsMissingEoi()
        {
            byte[] jpeg = CreateSolidJpeg(64, 64, "#6680A0");
            // Strip trailing EOI so the buffer looks truncated.
            byte[] truncated = jpeg[..^2];
            var thumb = new DecodedThumbnail(truncated, 64, 64);
            Assert.False(ThumbnailPreviewValidator.IsAcceptable(thumb));
        }

        [Fact]
        public void MagickDecoder_RejectsIncompleteJpegSource()
        {
            byte[] complete = CreateSolidJpeg(64, 64, "#4477AA");
            byte[] truncated = complete[..Math.Max(32, complete.Length / 3)];
            string path = Path.Combine(Path.GetTempPath(), "qmi-trunc-" + Guid.NewGuid().ToString("N") + ".jpg");
            try
            {
                File.WriteAllBytes(path, truncated);
                Assert.Null(MagickThumbnailDecoder.TryGetThumbnail(path, 120));
            }
            finally
            {
                try { File.Delete(path); } catch { /* temp */ }
            }
        }

        private static byte[] CreateSolidJpeg(int w, int h, string hex)
        {
            using var image = new MagickImage(new MagickColor(hex), (uint)w, (uint)h);
            image.Format = MagickFormat.Jpeg;
            return image.ToByteArray();
        }
    }
}
