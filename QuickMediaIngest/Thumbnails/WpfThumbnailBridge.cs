#nullable enable
using System.IO;
using System.Windows.Media.Imaging;
using QuickMediaIngest.Core;

namespace QuickMediaIngest.Thumbnails
{
    /// <summary>Converts Core thumbnail payloads to WPF bitmaps for UI binding.</summary>
    public static class WpfThumbnailBridge
    {
        public static BitmapSource? ToBitmapSource(DecodedThumbnail? payload)
        {
            if (payload == null || payload.JpegBytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(payload.JpegBytes, writable: false);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
