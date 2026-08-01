#nullable enable
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>One concrete source finished during unified merge (progressive UI).</summary>
    public sealed class UnifiedScanSourceCompleted
    {
        public required string SourceKey { get; init; }
        public required bool IsFtp { get; init; }
        public required IReadOnlyList<ImportItem> Items { get; init; }
        public string? FailureNote { get; init; }
        public int CompletedSources { get; init; }
        public int TotalSources { get; init; }
    }
}
