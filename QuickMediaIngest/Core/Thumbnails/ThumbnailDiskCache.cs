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

        public static void TrySave(BitmapSource thumb, string cachePath)
        {
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(thumb));
            using var fs = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None);
            encoder.Save(fs);
        }

        private static string GetCacheKey(string filePath)
        {
            const string cacheVersion = "thumb-v4";
            string input = cacheVersion + "|" + filePath + "|" + File.GetLastWriteTimeUtc(filePath).Ticks.ToString();
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
