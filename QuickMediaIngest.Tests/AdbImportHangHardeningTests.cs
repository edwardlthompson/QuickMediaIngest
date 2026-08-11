#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class AdbImportHangHardeningTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1024)]
        [InlineData(100L * 1024 * 1024)]
        public void AdbPullTimeout_Compute_StaysWithinFloorAndCeiling(long bytes)
        {
            TimeSpan t = AdbPullTimeout.Compute(bytes);
            Assert.True(t >= AdbPullTimeout.Floor);
            Assert.True(t <= AdbPullTimeout.Ceiling);
        }

        [Fact]
        public void AdbPullTimeout_Compute_UnknownUsesFloor()
        {
            Assert.Equal(AdbPullTimeout.Floor, AdbPullTimeout.Compute(0));
            Assert.Equal(AdbPullTimeout.Floor, AdbPullTimeout.Compute(-5));
        }

        [Fact]
        public void AdbPullTimeout_Compute_CapsAtCeilingForHugeFiles()
        {
            // 2GB @ 1MB/s would be huge; must clamp to 10 min
            TimeSpan t = AdbPullTimeout.Compute(2L * 1024 * 1024 * 1024);
            Assert.Equal(AdbPullTimeout.Ceiling, t);
        }

        [Fact]
        public void AdbPullTimeout_Compute_ScalesBetweenFloorAndCeiling()
        {
            // 400MB → 400s + 120s = 520s ≈ 8.67 min
            TimeSpan t = AdbPullTimeout.Compute(400L * 1024 * 1024);
            Assert.True(t > AdbPullTimeout.Floor);
            Assert.True(t < AdbPullTimeout.Ceiling);
            Assert.InRange(t.TotalMinutes, 8.0, 9.5);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(-5, 2)]
        [InlineData(1, 1)]
        [InlineData(8, 2)]
        [InlineData(2, 2)]
        public void AdbTransferIo_CapConcurrentCopies(int requested, int expected)
        {
            Assert.Equal(expected, AdbTransferIo.CapConcurrentCopies(requested));
        }

        [Fact]
        public void AdbTransferIo_IsAdbBackedProvider_DetectsRemapping()
        {
            var logger = new Mock<ILogger<AdbFileProvider>>();
            var adb = new AdbFileProvider("serial", logger.Object);
            var remapping = new RemappingFileProvider(adb, "/sdcard");
            Assert.True(AdbTransferIo.IsAdbBackedProvider(adb));
            Assert.True(AdbTransferIo.IsAdbBackedProvider(remapping));
            Assert.True(remapping.InnerIsAdb);
            Assert.False(AdbTransferIo.IsAdbBackedProvider(null));
        }

        [Theory]
        [InlineData(100_000_000, 50_000_000, ImportFreeSpaceDecision.AbortInsufficient)]
        [InlineData(10_000_000, 500_000_000, ImportFreeSpaceDecision.Allow)]
        [InlineData(0, 100_000_000, ImportFreeSpaceDecision.WarnUnknownSizesLowFree)]
        [InlineData(0, 500_000_000, ImportFreeSpaceDecision.Allow)]
        public void ImportFreeSpaceGate_Evaluate(long selected, long free, ImportFreeSpaceDecision expected)
        {
            Assert.Equal(expected, ImportFreeSpaceGate.Evaluate(selected, free));
        }

        [Fact]
        public void ImportFreeSpaceGate_Evaluate_AllowsWhenFreeUnknown()
        {
            Assert.Equal(ImportFreeSpaceDecision.Allow, ImportFreeSpaceGate.Evaluate(999_999_999, null));
        }

        [Fact]
        public void TryDeletePartialDestination_RemovesExistingFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "qmi_partial_" + Guid.NewGuid().ToString("N") + ".bin");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
            var logger = new Mock<ILogger>();
            try
            {
                IngestItemProcessor.TryDeletePartialDestination(path, logger.Object);
                Assert.False(File.Exists(path));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Fact]
        public void TryDeletePartialDestination_NoOpsOnMissingOrEmpty()
        {
            var logger = new Mock<ILogger>();
            IngestItemProcessor.TryDeletePartialDestination("", logger.Object);
            IngestItemProcessor.TryDeletePartialDestination(
                Path.Combine(Path.GetTempPath(), "qmi_missing_" + Guid.NewGuid().ToString("N")),
                logger.Object);
        }

        [Fact]
        public void AdbFileProvider_DocumentsPipeDrainRequirement()
        {
            // Regression guard: AdbFileProvider.RunAdbAsync must ReadToEndAsync stdout+stderr
            // concurrently with WaitForExitAsync. Redirect without drain deadlocks adb pull
            // once progress fills the OS pipe (~few MB), matching hung PreferAdb imports.
            string source = File.ReadAllText(
                Path.Combine(RepoRoot(), "QuickMediaIngest", "Core", "AdbFileProvider.cs"));
            Assert.Contains("Drain stdout/stderr", source, StringComparison.Ordinal);
            Assert.Contains("ReadToEndAsync()", source, StringComparison.Ordinal);
            Assert.Contains("WaitForExitAsync", source, StringComparison.Ordinal);
        }

        private static string RepoRoot()
        {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                if (File.Exists(Path.Combine(dir, "QuickMediaIngest-1.sln")))
                {
                    return dir;
                }

                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }

            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        }
    }
}
