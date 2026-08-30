#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class KeywordAutocompleteCatalogTests
    {
        [Fact]
        public void RecordAndSuggest_ReturnsRankedMatches()
        {
            var catalog = new KeywordAutocompleteCatalog();
            catalog.RecordKeywords(new[] { "Landscape", "Landscape", "Portrait", "LongExposure", "Lantern" });

            var suggestions = catalog.GetSuggestions("L", 5);
            Assert.Contains("Landscape", suggestions);
            Assert.Contains("LongExposure", suggestions);
            Assert.Contains("Lantern", suggestions);
            Assert.DoesNotContain("Portrait", suggestions);
            Assert.Equal("Landscape", suggestions[0]);
        }
    }
}
