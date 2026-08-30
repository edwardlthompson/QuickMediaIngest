#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class WhatsNewReaderTests
    {
        [Fact]
        public void ReadLatestHighlights_ReadsChangelogBulletPoints()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"changelog-{Guid.NewGuid():N}.md");
            File.WriteAllText(tempFile, "## [1.0.0]\n- Added fast thumbnail caching\n- Added dual FTP de-dupe\n");

            try
            {
                var highlights = WhatsNewReader.ReadLatestHighlights(tempFile, 5);
                Assert.Equal(2, highlights.Count);
                Assert.Contains("Added fast thumbnail caching", highlights);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
