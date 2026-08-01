#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Merges removable-drive and FTP sidebar sources into one item list for unified mode.</summary>
    public interface IUnifiedConcreteSourceScanService
    {
        Task<UnifiedScanMergeResult> MergeAllAsync(
            IReadOnlyList<object> concreteSources,
            bool forceRefresh,
            bool scanSubfolders,
            IDictionary<string, List<ImportItem>> itemCache,
            IProgress<(int Completed, int Total)>? mergeProgress = null,
            CancellationToken cancellationToken = default,
            bool preferAdbTransfer = false,
            IProgress<UnifiedScanSourceCompleted>? sourceCompleted = null);
    }
}
