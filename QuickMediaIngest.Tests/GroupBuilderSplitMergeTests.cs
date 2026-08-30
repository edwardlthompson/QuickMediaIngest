#nullable enable
using System;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class GroupBuilderSplitMergeTests
    {
        [Fact]
        public void SplitGroup_SplitsItemsAndUpdatesDates()
        {
            var g = new ItemGroup { Title = "Shoot 1" };
            g.Items.Add(new ImportItem { FileName = "1.jpg", DateTaken = new DateTime(2026, 8, 30, 10, 0, 0) });
            g.Items.Add(new ImportItem { FileName = "2.jpg", DateTaken = new DateTime(2026, 8, 30, 11, 0, 0) });
            g.Items.Add(new ImportItem { FileName = "3.jpg", DateTaken = new DateTime(2026, 8, 30, 12, 0, 0) });

            var (p, s) = GroupBuilder.SplitGroup(g, 1, "Shoot 2");

            Assert.Single(p.Items);
            Assert.Equal(2, s.Items.Count);
            Assert.Equal("Shoot 2", s.Title);
            Assert.Equal(new DateTime(2026, 8, 30, 11, 0, 0), s.StartDate);
        }

        [Fact]
        public void MergeGroups_CombinesItemsCorrectly()
        {
            var g1 = new ItemGroup { Title = "Shoot 1" };
            g1.Items.Add(new ImportItem { FileName = "1.jpg", DateTaken = new DateTime(2026, 8, 30, 10, 0, 0) });

            var g2 = new ItemGroup { Title = "Shoot 2" };
            g2.Items.Add(new ImportItem { FileName = "2.jpg", DateTaken = new DateTime(2026, 8, 30, 12, 0, 0) });

            var merged = GroupBuilder.MergeGroups(g1, g2);

            Assert.Equal(2, merged.Items.Count);
            Assert.Equal(new DateTime(2026, 8, 30, 10, 0, 0), merged.StartDate);
            Assert.Equal(new DateTime(2026, 8, 30, 12, 0, 0), merged.EndDate);
        }
    }
}
