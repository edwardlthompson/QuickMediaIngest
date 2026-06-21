using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>Live FTP smoke tests — optional; requires LAN FTP server.</summary>
    public class FtpThumbnailIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public FtpThumbnailIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task FtpPreviewDownload_AndThumbnail_DecodesJpg()
        {
            if (!LanFtpSmokeProbe.EnsureReachable(_output))
            {
                return;
            }

            LanFtpEndpoint ep = LanFtpSmokeProbe.FromEnvironment();
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            string tempPath = Path.Combine(Path.GetTempPath(), $"qmi-jpg-{Guid.NewGuid():N}.jpg");
            string remotePath = $"{ep.RemoteFolder.TrimEnd('/')}/Camera/pns_gate_16x9_test.jpg";

            try
            {
                bool downloaded = await downloader.TryDownloadPreviewAsync(
                    ep.Host,
                    ep.Port,
                    ep.User,
                    ep.Pass,
                    remotePath,
                    tempPath,
                    "pns_gate_16x9_test.jpg",
                    20,
                    CancellationToken.None);

                _output.WriteLine($"jpg downloaded={downloaded} bytes={(File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0)}");
                if (!downloaded)
                {
                    return;
                }

                var thumb = thumbnailService.GetThumbnail(tempPath);
                _output.WriteLine($"jpg thumb null={thumb == null}");
                Assert.NotNull(thumb);
            }
            catch (System.Net.WebException ex)
            {
                _output.WriteLine($"FTP server unreachable: {ex.Message}");
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        [Fact]
        public async Task FtpPreviewDownload_AndThumbnail_DecodesHeic()
        {
            if (!LanFtpSmokeProbe.EnsureReachable(_output))
            {
                return;
            }

            LanFtpEndpoint ep = LanFtpSmokeProbe.FromEnvironment();
            var downloader = new FtpFileDownloader(NullLogger<FtpFileDownloader>.Instance);
            var thumbnailService = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
            string tempPath = Path.Combine(Path.GetTempPath(), $"qmi-heic-{Guid.NewGuid():N}.heic");
            string remotePath = $"{ep.RemoteFolder.TrimEnd('/')}/20260608_223005.heic";

            try
            {
                bool downloaded = await downloader.TryDownloadPreviewAsync(
                    ep.Host,
                    ep.Port,
                    ep.User,
                    ep.Pass,
                    remotePath,
                    tempPath,
                    "20260608_223005.heic",
                    20,
                    CancellationToken.None);

                _output.WriteLine($"heic downloaded={downloaded} bytes={(File.Exists(tempPath) ? new FileInfo(tempPath).Length : 0)}");
                if (!downloaded)
                {
                    return;
                }

                var thumb = thumbnailService.GetThumbnail(tempPath);
                _output.WriteLine($"heic thumb null={thumb == null}");
                Assert.NotNull(thumb);
            }
            catch (System.Net.WebException ex)
            {
                _output.WriteLine($"FTP server unreachable: {ex.Message}");
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

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
