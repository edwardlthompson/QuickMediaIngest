#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.Core.Services;
using QuickMediaIngest.Thumbnails;

namespace QuickMediaIngest.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private async Task ApplyFtpThumbnailResultAsync(
            FtpThumbnailItemResult result,
            IReadOnlyDictionary<string, ImportItem> itemByKey)
        {
            if (!itemByKey.TryGetValue(result.ItemKey, out var item))
            {
                return;
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (result.Thumbnail != null && FtpThumbnailCache.IsAcceptable(result.Thumbnail))
                {
                    var bitmap = WpfThumbnailBridge.ToBitmapSource(result.Thumbnail);
                    if (bitmap != null)
                    {
                        item.Thumbnail = bitmap;
                        item.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                        _thumbnailByItemKey[result.ItemKey] = bitmap;
                        return;
                    }
                }

                item.Thumbnail = null;
                item.ThumbnailPreviewStatus = result.Thumbnail != null
                    ? ThumbnailPreviewStatus.Failed
                    : result.Status;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task ApplyRenderedSiblingThumbnailsAsync(IReadOnlyList<ImportItem> items)
        {
            if (!GroupRawAndRenderedPairs)
            {
                return;
            }

            var byStem = items
                .GroupBy(i => Path.GetFileNameWithoutExtension(i.FileName), StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var group in byStem)
            {
                ImportItem? rendered = group.FirstOrDefault(i =>
                {
                    string ext = Path.GetExtension(i.FileName).ToLowerInvariant();
                    return ext is ".heic" or ".heif" or ".jpg" or ".jpeg"
                        && i.Thumbnail is System.Windows.Media.Imaging.BitmapSource renderedBitmap
                        && renderedBitmap.PixelWidth >= ThumbnailPreviewValidator.MinPixelEdge
                        && renderedBitmap.PixelHeight >= ThumbnailPreviewValidator.MinPixelEdge;
                });

                if (rendered == null)
                {
                    continue;
                }

                foreach (ImportItem raw in group.Where(i =>
                    MediaExtensions.IsRawExtension(Path.GetExtension(i.FileName))
                    && !(i.Thumbnail is System.Windows.Media.Imaging.BitmapSource rawBitmap
                         && rawBitmap.PixelWidth >= ThumbnailPreviewValidator.MinPixelEdge
                         && rawBitmap.PixelHeight >= ThumbnailPreviewValidator.MinPixelEdge)))
                {
                    string rawKey = BuildItemKey(raw);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        raw.Thumbnail = rendered.Thumbnail;
                        raw.ThumbnailPreviewStatus = ThumbnailPreviewStatus.Loaded;
                        _thumbnailByItemKey[rawKey] = rendered.Thumbnail;
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
        }

        private async Task ApplyFtpThumbnailBatchResultsAsync(
            IReadOnlyList<FtpThumbnailItemResult> results,
            IReadOnlyDictionary<string, ImportItem> itemByKey)
        {
            foreach (var result in results)
            {
                await ApplyFtpThumbnailResultAsync(result, itemByKey);
            }
        }

        private bool ShouldSkipFtpThumbnailWorkItem(ImportItem item, IReadOnlyList<ImportItem> batchItems)
        {
            if (!GroupRawAndRenderedPairs)
            {
                return false;
            }

            string ext = Path.GetExtension(item.FileName);
            if (!MediaExtensions.IsRawExtension(ext))
            {
                return false;
            }

            string stem = Path.GetFileNameWithoutExtension(item.FileName);
            foreach (var other in batchItems)
            {
                if (ReferenceEquals(other, item))
                {
                    continue;
                }

                if (!string.Equals(Path.GetFileNameWithoutExtension(other.FileName), stem, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string otherExt = Path.GetExtension(other.FileName).ToLowerInvariant();
                if (otherExt is ".heic" or ".heif" or ".jpg" or ".jpeg")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
