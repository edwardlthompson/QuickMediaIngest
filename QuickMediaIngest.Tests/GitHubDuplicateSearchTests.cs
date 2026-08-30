#nullable enable
using System.Threading.Tasks;
using QuickMediaIngest.Core.GitHubFeedback;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class GitHubDuplicateSearchTests
    {
        [Fact]
        public async Task SearchDuplicatesFailSoftAsync_EmptyQuery_ReturnsEmptyList()
        {
            var results = await GitHubIssueComposer.SearchDuplicatesFailSoftAsync("");
            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchDuplicatesFailSoftAsync_WithQuery_FailsSoftWithoutThrowing()
        {
            var results = await GitHubIssueComposer.SearchDuplicatesFailSoftAsync("null reference exception");
            Assert.NotNull(results);
        }
    }
}
