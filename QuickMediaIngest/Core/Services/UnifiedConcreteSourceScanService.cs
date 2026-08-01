#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed partial class UnifiedConcreteSourceScanService : IUnifiedConcreteSourceScanService
    {
        private readonly ILocalScanner _scanner;
        private readonly IFtpScanner _ftpScanner;
        private readonly IAdbMediaScanner _adbMediaScanner;
        private readonly IAdbPathProbe _adbPathProbe;
        private readonly ILogger<UnifiedConcreteSourceScanService> _logger;

        public UnifiedConcreteSourceScanService(
            ILocalScanner scanner,
            IFtpScanner ftpScanner,
            IAdbMediaScanner adbMediaScanner,
            IAdbPathProbe adbPathProbe,
            ILogger<UnifiedConcreteSourceScanService> logger)
        {
            _scanner = scanner;
            _ftpScanner = ftpScanner;
            _adbMediaScanner = adbMediaScanner;
            _adbPathProbe = adbPathProbe;
            _logger = logger;
        }

        public async Task<UnifiedScanMergeResult> MergeAllAsync(
            IReadOnlyList<object> concreteSources,
            bool forceRefresh,
            bool scanSubfolders,
            IDictionary<string, List<ImportItem>> itemCache,
            IProgress<(int Completed, int Total)>? mergeProgress = null,
            CancellationToken cancellationToken = default,
            bool preferAdbTransfer = false,
            IProgress<UnifiedScanSourceCompleted>? sourceCompleted = null)
        {
            _logger.LogDebug(
                "Unified merge started for {SourceCount} sources (forceRefresh={ForceRefresh}, preferAdb={PreferAdb}).",
                concreteSources.Count,
                forceRefresh,
                preferAdbTransfer);
            var ftpListingFailures = new ConcurrentBag<string>();
            var cacheSync = new object();
            int completed = 0;
            int totalSources = concreteSources.Count;

            async Task<List<ImportItem>> ScanOneWithProgressAsync(object src)
            {
                (List<ImportItem> result, string sourceKey, bool isFtp, string? failureNote) = await ScanOneDetailedAsync(
                        src,
                        forceRefresh,
                        scanSubfolders,
                        preferAdbTransfer,
                        itemCache,
                        cacheSync,
                        ftpListingFailures,
                        cancellationToken)
                    .ConfigureAwait(false);

                int done = Interlocked.Increment(ref completed);
                mergeProgress?.Report((done, Math.Max(totalSources, 1)));
                sourceCompleted?.Report(new UnifiedScanSourceCompleted
                {
                    SourceKey = sourceKey,
                    IsFtp = isFtp,
                    Items = result,
                    FailureNote = failureNote,
                    CompletedSources = done,
                    TotalSources = Math.Max(totalSources, 1),
                });
                return result;
            }

            List<ImportItem>[] parallelResults =
                await Task.WhenAll(concreteSources.Select(src => ScanOneWithProgressAsync(src))).ConfigureAwait(false);

            return new UnifiedScanMergeResult
            {
                UnifiedItems = parallelResults.SelectMany(r => r).ToList(),
                FtpListingFailures = new HashSet<string>(ftpListingFailures, StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
