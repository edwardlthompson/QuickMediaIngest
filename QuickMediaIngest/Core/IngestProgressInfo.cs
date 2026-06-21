#nullable enable
namespace QuickMediaIngest.Core
{
    public class IngestProgressInfo
    {
        public string GroupTitle { get; set; } = string.Empty;
        public int GroupCurrent { get; set; }
        public int GroupTotal { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsStarted { get; set; }
    }
}
