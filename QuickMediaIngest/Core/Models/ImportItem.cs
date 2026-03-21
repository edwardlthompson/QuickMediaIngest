using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuickMediaIngest.Core.Models
{
    public class ImportItem : INotifyPropertyChanged
    {
        public string SourcePath { get; set; } = string.Empty; // Local "E:\DCIM\Image.jpg" OR FTP "/DCIM/Image.jpg"
        public string SourceId { get; set; } = string.Empty; // Stable source key used for unified view/import routing
        public bool IsFtpSource { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime DateTaken { get; set; }
        public bool IsVideo { get; set; }
        public string FileType { get; set; } = string.Empty; // "JPG", "CR2", "MP4"
        
        private bool _isSelected = true;
        public bool IsSelected 
        { 
            get => _isSelected; 
            set { _isSelected = value; OnPropertyChanged(); } 
        }

        private object? _thumbnail;
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
