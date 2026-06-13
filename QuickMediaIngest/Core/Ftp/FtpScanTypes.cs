#nullable enable
using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core
{
    internal sealed class FtpScanFile
    {
        public string FullPath { get; set; } = "/";
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Modified { get; set; } = DateTime.Now;
    }

    internal sealed class FtpFolderScanPlan
    {
        public string Folder { get; set; } = "/";
        public List<FtpScanFile> Files { get; set; } = new();
        public bool IsSkipped { get; set; }
        public string SkipReason { get; set; } = string.Empty;
    }
}
