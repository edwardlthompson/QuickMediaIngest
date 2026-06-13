#nullable enable
namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Represents progress information for an FTP scan operation.
    /// </summary>
    public class FtpScanProgress
    {
        public string Phase { get; set; } = "Prescan";
        public string CurrentFolder { get; set; } = "/";
        public int ProcessedFolders { get; set; }
        public int TotalFolders { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public int CurrentFolderProcessedFiles { get; set; }
        public int CurrentFolderTotalFiles { get; set; }
        public int SkippedFolders { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
