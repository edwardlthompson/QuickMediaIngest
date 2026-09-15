#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging;

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
        /// Attempts to safely eject / dismount a drive volume.
        /// </summary>
        public static bool TryEjectVolume(string? driveOrPath, Microsoft.Extensions.Logging.ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(driveOrPath))
            {
                return false;
            }

            try
            {
                string? root = Path.GetPathRoot(Path.GetFullPath(driveOrPath));
                if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
                {
                    return false;
                }

                string driveLetter = root.TrimEnd('\\');
                if (OperatingSystem.IsWindows())
                {
                    var query = $"SELECT * FROM Win32_Volume WHERE DriveLetter = '{driveLetter}'";
                    using var searcher = new System.Management.ManagementObjectSearcher(query);
                    foreach (System.Management.ManagementObject volume in searcher.Get())
                    {
                        try
                        {
                            volume.InvokeMethod("Dismount", null);
                            volume.InvokeMethod("Remove", null);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            logger?.LogDebug(ex, "WMI dismount/remove failed for volume {DriveLetter}.", driveLetter);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Safe volume eject failed for {Drive}.", driveOrPath);
            }

            return false;
        }
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
