#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestCollisionAnalyzerTests
    {
        [Fact]
        public void Analyze_DetectsCollisionsAndReportsActions()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"collision-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var group = new ItemGroup { Title = "EventShoot" };
                string targetDir = Path.Combine(tempDir, GroupFolderNaming.GetTargetFolderName(group));
                Directory.CreateDirectory(targetDir);

                // Create a pre-existing destination file
                string existingFile = Path.Combine(targetDir, "2026-08-30_IMG_0001.JPG");
                File.WriteAllText(existingFile, "old content");

                var item1 = new ImportItem
                {
                    FileName = "IMG_0001.JPG",
                    SourcePath = @"C:\Source\IMG_0001.JPG",
                    DateTaken = new DateTime(2026, 8, 30),
                    IsSelected = true
                };

                var item2 = new ImportItem
                {
                    FileName = "IMG_0002.JPG",
                    SourcePath = @"C:\Source\IMG_0002.JPG",
                    DateTaken = new DateTime(2026, 8, 30),
                    IsSelected = true
                };

                group.Items.Add(item1);
                group.Items.Add(item2);

                string template = "[Date]_[Original]";

                // 1. Suffix mode
                var suffixReport = IngestCollisionAnalyzer.Analyze(new[] { group }, tempDir, template, DuplicateHandlingMode.Suffix);
                Assert.Equal(2, suffixReport.TotalFiles);
                Assert.Equal(1, suffixReport.CollisionsCount);
                Assert.Equal("Suffix", suffixReport.Items[0].ActionTaken);
                Assert.Equal("Copy", suffixReport.Items[1].ActionTaken);

                // 2. Skip mode
                var skipReport = IngestCollisionAnalyzer.Analyze(new[] { group }, tempDir, template, DuplicateHandlingMode.Skip);
                Assert.Equal(1, skipReport.CollisionsCount);
                Assert.Equal("Skip", skipReport.Items[0].ActionTaken);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
