#nullable enable

namespace QuickMediaIngest.Core
{
    /// <summary>Immutable snapshot of byte-weighted import progress (parallel-safe).</summary>
    public sealed class ImportByteProgressSnapshot
    {
        public long TotalBytes { get; init; }
        public int TotalFiles { get; init; }
        public long CompletedBytes { get; init; }
        public long EffectiveBytes { get; init; }
        public int FilesCompleted { get; init; }
        public int FilesFailed { get; init; }
        public int FilesInFlight { get; init; }

        public int FilesProcessed => FilesCompleted + FilesFailed;
    }
}
