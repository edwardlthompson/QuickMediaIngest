using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestFileNamingTests
    {
        [Fact]
        public void ResolveFileName_SkipPolicy_SkipsWhenDestinationExists()
        {
            string targetDir = Path.Combine(Path.GetTempPath(), "qmi_naming_" + Guid.NewGuid());
            Directory.CreateDirectory(targetDir);
            try
            {
                string existing = Path.Combine(targetDir, "photo.jpg");
                File.WriteAllText(existing, "existing");

                var item = new ImportItem { FileName = "photo.jpg", DateTaken = DateTime.UtcNow };
                string result = IngestFileNaming.ResolveFileName(
                    item,
                    targetDir,
                    "[Original]",
                    "Shoot",
                    1,
                    DuplicateHandlingMode.Skip,
                    out bool skipped);

                Assert.True(skipped);
                Assert.Equal(string.Empty, result);
            }
            finally
            {
                Directory.Delete(targetDir, true);
            }
        }

        [Fact]
        public void ResolveFileName_SuffixPolicy_AppendsCounterWhenDestinationExists()
        {
            string targetDir = Path.Combine(Path.GetTempPath(), "qmi_naming_" + Guid.NewGuid());
            Directory.CreateDirectory(targetDir);
            try
            {
                File.WriteAllText(Path.Combine(targetDir, "photo.jpg"), "existing");

                var item = new ImportItem { FileName = "photo.jpg", DateTaken = DateTime.UtcNow };
                string result = IngestFileNaming.ResolveFileName(
                    item,
                    targetDir,
                    "[Original]",
                    "Shoot",
                    1,
                    DuplicateHandlingMode.Suffix,
                    out bool skipped);

                Assert.False(skipped);
                Assert.Equal("photo_01.jpg", result);
            }
            finally
            {
                Directory.Delete(targetDir, true);
            }
        }

        [Fact]
        public void ResolveFileName_OverwriteIfNewer_SkipsWhenLocalSourceIsOlder()
        {
            string targetDir = Path.Combine(Path.GetTempPath(), "qmi_naming_" + Guid.NewGuid());
            Directory.CreateDirectory(targetDir);
            try
            {
                string sourcePath = Path.Combine(targetDir, "source.jpg");
                string destPath = Path.Combine(targetDir, "photo.jpg");
                File.WriteAllText(sourcePath, "source");
                File.WriteAllText(destPath, "dest");
                File.SetLastWriteTimeUtc(destPath, DateTime.UtcNow);

                var item = new ImportItem
                {
                    FileName = "photo.jpg",
                    SourcePath = sourcePath,
                    DateTaken = DateTime.UtcNow.AddHours(-2),
                };
                string result = IngestFileNaming.ResolveFileName(
                    item,
                    targetDir,
                    "[Original]",
                    "Shoot",
                    1,
                    DuplicateHandlingMode.OverwriteIfNewer,
                    out bool skipped);

                Assert.True(skipped);
                Assert.Equal(string.Empty, result);
            }
            finally
            {
                Directory.Delete(targetDir, true);
            }
        }

        [Fact]
        public void ResolveFileName_OverwriteIfNewer_AllowsOverwriteWhenLocalSourceIsNewer()
        {
            string targetDir = Path.Combine(Path.GetTempPath(), "qmi_naming_" + Guid.NewGuid());
            Directory.CreateDirectory(targetDir);
            try
            {
                string sourcePath = Path.Combine(targetDir, "source.jpg");
                string destPath = Path.Combine(targetDir, "photo.jpg");
                File.WriteAllText(sourcePath, "source");
                File.WriteAllText(destPath, "dest");
                File.SetLastWriteTimeUtc(sourcePath, DateTime.UtcNow);
                File.SetLastWriteTimeUtc(destPath, DateTime.UtcNow.AddHours(-2));

                var item = new ImportItem
                {
                    FileName = "photo.jpg",
                    SourcePath = sourcePath,
                    DateTaken = DateTime.UtcNow,
                };
                string result = IngestFileNaming.ResolveFileName(
                    item,
                    targetDir,
                    "[Original]",
                    "Shoot",
                    1,
                    DuplicateHandlingMode.OverwriteIfNewer,
                    out bool skipped);

                Assert.False(skipped);
                Assert.Equal("photo.jpg", result);
            }
            finally
            {
                Directory.Delete(targetDir, true);
            }
        }
    }
}
