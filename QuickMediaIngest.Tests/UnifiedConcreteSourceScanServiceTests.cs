using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class UnifiedConcreteSourceScanServiceTests : IDisposable
    {
        public UnifiedConcreteSourceScanServiceTests()
        {
            FtpSourceCooldown.ClearAll();
        }

        public void Dispose() => FtpSourceCooldown.ClearAll();

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

                var ftpScanner = CreateLiveFtpScanner();
                var sut = CreateSut(localScanner.Object, ftpScanner.Object);

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
                    CancellationToken.None);

                Assert.Equal(2, merge.UnifiedItems.Count);
                Assert.Contains(merge.UnifiedItems, i => i.FileName == "local.jpg");
                Assert.Contains(merge.UnifiedItems, i => i.FileName == "remote.jpg");
                Assert.Empty(merge.FtpListingFailures);
            }
            finally
            {
                try { Directory.Delete(tmp.FullName, recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task MergeAllAsync_FtpThrow_StillReturnsLocal()
        {
            var tmp = Directory.CreateTempSubdirectory("qmi_unified_soft_" + Guid.NewGuid().ToString("N"));
            try
            {
                var localScanner = new Mock<ILocalScanner>();
                localScanner
                    .Setup(s => s.Scan(tmp.FullName, false, null))
                    .Returns(new List<ImportItem>
                    {
                        new() { FileName = "card.cr2", SourcePath = Path.Combine(tmp.FullName, "card.cr2") }
                    });

                var ftpScanner = new Mock<IFtpScanner>();
                ftpScanner
                    .Setup(s => s.TestConnectionAsync(
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new TimeoutException("dead host"));

                var sut = CreateSut(localScanner.Object, ftpScanner.Object);
                var ftpSource = new QuickMediaIngest.FtpSourceItem
                {
                    Host = "10.0.0.7",
                    Port = 2221,
                    RemoteFolder = "/DCIM"
                };

                var completed = new List<UnifiedScanSourceCompleted>();
                var progress = new SyncProgress<UnifiedScanSourceCompleted>(completed.Add);

                UnifiedScanMergeResult merge = await sut.MergeAllAsync(
                    new object[] { tmp.FullName, ftpSource },
                    forceRefresh: true,
                    scanSubfolders: false,
                    new Dictionary<string, List<ImportItem>>(),
                    mergeProgress: null,
                    CancellationToken.None,
                    preferAdbTransfer: false,
                    sourceCompleted: progress);

                Assert.Single(merge.UnifiedItems);
                Assert.Equal("card.cr2", merge.UnifiedItems[0].FileName);
                Assert.NotEmpty(merge.FtpListingFailures);
                Assert.Contains(completed, c => !c.IsFtp && c.Items.Count == 1);
                Assert.True(FtpSourceCooldown.IsCoolingDown("10.0.0.7", 2221, out _));
            }
            finally
            {
                try { Directory.Delete(tmp.FullName, recursive: true); } catch { }
            }
        }

        [Fact]
        public async Task MergeAllAsync_UsesConnectProbeBudgetAndListingBudget()
        {
            var ftpScanner = new Mock<IFtpScanner>();
            ftpScanner
                .Setup(s => s.TestConnectionAsync(
                    "ftp.test", 21, "anonymous", "anonymous", "/DCIM",
                    UnifiedFtpScanBudgets.ConnectProbeSeconds,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "ok"));
            ftpScanner
                .Setup(s => s.ScanAsync(
                    "ftp.test", 21, "anonymous", "anonymous", "/DCIM", false,
                    UnifiedFtpScanBudgets.ListingSeconds,
                    It.IsAny<CancellationToken>(),
                    It.IsAny<Action<FtpScanProgress>>()))
                .ReturnsAsync(new List<ImportItem>
                {
                    new() { FileName = "remote.jpg", SourcePath = "/DCIM/remote.jpg" }
                });

            var sut = CreateSut(Mock.Of<ILocalScanner>(), ftpScanner.Object);
            var ftpSource = new QuickMediaIngest.FtpSourceItem
            {
                Host = "ftp.test",
                Port = 21,
                RemoteFolder = "/DCIM"
            };

            UnifiedScanMergeResult merge = await sut.MergeAllAsync(
                new object[] { ftpSource },
                forceRefresh: true,
                scanSubfolders: false,
                new Dictionary<string, List<ImportItem>>());

            Assert.Single(merge.UnifiedItems);
            ftpScanner.VerifyAll();
        }

        [Fact]
        public async Task MergeAllAsync_CooldownSkipsLiveFtp()
        {
            FtpSourceCooldown.MarkFailed("cool.host", 21, TimeSpan.FromMinutes(5));

            var ftpScanner = new Mock<IFtpScanner>(MockBehavior.Strict);
            var sut = CreateSut(Mock.Of<ILocalScanner>(), ftpScanner.Object);
            var ftpSource = new QuickMediaIngest.FtpSourceItem
            {
                Host = "cool.host",
                Port = 21,
                RemoteFolder = "/DCIM"
            };

            UnifiedScanMergeResult merge = await sut.MergeAllAsync(
                new object[] { ftpSource },
                forceRefresh: false,
                scanSubfolders: false,
                new Dictionary<string, List<ImportItem>>());

            Assert.Empty(merge.UnifiedItems);
            Assert.NotEmpty(merge.FtpListingFailures);
            ftpScanner.Verify(
                s => s.TestConnectionAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private static Mock<IFtpScanner> CreateLiveFtpScanner()
        {
            var ftpScanner = new Mock<IFtpScanner>();
            ftpScanner
                .Setup(s => s.TestConnectionAsync(
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((true, "ok"));
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
            return ftpScanner;
        }

        private static UnifiedConcreteSourceScanService CreateSut(ILocalScanner local, IFtpScanner ftp) =>
            new(
                local,
                ftp,
                Mock.Of<IAdbMediaScanner>(),
                Mock.Of<IAdbPathProbe>(),
                Mock.Of<ILogger<UnifiedConcreteSourceScanService>>());

        private sealed class SyncProgress<T> : IProgress<T>
        {
            private readonly Action<T> _handler;
            public SyncProgress(Action<T> handler) => _handler = handler;
            public void Report(T value) => _handler(value);
        }
    }
}
