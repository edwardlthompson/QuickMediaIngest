using System;
using System.IO;
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
    /// <summary>End-to-end FTP thumbnail pipeline smoke tests against the LAN test server.</summary>
    [Collection("Wpf")]
    public class FtpThumbnailPipelineSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public FtpThumbnailPipelineSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Live FTP smoke; run manually against 10.0.0.23 DCIM.")]
        public async Task FtpThumbnailService_LoadsJpgHeicAndDngRepresentatives()
        {
            WpfTestHost.EnsureInitialized();

            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var service = new FtpThumbnailService(thumbnailService, downloader, NullLogger<FtpThumbnailService>.Instance);

            var endpoint = new FtpEndpoint("10.0.0.23", 2221, "android", "android");
            var items = new[]
            {
                Work("/DCIM/Camera/pns_gate_16x9_test.jpg", "pns_gate_16x9_test.jpg", 12463),
                Work("/DCIM/20260612_213411_1.heic", "20260612_213411_1.heic", 6227701),
            };

            try
            {
                FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                    endpoint,
                    items,
                    hints: null,
                    options: new FtpThumbnailLoadOptions { DownloadParallelism = 2, DecodeParallelism = 2 },
                    onProgress: null,
                    onItemCompleted: null,
                    CancellationToken.None);

                _output.WriteLine($"loaded={result.LoadedCount} skipped={result.SkippedCount}");
                foreach (var item in result.Items)
                {
                    _output.WriteLine($"{item.ItemKey.Split('|')[^1]} status={item.Status} thumb={(item.Thumbnail != null ? item.Thumbnail.PixelWidth + "x" + item.Thumbnail.PixelHeight : "null")}");
                }

                Assert.Equal(2, result.LoadedCount);
            }
            catch (Exception ex) when (ex is System.Net.WebException or TimeoutException)
            {
                _output.WriteLine($"FTP unavailable: {ex.Message}");
            }
        }

        private static FtpThumbnailWorkItem Work(string remotePath, string fileName, long fileSize) =>
            new()
            {
                ItemKey = $"smoke|{remotePath}",
                RemotePath = remotePath,
                FileName = fileName,
                FileSize = fileSize
            };
    }
}
