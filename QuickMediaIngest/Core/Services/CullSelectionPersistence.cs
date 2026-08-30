#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public sealed class CullItemState
    {
        public bool IsSelected { get; set; }
        public bool IsRejected { get; set; }
        public int Rating { get; set; }
        public string ColorLabel { get; set; } = string.Empty;
    }

    public static class CullSelectionPersistence
    {
        private static readonly ConcurrentDictionary<string, CullItemState> States = new(StringComparer.OrdinalIgnoreCase);

        public static string BuildKey(ImportItem item)
        {
            return $"{item.FileName}|{item.FileSize}|{item.DateTaken:yyyyMMddHHmmss}";
        }

        public static void Snapshot(IEnumerable<ImportItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                string key = BuildKey(item);
                States[key] = new CullItemState
                {
                    IsSelected = item.IsSelected,
                    IsRejected = item.IsRejected,
                    Rating = item.Rating,
                    ColorLabel = item.ColorLabel
                };
            }
        }

        public static void Restore(IEnumerable<ImportItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                string key = BuildKey(item);
                if (States.TryGetValue(key, out var state))
                {
                    item.IsSelected = state.IsSelected;
                    item.IsRejected = state.IsRejected;
                    item.Rating = state.Rating;
                    item.ColorLabel = state.ColorLabel;
                }
            }
        }

        public static void Clear() => States.Clear();
    }
}
