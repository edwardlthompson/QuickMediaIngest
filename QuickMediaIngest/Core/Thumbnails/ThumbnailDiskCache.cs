#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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

        public static DecodedThumbnail? TryLoad(string cachePath)
        {
            if (!File.Exists(cachePath))
            {
                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(cachePath);
                if (bytes.Length == 0)
                {
                    return null;
                }

                if (!JpegSofDimensionParser.TryGetDimensions(bytes, out int width, out int height))
                {
                    return null;
                }

                return new DecodedThumbnail(bytes, width, height);
            }
            catch
            {
                return null;
            }
        }

        public static DecodedThumbnail? TryLoadFtp(string host, int port, string remotePath, long fileSize)
        {
            string cachePath = GetFtpCachePath(host, port, remotePath, fileSize);
            DecodedThumbnail? thumb = TryLoad(cachePath);
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

        public static void TrySave(DecodedThumbnail thumb, string cachePath)
        {
            File.WriteAllBytes(cachePath, thumb.JpegBytes);
        }

        public static void TrySave(byte[] jpegBytes, string cachePath)
        {
            File.WriteAllBytes(cachePath, jpegBytes);
        }

        public static void TrySaveFtp(DecodedThumbnail thumb, string host, int port, string remotePath, long fileSize)
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
