using System;
using System.Collections.Generic;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class GroupBuilderTests
    {
        [Fact]
        public void BuildGroups_SplitsGroupsByTimeGap()
        {
            // Arrange
            var items = new List<ImportItem>
            {
                new ImportItem { DateTaken = new DateTime(2024, 3, 20, 10, 0, 0) },
                new ImportItem { DateTaken = new DateTime(2024, 3, 20, 10, 5, 0) },
                new ImportItem { DateTaken = new DateTime(2024, 3, 20, 12, 0, 0) }, // > gap
                new ImportItem { DateTaken = new DateTime(2024, 3, 20, 12, 2, 0) }
            };
            var builder = new GroupBuilder();
            TimeSpan gap = TimeSpan.FromMinutes(30);

            // Act
            var groups = builder.BuildGroups(items, gap);

            // Assert
            Assert.Equal(2, groups.Count);
            Assert.Equal(2, groups[0].Items.Count);
            Assert.Equal(2, groups[1].Items.Count);
        }
    }
}
