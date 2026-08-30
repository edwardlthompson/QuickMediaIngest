#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class RemovableDriveThrottleHarnessTests
    {
        [Fact]
        public async Task RunBenchmarkAsync_ExecutesTimingMeasurement()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"bench-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = await RemovableDriveThrottleHarness.RunBenchmarkAsync(tempDir, fileCount: 3, fileSize: 1024 * 16);
                Assert.True(result.SequentialElapsedMs >= 0);
                Assert.True(result.ParallelElapsedMs >= 0);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
