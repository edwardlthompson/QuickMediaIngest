using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>Live DCIM batch smoke — validates all HEIC/JPG representatives on LAN FTP.</summary>
    [Collection("Wpf")]
    public class FtpThumbnailPipelineDcimSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public FtpThumbnailPipelineDcimSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(Skip = "Live FTP batch smoke; run manually against 10.0.0.23 DCIM.")]
        public async Task FtpThumbnailService_LoadsAllDcimHeicFiles_BalancedMode()
        {
            WpfTestHost.EnsureInitialized();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var service = new FtpThumbnailService(thumbnailService, downloader, NullLogger<FtpThumbnailService>.Instance);

            var endpoint = new FtpEndpoint("10.0.0.23", 2221, "android", "android");
            string[] heicFiles =
            {
                "20260608_223005.heic",
                "20260612_213411.heic",
                "20260612_213411_1.heic",
                "20260612_213414.heic",
                "20260612_213415.heic",
                "20260612_213418.heic",
                "20260612_213418_1.heic",
            };

            var items = heicFiles.Select(name => new FtpThumbnailWorkItem
            {
                ItemKey = $"smoke|/DCIM/{name}",
                RemotePath = $"/DCIM/{name}",
                FileName = name,
                FileSize = 6_000_000
            }).ToList();

            var options = new FtpThumbnailLoadOptions
            {
                DownloadParallelism = 3,
                DecodeParallelism = 4,
                PerformanceMode = "Balanced"
            };

            FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                endpoint,
                items,
                hints: null,
                options,
                onProgress: null,
                onItemCompleted: null,
                cts.Token);

            _output.WriteLine($"loaded={result.LoadedCount} skipped={result.SkippedCount} total={items.Count}");
            foreach (var item in result.Items.OrderBy(i => i.ItemKey))
            {
                string name = item.ItemKey.Split('|')[^1];
                if (item.Thumbnail != null)
                {
                    _output.WriteLine(
                        $"{name} status={item.Status} {item.Thumbnail.PixelWidth}x{item.Thumbnail.PixelHeight}");
                }
                else
                {
                    _output.WriteLine($"{name} status={item.Status} thumb=null");
                }
            }

            Assert.True(result.LoadedCount >= heicFiles.Length - 1, $"Expected nearly all HEIC loaded; got {result.LoadedCount}/{heicFiles.Length}");
            Assert.All(result.Items.Where(i => i.Thumbnail != null), i => Assert.True(i.Thumbnail!.PixelWidth >= 32));
        }

        [Fact(Skip = "Long-running live FTP batch; run manually when validating DCIM Ultra.")]
        public async Task FtpThumbnailService_LoadsAllDcimHeicFiles_UltraMode()
        {
            WpfTestHost.EnsureInitialized();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var service = new FtpThumbnailService(thumbnailService, downloader, NullLogger<FtpThumbnailService>.Instance);

            var endpoint = new FtpEndpoint("10.0.0.23", 2221, "android", "android");
            string[] heicFiles =
            {
                "20260608_223005.heic",
                "20260612_213411.heic",
                "20260612_213411_1.heic",
                "20260612_213414.heic",
                "20260612_213415.heic",
                "20260612_213418.heic",
                "20260612_213418_1.heic",
            };

            var items = heicFiles.Select(name => new FtpThumbnailWorkItem
            {
                ItemKey = $"smoke|/DCIM/{name}",
                RemotePath = $"/DCIM/{name}",
                FileName = name,
                FileSize = 6_000_000
            }).ToList();

            var options = new FtpThumbnailLoadOptions
            {
                DownloadParallelism = 6,
                DecodeParallelism = 8,
                PerformanceMode = "Ultra"
            };

            FtpThumbnailBatchResult result = await service.LoadBatchAsync(
                endpoint,
                items,
                hints: null,
                options,
                onProgress: null,
                onItemCompleted: null,
                cts.Token);

            _output.WriteLine($"loaded={result.LoadedCount} skipped={result.SkippedCount} total={items.Count}");
            foreach (var item in result.Items.OrderBy(i => i.ItemKey))
            {
                string name = item.ItemKey.Split('|')[^1];
                if (item.Thumbnail != null)
                {
                    _output.WriteLine(
                        $"{name} status={item.Status} {item.Thumbnail.PixelWidth}x{item.Thumbnail.PixelHeight} fmt={item.Thumbnail.Format}");
                }
                else
                {
                    _output.WriteLine($"{name} status={item.Status} thumb=null");
                }
            }

            Assert.Equal(heicFiles.Length, result.LoadedCount);
            Assert.Equal(0, result.SkippedCount);
            Assert.All(result.Items, i => Assert.NotNull(i.Thumbnail));
            Assert.All(result.Items, i => Assert.True(i.Thumbnail!.PixelWidth >= 32));
            Assert.All(result.Items, i => Assert.True(i.Thumbnail!.PixelHeight >= 32));
        }

    }
}
