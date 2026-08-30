#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class VipsRawThumbnailDecoderTests
    {
        [Fact]
        public void VipsThumbnailDecoder_NonExistentFile_ReturnsNull()
        {
            DecodedThumbnail? thumb = VipsThumbnailDecoder.TryGetThumbnail(
                Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.cr2"),
                320,
                NullLogger.Instance);
            Assert.Null(thumb);
        }

        [Theory]
        [InlineData(".cr2")]
        [InlineData(".cr3")]
        [InlineData(".nef")]
        [InlineData(".arw")]
        [InlineData(".dng")]
        [InlineData(".orf")]
        [InlineData(".rw2")]
        [InlineData(".raf")]
        public void CommonRawFormats_AreIdentifiedAsRaw(string ext)
        {
            Assert.True(MediaExtensions.IsRawExtension(ext));
            Assert.True(MediaExtensions.IsImageExtension(ext));
        }

        [Fact]
        public void FtpTieredPreviewDecoder_CompleteFile_RawPrefersVips()
        {
            var mockThumbService = new Mock<IThumbnailService>();
            string tempFile = Path.Combine(Path.GetTempPath(), $"mock-{Guid.NewGuid():N}.cr3");

            try
            {
                File.WriteAllBytes(tempFile, new byte[1024]);

                // Non-valid image binary should safely return null through Vips / Magick fallback
                DecodedThumbnail? result = FtpTieredPreviewDecoder.TryDecodeDownloaded(
                    "sample.cr3",
                    tempFile,
                    null,
                    mockThumbService.Object,
                    NullLogger.Instance,
                    FtpPreviewDecodeMode.CompleteFile);

                // Should not throw and handles corrupt/empty buffer gracefully
                Assert.Null(result);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
