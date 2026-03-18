using System;
using System.Collections.Generic;

namespace QuickMediaIngest.Core.Models
{
    public class ItemGroup
    {
        public string Title { get; set; } = string.Empty; // e.g., "Shoot 1" or "Vacation"
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AlbumName { get; set; } = string.Empty; // Bound to UI TextBox
        public string FolderPath { get; set; } = string.Empty; // e.g., "E:\DCIM\100CANON"
        public List<ImportItem> Items { get; set; } = new List<ImportItem>();
        
        // Helper to get total size
        public long TotalSize => Items.FindAll(i => i.IsSelected).ConvertAll(i => i.FileSize).Sum();
    }
}
