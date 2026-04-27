#nullable enable
using System.Collections.Generic;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    /// <summary>Clone and stamp helpers shared by scan and view model code paths.</summary>
    public static class ImportItemListHelper
    {
        public static void StampItems(List<ImportItem> items, string sourceId, bool isFtp)
        {
            foreach (var item in items)
            {
                item.SourceId = sourceId;
                item.IsFtpSource = isFtp;
            }
        }

        public static List<ImportItem> CloneItems(List<ImportItem> items)
        {
            return items.Select(i => new ImportItem
            {
                SourcePath = i.SourcePath,
                SourceId = i.SourceId,
                IsFtpSource = i.IsFtpSource,
                FileName = i.FileName,
                FileSize = i.FileSize,
                DateTaken = i.DateTaken,
                IsVideo = i.IsVideo,
                FileType = i.FileType,
                IsSelected = i.IsSelected,
                Thumbnail = i.Thumbnail,
                IsPreviewVisible = i.IsPreviewVisible,
                PreviewLabel = i.PreviewLabel,
                StackKey = i.StackKey,
                IsStackRepresentative = i.IsStackRepresentative,
                ThumbnailPreviewStatus = i.ThumbnailPreviewStatus
            }).ToList();
        }
    }
}
