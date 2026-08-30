#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class WatchFolderServiceTests
    {
        [Fact]
        public void StartAndStop_TogglesIsWatching()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"watch-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                using var service = new WatchFolderService(NullLogger<WatchFolderService>.Instance);
                Assert.False(service.IsWatching);

                service.StartWatching(tempDir);
                Assert.True(service.IsWatching);
                Assert.Equal(tempDir, service.WatchedDirectory);

                service.StopWatching();
                Assert.False(service.IsWatching);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
