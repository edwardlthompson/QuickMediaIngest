#nullable enable
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed class UnifiedScanMergeResult
    {
        public List<ImportItem> UnifiedItems { get; init; } = new();
        public HashSet<string> FtpListingFailures { get; init; } = new(System.StringComparer.OrdinalIgnoreCase);
    }
}
