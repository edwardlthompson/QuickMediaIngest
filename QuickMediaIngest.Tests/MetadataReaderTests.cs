#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class MetadataReaderTests
    {
        [Fact]
        public void ReadMetadata_MissingFile_LeavesDateTakenUnchanged()
        {
            var logger = new Mock<ILogger<MetadataReader>>();
            var reader = new MetadataReader(logger.Object);
            var original = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Local);
            var item = new ImportItem
            {
                FileName = "missing.jpg",
                SourcePath = Path.Combine(Path.GetTempPath(), "qmi_missing_" + Guid.NewGuid() + ".jpg"),
                DateTaken = original,
            };

            reader.ReadMetadata(item);

            Assert.Equal(original, item.DateTaken);
        }

        [Fact]
        public void ReadMetadata_NoExif_FallsBackToLastWriteTime()
        {
            string path = Path.Combine(Path.GetTempPath(), "qmi_meta_" + Guid.NewGuid() + ".jpg");
            // Minimal JPEG SOI/EOI — no EXIF directory
            File.WriteAllBytes(path, [0xFF, 0xD8, 0xFF, 0xD9]);
            try
            {
                DateTime stamp = new DateTime(2021, 6, 15, 12, 30, 0, DateTimeKind.Local);
                File.SetLastWriteTime(path, stamp);

                var logger = new Mock<ILogger<MetadataReader>>();
                var reader = new MetadataReader(logger.Object);
                var item = new ImportItem
                {
                    FileName = Path.GetFileName(path),
                    SourcePath = path,
                    DateTaken = DateTime.Now,
                };

                reader.ReadMetadata(item);

                Assert.Equal(stamp, item.DateTaken);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
