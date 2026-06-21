#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>
    /// Automated smoke for BUILD_PLAN HUMAN verification rows.
    /// Skips when LAN FTP is offline; set QMI_SMOKE_REQUIRE=1 to fail instead of skip.
    /// </summary>
    [Collection("Wpf")]
    public class HumanVerificationSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public HumanVerificationSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task FtpColdLoad_UsesTieredCapsNotFullFileDownload()
        {
            if (!LanFtpSmokeProbe.EnsureReachable(_output))
            {
                return;
            }

            LanFtpEndpoint ep = LanFtpSmokeProbe.FromEnvironment();
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            string heicName = "20260612_213411_1.heic";
            string remotePath = $"{ep.RemoteFolder.TrimEnd('/')}/{heicName}";
            string tempPath = Path.Combine(Path.GetTempPath(), $"qmi-tier-{Guid.NewGuid():N}.heic");

            try
            {
                long cap = FtpPreviewDownloadLimits.HeicBytes;
                bool downloaded = await downloader.TryDownloadCappedAsync(
                    ep.Host,
                    ep.Port,
                    ep.User,
                    ep.Pass,
                    remotePath,
                    tempPath,
                    cap,
                    timeoutSeconds: 30,
                    CancellationToken.None);

                Assert.True(downloaded, "Expected tier-capped HEIC preview download to succeed.");
                long bytes = new FileInfo(tempPath).Length;
                _output.WriteLine($"HEIC capped download bytes={bytes} cap={cap}");
                Assert.True(bytes <= cap, $"Download exceeded tier cap: {bytes} > {cap}");
                Assert.True(bytes < 5_000_000, "Download looks like a full-file fetch (>5 MB), not a tiered preview.");
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        [Fact]
        public async Task FtpReconnect_SecondLoadUsesDiskCache()
        {
            if (!LanFtpSmokeProbe.EnsureReachable(_output))
            {
                return;
            }

            WpfTestHost.EnsureInitialized();
            LanFtpEndpoint ep = LanFtpSmokeProbe.FromEnvironment();
            string jpgName = "pns_gate_16x9_test.jpg";
            string remotePath = $"{ep.RemoteFolder.TrimEnd('/')}/Camera/{jpgName}";

            var workItem = new FtpThumbnailWorkItem
            {
                ItemKey = $"smoke|{remotePath}",
                RemotePath = remotePath,
                FileName = jpgName,
                FileSize = 12_463
            };

            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var service = new FtpThumbnailService(thumbnailService, downloader, NullLogger<FtpThumbnailService>.Instance);
            var endpoint = ep.ToFtpEndpoint();
            var options = new FtpThumbnailLoadOptions
            {
                DownloadParallelism = 2,
                DecodeParallelism = 2,
                PerformanceMode = "Ultra"
            };

            FtpThumbnailBatchResult first = await service.LoadBatchAsync(
                endpoint,
                new[] { workItem },
                hints: null,
                options,
                onProgress: null,
                onItemCompleted: null,
                CancellationToken.None);

            Assert.Equal(1, first.LoadedCount);
            string cachePath = ThumbnailDiskCache.GetFtpCachePath(ep.Host, ep.Port, remotePath, workItem.FileSize);
            _output.WriteLine($"cache path={cachePath} exists={File.Exists(cachePath)}");
            Assert.True(File.Exists(cachePath), "Expected FTP disk cache file after first load.");

            var cached = ThumbnailDiskCache.TryLoadFtp(ep.Host, ep.Port, remotePath, workItem.FileSize);
            Assert.NotNull(cached);
            _output.WriteLine("FTP thumbnail disk cache hit (TryLoadFtp) before second batch.");

            FtpThumbnailBatchResult second = await service.LoadBatchAsync(
                endpoint,
                new[] { workItem },
                hints: null,
                options,
                onProgress: null,
                onItemCompleted: null,
                CancellationToken.None);

            Assert.Equal(1, second.LoadedCount);
            Assert.NotNull(second.Items.Single().Thumbnail);
        }

        [Fact]
        public async Task FtpUltraMode_LoadsRepresentativeJpgAndHeic()
        {
            if (!LanFtpSmokeProbe.EnsureReachable(_output))
            {
                return;
            }

            WpfTestHost.EnsureInitialized();
            LanFtpEndpoint ep = LanFtpSmokeProbe.FromEnvironment();
            string basePath = ep.RemoteFolder.TrimEnd('/');

            var items = new[]
            {
                Work($"{basePath}/Camera/pns_gate_16x9_test.jpg", "pns_gate_16x9_test.jpg", 12_463),
                Work($"{basePath}/20260612_213411_1.heic", "20260612_213411_1.heic", 6_227_701),
            };

            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var service = new FtpThumbnailService(thumbnailService, downloader, NullLogger<FtpThumbnailService>.Instance);

            FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                ep.ToFtpEndpoint(),
                items,
                hints: null,
                new FtpThumbnailLoadOptions
                {
                    DownloadParallelism = 4,
                    DecodeParallelism = 4,
                    PerformanceMode = "Ultra"
                },
                onProgress: null,
                onItemCompleted: null,
                CancellationToken.None);

            _output.WriteLine($"Ultra loaded={result.LoadedCount} skipped={result.SkippedCount}");
            foreach (var item in result.Items)
            {
                _output.WriteLine(
                    $"{item.ItemKey.Split('|')[^1]} status={item.Status} " +
                    $"thumb={(item.Thumbnail != null ? item.Thumbnail.PixelWidth + "x" + item.Thumbnail.PixelHeight : "null")}");
            }

            Assert.Equal(2, result.LoadedCount);
            Assert.All(result.Items, i => Assert.NotNull(i.Thumbnail));
        }

        private static FtpThumbnailWorkItem Work(string remotePath, string fileName, long fileSize) =>
            new()
            {
                ItemKey = $"smoke|{remotePath}",
                RemotePath = remotePath,
                FileName = fileName,
                FileSize = fileSize
            };

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }
    }
}
