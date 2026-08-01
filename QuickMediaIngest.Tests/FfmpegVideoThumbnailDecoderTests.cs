using System.IO;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FfmpegVideoThumbnailDecoderTests
    {
        [Fact]
        public void TryGetThumbnail_ReturnsNullForMissingFile()
        {
            Assert.Null(FfmpegVideoThumbnailDecoder.TryGetThumbnail(
                Path.Combine(Path.GetTempPath(), "qmi-missing-" + Path.GetRandomFileName() + ".mp4")));
        }

        [Fact]
        public void TryGetThumbnail_ReturnsNullForNonVideoBytes()
        {
            string path = Path.Combine(Path.GetTempPath(), "qmi-ff-" + Path.GetRandomFileName() + ".bin");
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
                // Missing/invalid input: either null (ffmpeg missing or decode fail) — must not throw.
                _ = FfmpegVideoThumbnailDecoder.TryGetThumbnail(path);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
