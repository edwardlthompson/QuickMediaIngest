using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class KeywordInputParserTests
    {
        [Fact]
        public void Parse_ReturnsEmptyForNullOrWhitespace()
        {
            Assert.Empty(KeywordInputParser.Parse(null));
            Assert.Empty(KeywordInputParser.Parse("   "));
        }

        [Fact]
        public void Parse_ExtractsHashtagsWithoutHash()
        {
            var result = KeywordInputParser.Parse("#beach, #golden-hour, sunset");

            Assert.Equal(new[] { "beach", "golden-hour", "sunset" }, result);
        }

        [Fact]
        public void Parse_SplitsCommaAndSemicolonSegments()
        {
            var result = KeywordInputParser.Parse("alpha; beta, gamma");

            Assert.Equal(new[] { "alpha", "beta", "gamma" }, result);
        }

        [Fact]
        public void Parse_DeduplicatesCaseInsensitive()
        {
            var result = KeywordInputParser.Parse("Tag, #tag, TAG");

            Assert.Single(result);
            Assert.Equal("tag", result[0]);
        }
    }
}
