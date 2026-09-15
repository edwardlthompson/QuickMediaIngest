#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using QuickMediaIngest.Core.AppModel;

namespace QuickMediaIngest.Core
{
    /// <summary>Lists removable / media mounts for the Linux ingest-bench (also uses DriveInfo).</summary>
    public static class MountEnumerator
    {
        public static List<VolumeChoice> ListVolumes(IEnumerable<string>? extraRoots = null)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<VolumeChoice>();
            foreach (string root in EnumerateCandidatePaths(extraRoots))
            {
                string full = Path.GetFullPath(root);
                if (!Directory.Exists(full) || !seen.Add(full))
                {
                    continue;
                }

                string name = Path.GetFileName(full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (string.IsNullOrEmpty(name))
                {
                    name = full;
                }

                list.Add(new VolumeChoice(full, name, GuessKind(full)));
            }

            return list;
        }

        private static IEnumerable<string> EnumerateCandidatePaths(IEnumerable<string>? extraRoots)
        {
            if (extraRoots is not null)
            {
                foreach (string extra in extraRoots)
                {
                    if (!string.IsNullOrWhiteSpace(extra))
                    {
                        yield return extra;
                    }
                }
            }

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                if (drive.DriveType is DriveType.Removable or DriveType.CDRom)
                {
                    yield return drive.RootDirectory.FullName;
                }
            }

            string user = Environment.UserName;
            foreach (string parent in new[]
                     {
                         Path.Combine("/media", user),
                         Path.Combine("/run/media", user),
                     })
            {
                if (!Directory.Exists(parent))
                {
                    continue;
                }

                string[] kids;
                try
                {
                    kids = Directory.GetDirectories(parent);
                }
                catch
                {
                    continue;
                }

                foreach (string kid in kids)
                {
                    yield return kid;
                }
            }
        }

        private static string GuessKind(string path)
        {
            string name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (VolumeScanHint.HasCameraFolder(path))
            {
                return "Card";
            }

            if (VolumeScanHint.IsDeniedLabel(name))
            {
                return "Disk";
            }

            string n = path.Replace('\\', '/');
            if (n.StartsWith("/media/", StringComparison.Ordinal) || n.StartsWith("/run/media/", StringComparison.Ordinal))
            {
                return "Disk";
            }

            return "Local";
        }
    }
}
