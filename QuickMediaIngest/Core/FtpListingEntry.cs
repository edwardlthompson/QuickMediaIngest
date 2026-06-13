#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    public sealed class FtpListingEntry
    {
        public string Name { get; init; } = string.Empty;
        public string FullPath { get; init; } = "/";
        public bool IsDirectory { get; init; }
        public long Size { get; init; }
        public DateTime Modified { get; init; } = DateTime.Now;
    }
}
