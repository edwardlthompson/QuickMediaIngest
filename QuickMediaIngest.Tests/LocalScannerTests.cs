using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class LocalScannerTests
    {
        [Fact]
        public void Scan_IncludesOnlyMediaExtensions()
        {
            string root = Path.Combine(Path.GetTempPath(), "qmi_local_scan_" + Guid.NewGuid());
            Directory.CreateDirectory(root);
            try
            {
                File.WriteAllText(Path.Combine(root, "photo.jpg"), "jpg");
                File.WriteAllText(Path.Combine(root, "clip.mp4"), "mp4");
                File.WriteAllText(Path.Combine(root, "notes.txt"), "txt");
                File.WriteAllText(Path.Combine(root, "thumb.ctg"), "ctg");

                var logger = new Mock<ILogger<LocalScanner>>();
                var scanner = new LocalScanner(logger.Object);
                var items = scanner.Scan(root, includeSubfolders: false);

                Assert.Equal(2, items.Count);
                Assert.Contains(items, i => i.FileName == "photo.jpg");
                Assert.Contains(items, i => i.FileName == "clip.mp4" && i.IsVideo);
                Assert.DoesNotContain(items, i => i.FileName == "notes.txt");
                Assert.DoesNotContain(items, i => i.FileName == "thumb.ctg");
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Scan_IncludeSubfolders_FindsNestedMedia()
        {
            string root = Path.Combine(Path.GetTempPath(), "qmi_local_scan_" + Guid.NewGuid());
            string nested = Path.Combine(root, "DCIM", "100CANON");
            Directory.CreateDirectory(nested);
            try
            {
                File.WriteAllText(Path.Combine(nested, "IMG_0001.JPG"), "jpg");

                var logger = new Mock<ILogger<LocalScanner>>();
                var scanner = new LocalScanner(logger.Object);
                var items = scanner.Scan(root, includeSubfolders: true);

                Assert.Single(items);
                Assert.Equal("IMG_0001.JPG", items[0].FileName);
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void Scan_MissingDirectory_ReturnsEmptyList()
        {
            var logger = new Mock<ILogger<LocalScanner>>();
            var scanner = new LocalScanner(logger.Object);
            var items = scanner.Scan(Path.Combine(Path.GetTempPath(), "qmi_missing_" + Guid.NewGuid()), includeSubfolders: true);
            Assert.Empty(items);
        }
    }
}
