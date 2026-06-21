using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestEngineTests
    {
        [Fact]
        public void ResolveFileName_UsesNamingTemplateAndGroupTitle()
        {
            // Arrange
            var provider = new Mock<IFileProvider>();
            var logger = new Mock<ILogger<IngestEngine>>();
            var engine = new IngestEngine(provider.Object, logger.Object);
            var item = new ImportItem
            {
                FileName = "IMG_1234.JPG",
                DateTaken = new DateTime(2024, 3, 20, 14, 30, 0)
            };
            string targetDir = "C:/Test";
            string namingTemplate = "[YYYY]-[MM]-[DD]_[Original]_[ShootName]";
            string groupTitle = "Shoot 1";

            // Act
            string result = engine.ResolveFileName(
                item,
                targetDir,
                namingTemplate,
                groupTitle,
                sequenceNumber: 1,
                DuplicateHandlingMode.Suffix,
                out bool skippedAsDuplicate);

            // Assert
            Assert.False(skippedAsDuplicate);
            Assert.Contains("2024-03-20", result);
            Assert.Contains("IMG_1234", result);
            Assert.Contains("Shoot 1", result);
        }

        [Fact]
        public async Task IngestGroupAsync_DeleteAfterImport_Ftp_DeletesWhenListingSizeMatchesDestination()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "qmi_ingest_test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var provider = new Mock<IFileProvider>();
                provider.Setup(p =>
                        p.CopyAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<IProgress<long>>()))
                    .Returns<string, string, CancellationToken, IProgress<long>>((_, dst, _, progress) =>
                    {
                        File.WriteAllBytes(dst, new byte[321]);
                        progress?.Report(321);
                        return Task.CompletedTask;
                    });
                provider.Setup(p =>
                        p.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

                var logger = new Mock<ILogger<IngestEngine>>();
                var engine = new IngestEngine(provider.Object, logger.Object);

                var item = new ImportItem
                {
                    SourcePath = "/DCIM/100CANON/IMG_0001.JPG",
                    FileName = "IMG_0001.JPG",
                    FileSize = 321,
                    IsFtpSource = true,
                    IsSelected = true,
                    DateTaken = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                };

                var group = new ItemGroup
                {
                    Title = "Shoot",
                    StartDate = item.DateTaken.Date,
                    EndDate = item.DateTaken.Date,
                    Items = new List<ImportItem> { item },
                };

                await engine.IngestGroupAsync(
                    group,
                    tempRoot,
                    "[Original]",
                    CancellationToken.None,
                    new IngestOptions { VerificationMode = ImportVerificationMode.Fast },
                    deleteAfterImport: true);

                provider.Verify(
                    p => p.DeleteAsync("/DCIM/100CANON/IMG_0001.JPG", It.IsAny<CancellationToken>()),
                    Times.Once);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // Ignore test cleanup failures.
                }
            }
        }

        [Fact]
        public async Task IngestGroupAsync_WithByteTracker_ReportsIncrementalCopyProgress()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "qmi_ingest_byte_" + Guid.NewGuid());
            Directory.CreateDirectory(tempRoot);
            try
            {
                var provider = new Mock<IFileProvider>();
                provider.Setup(p =>
                        p.CopyAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<IProgress<long>>()))
                    .Returns<string, string, CancellationToken, IProgress<long>>((_, dst, _, progress) =>
                    {
                        progress?.Report(512);
                        File.WriteAllBytes(dst, new byte[1024]);
                        progress?.Report(1024);
                        return Task.CompletedTask;
                    });

                var tracker = new ImportByteProgressTracker(totalBytes: 1024, totalFiles: 1);
                long maxEffective = 0;
                tracker.ProgressChanged += snap => maxEffective = Math.Max(maxEffective, snap.EffectiveBytes);

                var logger = new Mock<ILogger<IngestEngine>>();
                var engine = new IngestEngine(provider.Object, logger.Object);
                var item = new ImportItem
                {
                    SourcePath = "C:/src/photo.jpg",
                    FileName = "photo.jpg",
                    FileSize = 1024,
                    IsSelected = true,
                    DateTaken = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                };

                var group = new ItemGroup
                {
                    Title = "Shoot",
                    StartDate = item.DateTaken.Date,
                    EndDate = item.DateTaken.Date,
                    Items = new List<ImportItem> { item },
                };

                await engine.IngestGroupAsync(
                    group,
                    tempRoot,
                    "[Original]",
                    CancellationToken.None,
                    new IngestOptions
                    {
                        VerificationMode = ImportVerificationMode.Fast,
                        MaxConcurrentFileCopies = 1,
                        ByteProgressTracker = tracker,
                    });

                Assert.True(maxEffective >= 512);
                ImportByteProgressSnapshot finalSnap = tracker.GetSnapshot();
                Assert.Equal(1024, finalSnap.CompletedBytes);
                Assert.Equal(1, finalSnap.FilesCompleted);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempRoot, true);
                }
                catch
                {
                    // Ignore test cleanup failures.
                }
            }
        }
    }
}
