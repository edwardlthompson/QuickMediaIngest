#nullable enable
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ShootTitleBatchRenamerTests
    {
        [Fact]
        public void RenameShootsWithUniqueness_EnsuresDistinctNames()
        {
            var g1 = new ItemGroup { Title = "Old1" };
            var g2 = new ItemGroup { Title = "Old2" };
            var g3 = new ItemGroup { Title = "Old3" };

            var list = new List<ItemGroup> { g1, g2, g3 };
            ShootTitleBatchRenamer.RenameShootsWithUniqueness(list, "Wedding Day");

            Assert.Equal("Wedding Day 1", g1.Title);
            Assert.Equal("Wedding Day 2", g2.Title);
            Assert.Equal("Wedding Day 3", g3.Title);
        }

        [Fact]
        public void RenameShootsWithUniqueness_SingleShoot_UsesBaseTitleDirectly()
        {
            var g1 = new ItemGroup { Title = "Old1" };
            ShootTitleBatchRenamer.RenameShootsWithUniqueness(new[] { g1 }, "Vacation");

            Assert.Equal("Vacation", g1.Title);
        }
    }
}
