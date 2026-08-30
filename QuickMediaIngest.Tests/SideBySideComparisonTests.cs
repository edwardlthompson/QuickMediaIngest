#nullable enable
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class SideBySideComparisonTests
    {
        [Fact]
        public void Swap_ExchangesLeftAndRightItems()
        {
            var itemA = new ImportItem { FileName = "A.JPG" };
            var itemB = new ImportItem { FileName = "B.JPG" };

            var state = new SideBySideComparisonState
            {
                LeftItem = itemA,
                RightItem = itemB
            };

            state.Swap();

            Assert.Same(itemB, state.LeftItem);
            Assert.Same(itemA, state.RightItem);
        }
    }
}
