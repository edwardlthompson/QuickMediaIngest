#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MetadataKeywordWriterTests
    {
        [Fact]
        public void TryApplyKeywords_NullOrEmptyKeywords_DoesNotCreateSidecar()
        {
            string dir = CreateTempDir();
            try
            {
                string path = Path.Combine(dir, "photo.dng");
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
                MetadataKeywordWriter.TryApplyKeywords(path, null);
                MetadataKeywordWriter.TryApplyKeywords(path, Array.Empty<string>());
                Assert.False(File.Exists(path + ".xmp"));
            }
            finally
            {
                TryDelete(dir);
            }
        }

        [Fact]
        public void TryApplyKeywords_RawExtension_CreatesXmpSidecar()
        {
            string dir = CreateTempDir();
            try
            {
                string path = Path.Combine(dir, "IMG_0001.dng");
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
                MetadataKeywordWriter.TryApplyKeywords(path, new[] { "travel", "Travel", "  beach  " });

                string xmp = Path.ChangeExtension(path, ".xmp");
                Assert.True(File.Exists(xmp));
                string text = File.ReadAllText(xmp);
                Assert.Contains("travel", text, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("beach", text, StringComparison.OrdinalIgnoreCase);
                // Case-insensitive dedupe — "Travel" should not appear twice as separate entries.
                Assert.Equal(1, CountOccurrencesIgnoreCase(text, "travel"));
            }
            finally
            {
                TryDelete(dir);
            }
        }

        [Fact]
        public void TryApplyKeywords_MissingFile_NoThrow()
        {
            MetadataKeywordWriter.TryApplyKeywords(
                Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N") + ".jpg"),
                new[] { "tag" });
        }

        private static int CountOccurrencesIgnoreCase(string haystack, string needle)
        {
            int count = 0;
            int index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }

        private static string CreateTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi-kw-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void TryDelete(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    public class IngestItemProcessorTests
    {
        [Fact]
        public async Task ProcessOneAsync_SkippedDuplicate_DoesNotCallCopy()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi-proc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string existing = Path.Combine(dir, "IMG_1.JPG");
                File.WriteAllBytes(existing, new byte[] { 9, 9, 9 });

                var provider = new Mock<IFileProvider>(MockBehavior.Strict);
                var logger = new Mock<ILogger>();
                var item = new ImportItem
                {
                    FileName = "IMG_1.JPG",
                    SourcePath = @"E:\DCIM\IMG_1.JPG",
                    FileSize = 3,
                    DateTaken = DateTime.Now
                };
                var group = new ItemGroup { Title = "Shoot" };
                var options = new IngestOptions
                {
                    DuplicateHandling = DuplicateHandlingMode.Skip
                };

                await IngestItemProcessor.ProcessOneAsync(
                    item,
                    itemIndex: 1,
                    total: 1,
                    group,
                    dir,
                    namingTemplate: "[Original]",
                    options,
                    deleteAfterImport: false,
                    provider.Object,
                    logger.Object,
                    progressChanged: null,
                    itemProcessed: null,
                    CancellationToken.None);

                provider.Verify(p => p.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<long>?>()), Times.Never);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        [Fact]
        public async Task ProcessOneAsync_DeleteAfterImport_SkipsDeleteWhenVerificationFails()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi-proc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var provider = new Mock<IFileProvider>();
                provider
                    .Setup(p => p.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<long>?>()))
                    .Returns<string, string, CancellationToken, IProgress<long>?>((_, dest, _, __) =>
                    {
                        // Write a tiny dest file so File.Exists passes, but size won't match Fast verification.
                        File.WriteAllBytes(dest, new byte[] { 1 });
                        return Task.CompletedTask;
                    });

                var logger = new Mock<ILogger>();
                var item = new ImportItem
                {
                    FileName = "IMG_2.JPG",
                    SourcePath = @"E:\DCIM\IMG_2.JPG",
                    FileSize = 9999,
                    DateTaken = DateTime.Now
                };
                var group = new ItemGroup { Title = "Shoot" };
                var options = new IngestOptions
                {
                    DuplicateHandling = DuplicateHandlingMode.Suffix,
                    VerificationMode = ImportVerificationMode.Fast
                };

                await IngestItemProcessor.ProcessOneAsync(
                    item,
                    1,
                    1,
                    group,
                    dir,
                    "[Original]",
                    options,
                    deleteAfterImport: true,
                    provider.Object,
                    logger.Object,
                    null,
                    null,
                    CancellationToken.None);

                provider.Verify(p => p.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }
    }

    public class UpdateServiceTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_responder(request));
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenRemoteVersionNewer_ReturnsDownloadUrl()
        {
            string json = """
                {
                  "tag_name": "v99.0.0",
                  "html_url": "https://example.com/releases/v99.0.0",
                  "assets": [
                    {
                      "name": "QuickMediaIngest.exe",
                      "browser_download_url": "https://example.com/QuickMediaIngest.exe"
                    }
                  ]
                }
                """;

            using var http = new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));
            var logger = new Mock<ILogger<UpdateService>>();
            var svc = new UpdateService(http, logger.Object);

            UpdateCheckResult result = await svc.CheckForUpdateAsync(intervalHours: 24, force: true, packageType: "Portable");

            Assert.Equal("https://example.com/QuickMediaIngest.exe", result.DownloadUrl);
            Assert.Equal("v99.0.0", result.RemoteVersionTag);
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenHttpFails_ReturnsDefaultWithoutThrowing()
        {
            using var http = new HttpClient(new StubHandler(_ =>
                throw new HttpRequestException("network down")));
            var logger = new Mock<ILogger<UpdateService>>();
            var svc = new UpdateService(http, logger.Object);

            UpdateCheckResult result = await svc.CheckForUpdateAsync(force: true);
            Assert.Null(result.DownloadUrl);
            Assert.Null(result.RemoteVersionTag);
        }
    }
}
