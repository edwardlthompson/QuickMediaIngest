#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestDryRunTests
    {
        [Fact]
        public async Task ProcessFileItemAsync_WhenDryRun_DoesNotCopyOrDelete()
        {
            var mockProvider = new Mock<IFileProvider>();
            var group = new ItemGroup { Title = "ShootA" };
            var item = new ImportItem
            {
                SourcePath = @"C:\FakeSource\DSC_0001.JPG",
                FileName = "DSC_0001.JPG",
                FileSize = 1024,
                IsSelected = true
            };

            var options = new IngestOptions
            {
                IsDryRun = true
            };

            string targetDir = Path.Combine(Path.GetTempPath(), "DryRunTest");
            bool itemProcessedCalled = false;

            await IngestItemProcessor.ProcessOneAsync(
                item,
                0,
                1,
                group,
                targetDir,
                "[Date]_[Original]",
                options,
                deleteAfterImport: true,
                mockProvider.Object,
                NullLogger.Instance,
                progressChanged: null,
                itemProcessed: info =>
                {
                    if (!info.IsStarted)
                    {
                        itemProcessedCalled = true;
                        Assert.True(info.Success);
                        Assert.Equal("Dry run (simulated)", info.ErrorMessage);
                    }
                },
                CancellationToken.None);

            Assert.True(itemProcessedCalled);
            // Verify CopyAsync and DeleteAsync were NEVER called
            mockProvider.Verify(p => p.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<System.IProgress<long>>(), It.IsAny<long>()), Times.Never);
            mockProvider.Verify(p => p.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
