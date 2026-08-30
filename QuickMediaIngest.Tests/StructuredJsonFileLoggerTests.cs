#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class StructuredJsonFileLoggerTests
    {
        [Fact]
        public void Log_WritesJsonFormattedEntry()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"structured-log-{Guid.NewGuid():N}.jsonl");

            try
            {
                var logger = new StructuredJsonFileLogger("TestCategory", tempFile);
                logger.LogInformation("Test message {Code}", 42);

                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("\"Category\":\"TestCategory\"", content);
                Assert.Contains("\"Level\":\"Information\"", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
