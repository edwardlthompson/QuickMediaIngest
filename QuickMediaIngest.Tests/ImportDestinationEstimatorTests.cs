#nullable enable
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ImportDestinationEstimatorTests
    {
        [Fact]
        public void ForecastSpace_CalculatesSelectedAndForecastCorrectly()
        {
            var group = new ItemGroup { Title = "Shoot1" };
            group.Items.Add(new ImportItem { FileName = "a.jpg", FileSize = 1000, IsSelected = true });
            group.Items.Add(new ImportItem { FileName = "b.jpg", FileSize = 2000, IsSelected = true });
            group.Items.Add(new ImportItem { FileName = "c.jpg", FileSize = 5000, IsSelected = false });

            var result = ImportDestinationEstimator.ForecastSpace(new[] { group }, "C:\\");

            Assert.Equal(3000, result.selectedBytes);
            if (result.availableFreeBytes.HasValue)
            {
                Assert.Equal(result.availableFreeBytes.Value - 3000, result.forecastRemainingBytes);
            }
        }
    }
}
