
using System.Collections.Generic;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Data.Models;
using Microsoft.Extensions.Logging;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class WhitelistFilterTests
    {
        [Fact]
        public void Filter_RemovesItemsNotMatchingRules()
        {
            // Arrange
            var items = new List<ImportItem>
            {
                new ImportItem { FileName = "IMG_001.JPG", SourceId = "dev1", SourcePath = "/photos/IMG_001.JPG" },
                new ImportItem { FileName = "IMG_002.JPG", SourceId = "dev2", SourcePath = "/photos/IMG_002.JPG" }
            };
            var rules = new List<WhitelistRule>
            {
                new WhitelistRule { DeviceId = "dev1", Path = "/photos/IMG_001.JPG", RuleType = "Folder" }
            };
            var logger = new Mock<ILogger<WhitelistFilter>>();
            var filter = new WhitelistFilter(logger.Object);

            // Act
            var filtered = filter.Filter(items, rules);

            // Assert
            Assert.Single(filtered);
            Assert.Equal("IMG_001.JPG", filtered[0].FileName);
        }
    }
}
