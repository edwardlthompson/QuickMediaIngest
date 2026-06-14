#nullable enable
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    /// <summary>Public FTP thumbnail disk cache API for ViewModels.</summary>
    public static class FtpThumbnailCache
    {
        public static bool IsAcceptable(BitmapSource? thumb) =>
            ThumbnailPreviewValidator.IsAcceptable(thumb);

        public static BitmapSource? TryLoad(string host, int port, string remotePath, long fileSize) =>
            ThumbnailDiskCache.TryLoadFtp(host, port, remotePath, fileSize);

        public static void TrySave(BitmapSource thumb, string host, int port, string remotePath, long fileSize) =>
            ThumbnailDiskCache.TrySaveFtp(thumb, host, port, remotePath, fileSize);
    }
}
