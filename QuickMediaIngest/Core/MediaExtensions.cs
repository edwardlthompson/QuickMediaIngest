#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Shared image/video extension checks for local, ADB, and FTP scanners.</summary>
    public static class MediaExtensions
    {
        private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jpe", ".jfif", ".jif", ".thm",
            ".png", ".gif", ".bmp", ".dib",
            ".tif", ".tiff",
            ".webp",
            ".heic", ".heif", ".hif", ".heifs",
            ".avif", ".avifs",
            ".jxl",
            ".jxr", ".wdp", ".hdp",
            ".jp2", ".j2k", ".j2c", ".jpx",
            ".tga", ".targa", ".exr", ".hdr",
            ".pcx", ".ppm", ".pgm", ".pbm", ".pnm",
            ".qoi", ".ico", ".cin", ".dpx",
            ".fits", ".fit",
            ".mpo", ".psd", ".insp",
            ".dng", ".cr2", ".cr3", ".crw",
            ".nef", ".nrw",
            ".arw", ".sr2", ".srf",
            ".raf", ".orf",
            ".rw2", ".rwl",
            ".srw", ".pef", ".raw",
            ".kdc", ".dcr", ".k25",
            ".mrw", ".x3f", ".iiq",
            ".fff", ".3fr", ".mef", ".mos", ".erf",
        };

        private static readonly HashSet<string> Raw = new(StringComparer.OrdinalIgnoreCase)
        {
            ".dng", ".cr2", ".cr3", ".crw",
            ".nef", ".nrw",
            ".arw", ".sr2", ".srf",
            ".raf", ".orf",
            ".rw2", ".rwl",
            ".srw", ".pef", ".raw",
            ".kdc", ".dcr", ".k25",
            ".mrw", ".x3f", ".iiq",
            ".fff", ".3fr", ".mef", ".mos", ".erf",
        };

        private static readonly HashSet<string> Videos = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".m4v", ".mov", ".qt",
            ".avi", ".wmv", ".asf",
            ".mkv", ".webm",
            ".3gp", ".3g2",
            ".mts", ".m2ts", ".ts", ".m2t",
            ".mpg", ".mpeg", ".mpe", ".m1v", ".m2v",
            ".ogv", ".ogm", ".flv", ".f4v", ".mxf",
            ".lrv", ".insv",
            ".mjpg", ".mjpeg",
            ".dv", ".mod", ".tod", ".vob", ".amv",
        };

        private static readonly HashSet<string> HeifFamily = new(StringComparer.OrdinalIgnoreCase)
        {
            ".heic", ".heif", ".hif", ".heifs", ".avif", ".avifs",
        };

        private static readonly HashSet<string> JpegFamily = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".jpe", ".jfif", ".jif", ".thm",
        };

        public static bool IsMediaFile(string fileName) =>
            !IsAndroidTrashOrNoise(fileName) &&
            IsMediaExtension(Path.GetExtension(fileName));

        public static bool IsAndroidTrashOrNoise(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return true;
            }

            string name = Path.GetFileName(fileName.Trim());
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }

            if (name.Equals(".nomedia", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return name.StartsWith(".trashed-", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsAndroidTrashDirectory(string dirName)
        {
            if (string.IsNullOrWhiteSpace(dirName))
            {
                return false;
            }

            string name = Path.GetFileName(dirName.Trim().TrimEnd('/', '\\'));
            return name.Equals(".Trash", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("trash", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMediaExtension(string extension) =>
            IsImageExtension(extension) || IsVideoExtension(extension);

        public static bool IsImageExtension(string ext) => Images.Contains(ext ?? string.Empty);

        public static bool IsRawExtension(string ext) => Raw.Contains(ext ?? string.Empty);

        public static bool IsVideoExtension(string ext) => Videos.Contains(ext ?? string.Empty);

        public static bool IsHeifFamily(string ext) => HeifFamily.Contains(ext ?? string.Empty);

        public static bool IsJpegFamily(string ext) => JpegFamily.Contains(ext ?? string.Empty);
    }
}
