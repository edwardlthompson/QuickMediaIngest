using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ImportByteProgressTrackerTests
    {
        [Fact]
        public void RegisterFileCompleted_IncrementsCountersAndBytes()
        {
            var tracker = new ImportByteProgressTracker(totalBytes: 1000, totalFiles: 2);
            tracker.RegisterFileStarted("a", 400);
            tracker.ReportBytes("a", 200);
            tracker.RegisterFileCompleted("a", 400, success: true);

            ImportByteProgressSnapshot snap = tracker.GetSnapshot();
            Assert.Equal(400, snap.CompletedBytes);
            Assert.Equal(400, snap.EffectiveBytes);
            Assert.Equal(1, snap.FilesCompleted);
            Assert.Equal(0, snap.FilesFailed);
            Assert.Equal(0, snap.FilesInFlight);
        }

        [Fact]
        public async Task ParallelCopies_ProduceMonotonicEffectiveBytes()
        {
            var tracker = new ImportByteProgressTracker(totalBytes: 3000, totalFiles: 3);
            long lastEffective = 0;
            tracker.ProgressChanged += snap =>
            {
                Assert.True(snap.EffectiveBytes >= lastEffective);
                lastEffective = snap.EffectiveBytes;
            };

            var tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    tracker.RegisterFileStarted("f1", 1000);
                    tracker.ReportBytes("f1", 500);
                    tracker.RegisterFileCompleted("f1", 1000, true);
                }),
                Task.Run(() =>
                {
                    tracker.RegisterFileStarted("f2", 1000);
                    tracker.ReportBytes("f2", 250);
                    tracker.RegisterFileCompleted("f2", 1000, true);
                }),
                Task.Run(() =>
                {
                    tracker.RegisterFileStarted("f3", 1000);
                    tracker.RegisterFileCompleted("f3", 1000, true);
                }),
            };

            await Task.WhenAll(tasks);

            ImportByteProgressSnapshot finalSnap = tracker.GetSnapshot();
            Assert.Equal(3000, finalSnap.CompletedBytes);
            Assert.Equal(3, finalSnap.FilesCompleted);
            Assert.Equal(3, finalSnap.FilesProcessed);
        }

        [Fact]
        public void ZeroByteTotal_DoesNotThrow()
        {
            var tracker = new ImportByteProgressTracker(totalBytes: 0, totalFiles: 1);
            tracker.RegisterFileStarted("z", 0);
            tracker.RegisterFileCompleted("z", 0, success: true);

            ImportByteProgressSnapshot snap = tracker.GetSnapshot();
            Assert.Equal(0, snap.EffectiveBytes);
            Assert.Equal(1, snap.FilesCompleted);
        }
    }
}
