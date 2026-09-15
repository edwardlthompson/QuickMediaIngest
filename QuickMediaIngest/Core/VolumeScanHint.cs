#nullable enable
using System;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Default drive-picker checks: camera cards on, data disks (Games, Steam) off.</summary>
    public static class VolumeScanHint
    {
        private static readonly string[] DeniedLabels =
        {
            "games", "steam", "steamlibrary", "windows", "win10", "win11", "timeshift", "backup",
        };

        public static bool HasCameraFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return false;
            }

            try
            {
                return Directory.Exists(Path.Combine(path, "DCIM"))
                    || Directory.Exists(Path.Combine(path, "dcim"));
            }
            catch
            {
                return false;
            }
        }

        public static bool IsDeniedLabel(string? label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            string name = label.Trim();
            foreach (string denied in DeniedLabels)
            {
                if (name.Equals(denied, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ShouldSelectByDefault(string path, string label)
        {
            if (HasCameraFolder(path))
            {
                return true;
            }

            if (IsDeniedLabel(label) || IsDeniedLabel(Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))))
            {
                return false;
            }

            try
            {
                var drive = new DriveInfo(path);
                if (drive.IsReady && drive.DriveType is DriveType.Removable or DriveType.CDRom)
                {
                    return true;
                }
            }
            catch
            {
                // Linux bind mounts often throw or report Fixed.
            }

            return false;
        }
    }
}
