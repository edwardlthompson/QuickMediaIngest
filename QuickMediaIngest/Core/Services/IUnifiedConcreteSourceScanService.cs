#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Merges removable-drive and FTP sidebar sources into one item list for unified mode.</summary>
    public interface IUnifiedConcreteSourceScanService
    {
        Task<UnifiedScanMergeResult> MergeAllAsync(
            IReadOnlyList<object> concreteSources,
            bool forceRefresh,
            bool scanSubfolders,
            IDictionary<string, List<QuickMediaIngest.Core.Models.ImportItem>> itemCache,
            IProgress<(int Completed, int Total)>? mergeProgress = null,
            CancellationToken cancellationToken = default);
    }
}
