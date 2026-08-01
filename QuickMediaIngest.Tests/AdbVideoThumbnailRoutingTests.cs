using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Thumbnails.Wpf;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class AdbVideoThumbnailRoutingTests
    {
        [Fact]
        public async Task DeviceVideoThumb_SkipsAdbPreviewPull()
        {
            WpfTestHost.EnsureInitialized();

            var preview = new Mock<IAdbPreviewFetcher>(MockBehavior.Strict);
            var videoThumb = new Mock<IAdbVideoThumbnailFetcher>(MockBehavior.Strict);
            videoThumb
                .Setup(v => v.TryFetchVideoThumbJpegAsync(
                    It.IsAny<AdbTransferSession>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns<AdbTransferSession, string, string, CancellationToken>((_, _, local, _) =>
                {
                    using var image = new ImageMagick.MagickImage(ImageMagick.MagickColors.Gray, 96, 96);
                    image.Format = ImageMagick.MagickFormat.Jpeg;
                    image.Quality = 85;
                    image.Write(local);
                    return Task.FromResult(true);
                });

            string unique = Guid.NewGuid().ToString("N");
            string remote = $"/Camera/clip-{unique}.mp4";
            const long huge = 400L * 1024 * 1024;

            var session = new AdbTransferSession("serial-test", "/sdcard/DCIM");
            var service = new FtpThumbnailService(
                new ThumbnailService(NullLogger<ThumbnailService>.Instance),
                new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance),
                NullLogger<FtpThumbnailService>.Instance);

            FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                new FtpEndpoint("127.0.0.1", 2121, "u", "p"),
                new[]
                {
                    new FtpThumbnailWorkItem
                    {
                        ItemKey = $"v|{remote}",
                        RemotePath = remote,
                        FileName = Path.GetFileName(remote),
                        FileSize = huge
                    }
                },
                hints: null,
                new FtpThumbnailLoadOptions
                {
                    AdbSession = session,
                    AdbPreviewFetcher = preview.Object,
                    AdbVideoThumbnailFetcher = videoThumb.Object,
                    DownloadParallelism = 1,
                    DecodeParallelism = 1
                },
                onProgress: null,
                onItemCompleted: null,
                CancellationToken.None);

            Assert.True(
                result.LoadedCount == 1,
                $"Expected device JPEG decode; loaded={result.LoadedCount} skipped={result.SkippedCount} " +
                $"status={result.Items.FirstOrDefault()?.Status}");
            Assert.Equal(1, result.AdbDecodedCount);
            preview.Verify(
                p => p.TryFetchCappedAsync(
                    It.IsAny<AdbTransferSession>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            videoThumb.VerifyAll();
        }

        [Fact]
        public async Task LargeVideoWithoutDeviceThumb_DoesNotCallCompletePull()
        {
            WpfTestHost.EnsureInitialized();

            var preview = new Mock<IAdbPreviewFetcher>(MockBehavior.Strict);
            var videoThumb = new Mock<IAdbVideoThumbnailFetcher>();
            videoThumb
                .Setup(v => v.TryFetchVideoThumbJpegAsync(
                    It.IsAny<AdbTransferSession>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            string unique = Guid.NewGuid().ToString("N");
            string remote = $"/Camera/big-{unique}.mp4";
            const long huge = 400L * 1024 * 1024;

            var session = new AdbTransferSession("serial-test", "/sdcard/DCIM");
            var service = new FtpThumbnailService(
                new ThumbnailService(NullLogger<ThumbnailService>.Instance),
                new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance),
                NullLogger<FtpThumbnailService>.Instance);

            FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                new FtpEndpoint("127.0.0.1", 2122, "u", "p"),
                new[]
                {
                    new FtpThumbnailWorkItem
                    {
                        ItemKey = $"v|{remote}",
                        RemotePath = remote,
                        FileName = Path.GetFileName(remote),
                        FileSize = huge
                    }
                },
                hints: null,
                new FtpThumbnailLoadOptions
                {
                    AdbSession = session,
                    AdbPreviewFetcher = preview.Object,
                    AdbVideoThumbnailFetcher = videoThumb.Object,
                    DownloadParallelism = 1,
                    DecodeParallelism = 1
                },
                onProgress: null,
                onItemCompleted: null,
                CancellationToken.None);

            Assert.Equal(0, result.LoadedCount);
            preview.Verify(
                p => p.TryFetchCappedAsync(
                    It.IsAny<AdbTransferSession>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
