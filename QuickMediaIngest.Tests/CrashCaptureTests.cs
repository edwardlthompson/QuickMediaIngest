#nullable enable
using QuickMediaIngest.Core.CrashCapture;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class CrashCaptureTests
    {
        private sealed class MemoryStore : IPendingCrashStore
        {
            public PendingCrash? Item { get; set; }
            public PendingCrash? Load() => Item;
            public bool Replace(PendingCrash record)
            {
                Item = record;
                return true;
            }
            public void Clear() => Item = null;
        }

        [Fact]
        public void OptInFalse_DoesNotPersist()
        {
            var store = new MemoryStore();
            var svc = new CrashCaptureService(store, () => false);
            Assert.False(svc.TryCapture(new InvalidOperationException("boom"), "1.3.27"));
            Assert.Null(store.Load());
        }

        [Fact]
        public void OptInTrue_SanitizesAndKeepsAtMostOne()
        {
            var store = new MemoryStore();
            var svc = new CrashCaptureService(store, () => true);
            Assert.True(svc.TryCapture(new InvalidOperationException("C:\\Users\\Ada\\x ghp_abcdefghijklmnopqrstuvwxyz012345"), "1.3.27"));
            Assert.True(svc.TryCapture(new ArgumentException("second"), "1.3.27"));
            PendingCrash? pending = store.Load();
            Assert.NotNull(pending);
            Assert.Equal("ArgumentException", pending!.ExceptionType);
            Assert.DoesNotContain("Ada", pending.Description, StringComparison.Ordinal);
            Assert.DoesNotContain("ghp_", pending.Stack, StringComparison.Ordinal);
        }

        [Fact]
        public void ApplyOptInFalse_ClearsStore()
        {
            var store = new MemoryStore { Item = new PendingCrash { Fingerprint = "abc" } };
            var svc = new CrashCaptureService(store, () => true);
            svc.ApplyOptIn(false);
            Assert.Null(store.Load());
        }
    }
}
