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
        public int MaxConcurrentFileCopies { get; set; }
        public int DelayBetweenFilesMilliseconds { get; set; }
    }
}
