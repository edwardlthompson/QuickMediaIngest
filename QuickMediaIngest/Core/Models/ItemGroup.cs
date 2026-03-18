using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QuickMediaIngest.Core.Models
{
    public class ItemGroup : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private bool _isSelected = true;

        public string Title 
        { 
            get => _title; 
            set { _title = value; OnPropertyChanged(); } 
        }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string AlbumName { get; set; } = string.Empty; // Bound to UI TextBox
        public string FolderPath { get; set; } = string.Empty; // e.g., "E:\DCIM\100CANON"
        public List<ImportItem> Items { get; set; } = new List<ImportItem>();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
                foreach (var item in Items)
                {
                    item.IsSelected = value;
                }
            }
        }

        public long TotalSize => Items.FindAll(i => i.IsSelected).ConvertAll(i => i.FileSize).Sum();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
