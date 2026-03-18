using System;

namespace QuickMediaIngest.Core.Models
{
    public class ImportItem
    {
        public string SourcePath { get; set; } = string.Empty; // Local "E:\DCIM\Image.jpg" OR FTP "/DCIM/Image.jpg"
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime DateTaken { get; set; }
        public bool IsVideo { get; set; }
        public string FileType { get; set; } = string.Empty; // "JPG", "CR2", "MP4"
        public bool IsSelected { get; set; } = true; // For UI checkboxes
        
        // Placeholder for thumbnail (will bind to UI)
        public object? Thumbnail { get; set; } 
    }
}
