#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    public sealed class FilterChip
    {
        public FilterChip(string id, string label)
        {
            Id = id;
            Label = label;
        }

        public string Id { get; }

        public string Label { get; }
    }

    /// <summary>Group-by hours, file-type filter, and removable keyword chips.</summary>
    public static class IngestBenchFilters
    {
        public static IReadOnlyList<string> FileTypes { get; } =
            new[] { "All", "Images", "Videos", "RAW", "JPEG" };

        public static TimeSpan Gap(IngestBenchAppModel model) =>
            TimeSpan.FromHours(Math.Clamp(model.TimeBetweenShootsHours, 1, 24));

        public static bool PassesFileType(ImportItem item, string? fileType)
        {
            string kind = string.IsNullOrWhiteSpace(fileType) ? "All" : fileType;
            string ext = Path.GetExtension(item.FileName);
            return kind switch
            {
                "Images" => MediaExtensions.IsImageExtension(ext),
                "Videos" => MediaExtensions.IsVideoExtension(ext),
                "RAW" => MediaExtensions.IsRawExtension(ext),
                "JPEG" => MediaExtensions.IsJpegFamily(ext),
                _ => true,
            };
        }

        public static void RefreshChips(IngestBenchAppModel model)
        {
            model.FilterChips.Clear();
            if (!string.IsNullOrWhiteSpace(model.ShootFilter))
            {
                model.FilterChips.Add(new FilterChip("kw", model.ShootFilter.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(model.FilterFileType)
                && !string.Equals(model.FilterFileType, "All", StringComparison.OrdinalIgnoreCase))
            {
                model.FilterChips.Add(new FilterChip("type", model.FilterFileType));
            }
        }

        public static void RemoveChip(IngestBenchHost host, string? id)
        {
            if (string.Equals(id, "kw", StringComparison.Ordinal))
            {
                host.Model.ShootFilter = string.Empty;
            }
            else if (string.Equals(id, "type", StringComparison.Ordinal))
            {
                host.Model.FilterFileType = "All";
            }

            IngestBenchShoots.ApplyFilter(host);
        }
    }
}
