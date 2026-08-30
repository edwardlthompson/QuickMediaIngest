#nullable enable
using System;
using System.Collections.Generic;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FtpAdbAliasFilterTests
    {
        [Fact]
        public void DeduplicateDualFtpAliases_RemovesDuplicateIdenticalMediaAcrossTransports()
        {
            var item1 = new ImportItem
            {
                FileName = "IMG_0001.JPG",
                FileSize = 2048,
                DateTaken = new DateTime(2026, 8, 30, 10, 0, 0),
                SourcePath = "ftp://192.168.1.100/DCIM/IMG_0001.JPG",
                IsFtpSource = true
            };

            var item2 = new ImportItem
            {
                FileName = "IMG_0001.JPG",
                FileSize = 2048,
                DateTaken = new DateTime(2026, 8, 30, 10, 0, 0),
                SourcePath = "/sdcard/DCIM/Camera/IMG_0001.JPG",
                IsFtpSource = false
            };

            var item3 = new ImportItem
            {
                FileName = "IMG_0002.JPG",
                FileSize = 4096,
                DateTaken = new DateTime(2026, 8, 30, 10, 0, 5),
                SourcePath = "/sdcard/DCIM/Camera/IMG_0002.JPG",
                IsFtpSource = false
            };

            var list = new List<ImportItem> { item1, item2, item3 };
            var deduplicated = FtpAdbAliasFilter.DeduplicateDualFtpAliases(list);

            Assert.Equal(2, deduplicated.Count);
            Assert.Contains(deduplicated, i => i.FileName == "IMG_0001.JPG");
            Assert.Contains(deduplicated, i => i.FileName == "IMG_0002.JPG");
        }
    }
}
