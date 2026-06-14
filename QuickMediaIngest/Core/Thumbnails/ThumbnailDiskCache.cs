#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    internal static class ThumbnailDiskCache
    {
        private const string FtpCacheVersion = "ftp-thumb-v2";

        public static string GetCacheDirectory()
        {
            string cacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "QuickMediaIngest",
                "Thumbnails");
            Directory.CreateDirectory(cacheDir);
            return cacheDir;
        }

        public static string GetCachePath(string filePath)
        {
            return Path.Combine(GetCacheDirectory(), GetCacheKey(filePath) + ".jpg");
        }

        public static string GetFtpCachePath(string host, int port, string remotePath, long fileSize)
        {
            return Path.Combine(GetCacheDirectory(), GetFtpCacheKey(host, port, remotePath, fileSize) + ".jpg");
        }

        public static BitmapSource? TryLoad(string cachePath)
        {
            if (!File.Exists(cachePath))
            {
                return null;
            }

            var bitmap = new BitmapImage();
            using var stream = new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        public static BitmapSource? TryLoadFtp(string host, int port, string remotePath, long fileSize)
        {
            string cachePath = GetFtpCachePath(host, port, remotePath, fileSize);
            BitmapSource? thumb = TryLoad(cachePath);
            if (thumb == null)
            {
                return null;
            }

            if (ThumbnailPreviewValidator.IsAcceptable(thumb))
            {
                return thumb;
            }

            try
            {
                if (File.Exists(cachePath))
                {
                    File.Delete(cachePath);
                }
            }
            catch
            {
                // Ignore stale cache purge failures.
            }

            return null;
        }

        public static void TrySave(BitmapSource thumb, string cachePath)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumb));
            using var fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(fs);
        }

        public static void TrySaveFtp(BitmapSource thumb, string host, int port, string remotePath, long fileSize)
        {
            if (!ThumbnailPreviewValidator.IsAcceptable(thumb))
            {
                return;
            }

            try
            {
                TrySave(thumb, GetFtpCachePath(host, port, remotePath, fileSize));
            }
            catch
            {
                // Ignore cache write failures.
            }
        }

        private static string GetCacheKey(string filePath)
        {
            const string cacheVersion = "thumb-v4";
            string input = cacheVersion + "|" + filePath + "|" + File.GetLastWriteTimeUtc(filePath).Ticks.ToString();
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        private static string GetFtpCacheKey(string host, int port, string remotePath, long fileSize)
        {
            string normalizedHost = host.Trim().ToLowerInvariant();
            string normalizedPath = remotePath.Replace('\\', '/');
            string input = $"{FtpCacheVersion}|{normalizedHost}|{port}|{normalizedPath}|{fileSize}";
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
