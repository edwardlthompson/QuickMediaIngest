#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuickMediaIngest.Core.Models
{
    /// <summary>
    /// Represents a single importable media item (file) with metadata and selection state.
    /// </summary>
    public class ImportItem : INotifyPropertyChanged
    {
        // Indicates if this item is a duplicate (across sources)
        private bool _isDuplicate;
        public bool IsDuplicate
        {
            get => _isDuplicate;
            set { _isDuplicate = value; OnPropertyChanged(); }
        }
        /// <summary>
        /// The source path of the file (local or FTP).
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        /// <summary>
        /// Stable source key used for unified view/import routing.
        /// </summary>
        public string SourceId { get; set; } = string.Empty;
        /// <summary>
        /// Whether the source is FTP.
        /// </summary>
        public bool IsFtpSource { get; set; }
        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        /// <summary>
        /// The file size in bytes.
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// The date the photo or video was taken.
        /// </summary>
        public DateTime DateTaken { get; set; }
        /// <summary>
        /// Whether the file is a video.
        /// </summary>
        public bool IsVideo { get; set; }
        /// <summary>
        /// The file type/extension (e.g., "JPG", "MP4").
        /// </summary>
        public string FileType { get; set; } = string.Empty;
        
        private bool _isSelected = true;
        /// <summary>
        /// Whether the item is selected for import.
        /// </summary>
        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(); } 
        }

        private object? _thumbnail;
        /// <summary>
        /// The thumbnail image for the item (UI-bound).
        /// </summary>
        public object? Thumbnail 
        { 
            get => _thumbnail; 
            set { _thumbnail = value; OnPropertyChanged(); } 
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
