#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Shared image/video extension checks for local and FTP scanners.</summary>
    public static class MediaExtensions
    {
        public static bool IsMediaFile(string fileName) =>
            IsMediaExtension(Path.GetExtension(fileName));

        public static bool IsMediaExtension(string extension)
        {
            string ext = extension.ToLowerInvariant();
            return IsImageExtension(ext) || IsVideoExtension(ext);
        }

        public static bool IsImageExtension(string ext) =>
            ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tif" or ".tiff" or ".webp"
                or ".dng" or ".cr2" or ".cr3" or ".nef" or ".arw" or ".raf" or ".orf" or ".rw2"
                or ".srw" or ".heic" or ".heif";

        public static bool IsRawExtension(string ext) =>
            ext is ".dng" or ".cr2" or ".cr3" or ".nef" or ".arw" or ".raf" or ".orf" or ".rw2" or ".srw";

        public static bool IsVideoExtension(string ext) =>
            ext is ".mp4" or ".mov" or ".m4v" or ".avi" or ".wmv" or ".mkv" or ".3gp" or ".mts" or ".m2ts" or ".mpg" or ".mpeg";
    }
}
