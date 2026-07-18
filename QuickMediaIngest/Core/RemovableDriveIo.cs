#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Detects removable/USB media and caps I/O parallelism.
    /// High concurrency on SD cards stalls preview decode and import copies.
    /// </summary>
    public static class RemovableDriveIo
    {
        public const int MaxPreviewWorkers = 2;
        public const int MaxConcurrentCopies = 1;

        public static bool IsOnRemovableDrive(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string? root = Path.GetPathRoot(Path.GetFullPath(path));
                if (string.IsNullOrEmpty(root))
                {
                    return false;
                }

                return new DriveInfo(root).DriveType == DriveType.Removable;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static int CapPreviewWorkers(int requested, string? samplePath)
        {
            int workers = Math.Max(1, requested);
            return IsOnRemovableDrive(samplePath)
                ? Math.Min(workers, MaxPreviewWorkers)
                : workers;
        }

        /// <summary>
        /// Caps concurrent file copies. When <paramref name="requested"/> is 0 (engine default),
        /// removable media is forced to <see cref="MaxConcurrentCopies"/>.
        /// </summary>
        public static int CapConcurrentCopies(int requested, string? samplePath)
        {
            if (!IsOnRemovableDrive(samplePath))
            {
                return requested;
            }

            if (requested <= 0)
            {
                return MaxConcurrentCopies;
            }

            return Math.Min(requested, MaxConcurrentCopies);
        }
    }
}
