#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Services
{
    public sealed class BenchmarkResult
    {
        public double SequentialElapsedMs { get; set; }
        public double ParallelElapsedMs { get; set; }
        public double SpeedupFactor => SequentialElapsedMs > 0 ? ParallelElapsedMs / SequentialElapsedMs : 1.0;
    }

    public static class RemovableDriveThrottleHarness
    {
        public static async Task<BenchmarkResult> RunBenchmarkAsync(string directory, int fileCount = 10, int fileSize = 1024 * 512, CancellationToken cancellationToken = default)
        {
            var result = new BenchmarkResult();
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return result;
            }

            // Create test files
            var files = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                files[i] = Path.Combine(directory, $"bench_{i}.dat");
                await File.WriteAllBytesAsync(files[i], new byte[fileSize], cancellationToken).ConfigureAwait(false);
            }

            try
            {
                // Measure sequential read
                var sw = Stopwatch.StartNew();
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    byte[] data = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                }
                sw.Stop();
                result.SequentialElapsedMs = sw.Elapsed.TotalMilliseconds;

                // Measure parallel read
                sw.Restart();
                await Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken }, async (file, ct) =>
                {
                    byte[] data = await File.ReadAllBytesAsync(file, ct).ConfigureAwait(false);
                }).ConfigureAwait(false);
                sw.Stop();
                result.ParallelElapsedMs = sw.Elapsed.TotalMilliseconds;
            }
            finally
            {
                foreach (var file in files)
                {
                    if (File.Exists(file)) File.Delete(file);
                }
            }

            return result;
        }
    }
}
