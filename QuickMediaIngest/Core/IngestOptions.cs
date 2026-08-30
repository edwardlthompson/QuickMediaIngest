#nullable enable
using System.Collections.Generic;

namespace QuickMediaIngest.Core
{
    public enum DuplicateHandlingMode
    {
        Suffix = 0,
        Skip = 1,
        OverwriteIfNewer = 2,
    }

    public enum ImportVerificationMode
    {
        Fast = 0,
        Strict = 1,
    }

    public sealed class IngestOptions
    {
        public DuplicateHandlingMode DuplicateHandling { get; set; } = DuplicateHandlingMode.Suffix;
        public ImportVerificationMode VerificationMode { get; set; } = ImportVerificationMode.Fast;
        public bool ApplyImportKeywords { get; set; }
        public IReadOnlyList<string>? ImportKeywords { get; set; }
        public bool StripGpsAndPii { get; set; }
        /// <summary>When true, simulates import calculations without performing file writes or deletions.</summary>
        public bool IsDryRun { get; set; }
        public string? CreatorStamp { get; set; }
        public string? CopyrightStamp { get; set; }
        public bool WriteXmpSidecarsOnly { get; set; }
        /// <summary>Optional secondary destination root for 3-2-1 dual-copy backups.</summary>
        public string? SecondaryDestinationRoot { get; set; }
        public int MaxConcurrentFileCopies { get; set; }
        public int DelayBetweenFilesMilliseconds { get; set; }
        public ImportByteProgressTracker? ByteProgressTracker { get; set; }
    }
}
