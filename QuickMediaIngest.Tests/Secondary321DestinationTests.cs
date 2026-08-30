#nullable enable
using System;
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
    public class Secondary321DestinationTests
    {
        [Fact]
        public async Task ProcessFileItemAsync_CopiesToSecondaryDestinationRoot()
        {
            string primaryDir = Path.Combine(Path.GetTempPath(), $"primary-dest-{Guid.NewGuid():N}");
            string secondaryDir = Path.Combine(Path.GetTempPath(), $"sec-dest-{Guid.NewGuid():N}");
            Directory.CreateDirectory(primaryDir);
            Directory.CreateDirectory(secondaryDir);

            try
            {
                var group = new ItemGroup { Title = "ShootA" };
                var item = new ImportItem
                {
                    SourcePath = @"C:\Fake\test.jpg",
                    FileName = "test.jpg",
                    FileSize = 100,
                    IsSelected = true
                };

                var mockProvider = new Mock<IFileProvider>();
                mockProvider.Setup(p => p.CopyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IProgress<long>>(), It.IsAny<long>()))
                    .Callback<string, string, CancellationToken, IProgress<long>, long>((src, dst, ct, prog, sz) =>
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
                        File.WriteAllText(dst, "sample payload");
                    })
                    .Returns(Task.CompletedTask);

                var options = new IngestOptions
                {
                    SecondaryDestinationRoot = secondaryDir
                };

                await IngestItemProcessor.ProcessOneAsync(
                    item,
                    1,
                    1,
                    group,
                    primaryDir,
                    "[Original]",
                    options,
                    deleteAfterImport: false,
                    mockProvider.Object,
                    NullLogger.Instance,
                    progressChanged: null,
                    itemProcessed: null,
                    CancellationToken.None);

                string primaryFile = Path.Combine(primaryDir, "test.jpg");
                string secFile = Path.Combine(secondaryDir, Path.GetFileName(primaryDir), "test.jpg");

                Assert.True(File.Exists(primaryFile));
                Assert.True(File.Exists(secFile));
            }
            finally
            {
                if (Directory.Exists(primaryDir)) Directory.Delete(primaryDir, true);
                if (Directory.Exists(secondaryDir)) Directory.Delete(secondaryDir, true);
            }
        }
    }
}
