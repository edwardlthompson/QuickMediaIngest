#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core.CrashCapture;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DiscardedCrashFingerprintTests
    {
        [Fact]
        public void DiscardedFingerprint_IsNotReCaptured()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"crash-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            string storePath = Path.Combine(tempDir, "pending-crash.json");

            try
            {
                var store = new FilePendingCrashStore(storePath);
                var service = new CrashCaptureService(store, () => true);

                var ex = new InvalidOperationException("Test discarded exception");
                bool captured = service.TryCapture(ex, "1.0.0");
                Assert.True(captured);

                var crash = service.Peek();
                Assert.NotNull(crash);
                string fp = crash.Fingerprint;

                store.MarkDiscarded(fp);
                Assert.True(store.IsDiscarded(fp));
                Assert.Null(service.Peek());

                // Re-capture attempt of the identical exception must fail
                bool reCaptured = service.TryCapture(ex, "1.0.0");
                Assert.False(reCaptured);
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
    }
}
