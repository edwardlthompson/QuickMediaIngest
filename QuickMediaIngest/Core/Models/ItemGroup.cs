#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace QuickMediaIngest.Core.Models
{
    /// <summary>
    /// Represents a group of import items, typically grouped by time or album.
    /// </summary>
    public class ItemGroup : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _keywordsText = string.Empty;
        private bool _isSelected = true;
        private bool _expandStackedPairsInShoot;
        private bool _isExpanded;

        /// <summary>
        /// The title of the group (e.g., "Shoot 1").
        /// </summary>
        public string Title 
        { 
            get => _title; 
            set { _title = value; OnPropertyChanged(); } 
        }

        /// <summary>
        /// Optional keywords for this shoot (hashtags and/or comma-separated). Applied on import when embedding is enabled.
        /// </summary>
        public string KeywordsText
        {
            get => _keywordsText;
            set { _keywordsText = value ?? string.Empty; OnPropertyChanged(); }
        }

        /// <summary>
        /// The start date of the group (earliest item).
        /// </summary>
        public DateTime StartDate { get; set; }
        /// <summary>
        /// The end date of the group (latest item).
        /// </summary>
        public DateTime EndDate { get; set; }
        /// <summary>
        /// The album name for the group (UI-bound).
        /// </summary>
        public string AlbumName { get; set; } = string.Empty;
        /// <summary>
        /// The folder path for the group (e.g., source directory).
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;
        /// <summary>
        /// The list of import items in this group.
        /// </summary>
        public List<ImportItem> Items { get; set; } = new List<ImportItem>();

        /// <summary>
        /// Whether the group is selected for import. Setting this also updates all contained items.
        /// </summary>
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

        /// <summary>
        /// Synchronizes the group's selection state from its items.
        /// </summary>
        public void SyncSelectionFromItems()
        {
            _isSelected = Items.Count > 0 && Items.All(i => i.IsSelected);
            OnPropertyChanged(nameof(IsSelected));
        }

        /// <summary>
        /// The total size (in bytes) of all selected items in the group.
        /// </summary>
        public long TotalSize => Items.FindAll(i => i.IsSelected).ConvertAll(i => i.FileSize).Sum();

        /// <summary>
        /// When RAW/JPEG stacking is enabled, expands stacked pairs side-by-side for this shoot only.
        /// </summary>
        public bool ExpandStackedPairsInShoot
        {
            get => _expandStackedPairsInShoot;
            set { _expandStackedPairsInShoot = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Controls whether this shoot group is expanded in the UI.
        /// Defaults to collapsed for faster scanning.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
