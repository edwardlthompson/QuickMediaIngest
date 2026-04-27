using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class UnifiedConcreteSourceScanServiceTests
    {
        [Fact]
        public async Task MergeAllAsync_CombinesLocalAndFtpItems()
        {
            var tmp = Directory.CreateTempSubdirectory("qmi_unified_merge_" + Guid.NewGuid().ToString("N"));
            try
            {
            var localScanner = new Mock<ILocalScanner>();
            localScanner
                .Setup(s => s.Scan(tmp.FullName, false, null))
                .Returns(new List<ImportItem>
                {
                    new() { FileName = "local.jpg", SourcePath = Path.Combine(tmp.FullName, "local.jpg") }
                });

            var ftpScanner = new Mock<IFtpScanner>();
            ftpScanner
                .Setup(s => s.ScanAsync(
                    "ftp.test",
                    21,
                    "anonymous",
                    "anonymous",
                    "/DCIM",
                    false,
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Action<FtpScanProgress>>()))
                .ReturnsAsync(new List<ImportItem>
                {
                    new() { FileName = "remote.jpg", SourcePath = "/DCIM/remote.jpg" }
                });

            var sut = new UnifiedConcreteSourceScanService(
                localScanner.Object,
                ftpScanner.Object,
                Mock.Of<ILogger<UnifiedConcreteSourceScanService>>());

            var ftpSource = new QuickMediaIngest.FtpSourceItem
            {
                Host = "ftp.test",
                Port = 21,
                RemoteFolder = "/DCIM"
            };

            var cache = new Dictionary<string, List<ImportItem>>();
            object[] concreteSources = { tmp.FullName, ftpSource };

            UnifiedScanMergeResult merge = await sut.MergeAllAsync(
                    concreteSources,
                    forceRefresh: true,
                    scanSubfolders: false,
                    cache,
                    mergeProgress: null,
                    CancellationToken.None)
                .ConfigureAwait(false);

            Assert.Equal(2, merge.UnifiedItems.Count);
            Assert.Contains(merge.UnifiedItems, i => i.FileName == "local.jpg");
            Assert.Contains(merge.UnifiedItems, i => i.FileName == "remote.jpg");
            Assert.Empty(merge.FtpListingFailures);
            }
            finally
            {
                try
                {
                    Directory.Delete(tmp.FullName, recursive: true);
                }
                catch
                {
                    // Best-effort cleanup on test agents.
                }
            }
        }
    }
}
