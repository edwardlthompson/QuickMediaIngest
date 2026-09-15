#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.AppModel
{
    /// <summary>CLI / xdg folders passed when an SD card is opened with this app.</summary>
    public static class LaunchMediaRoots
    {
        public static string[] Parse(IEnumerable<string>? args)
        {
            var roots = new List<string>();
            if (args is null)
            {
                return Array.Empty<string>();
            }

            foreach (string raw in args)
            {
                if (string.IsNullOrWhiteSpace(raw) || raw[0] == '-')
                {
                    continue;
                }

                string path = ToLocalPath(raw.Trim());
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path) ?? path;
                }

                if (!Directory.Exists(path))
                {
                    continue;
                }

                string full = Path.GetFullPath(path);
                if (!roots.Exists(r => string.Equals(r, full, StringComparison.OrdinalIgnoreCase)))
                {
                    roots.Add(full);
                }
            }

            return roots.ToArray();
        }

        public static bool Matches(string volumePath, IReadOnlyList<string> roots)
        {
            if (string.IsNullOrWhiteSpace(volumePath) || roots.Count == 0)
            {
                return false;
            }

            string volume = TrimSlash(Path.GetFullPath(volumePath));
            foreach (string root in roots)
            {
                string other = TrimSlash(Path.GetFullPath(root));
                if (string.Equals(volume, other, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (other.StartsWith(volume + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || volume.StartsWith(other + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Tick matching volumes, hide the picker, set scan roots. False if nothing matched.</summary>
        public static bool Apply(IngestBenchHost host, IReadOnlyList<string> roots)
        {
            if (roots.Count == 0)
            {
                return false;
            }

            IngestBenchScan.BeginPick(host, roots);
            foreach (VolumeChoice volume in host.Model.Volumes)
            {
                volume.IsSelected = Matches(volume.Path, roots);
            }

            if (!host.Model.Volumes.Any(v => v.IsSelected && Directory.Exists(v.Path)))
            {
                return false;
            }

            host.ConfirmDrivePick();
            return true;
        }

        private static string ToLocalPath(string raw)
        {
            if (Uri.TryCreate(raw, UriKind.Absolute, out Uri? uri) && uri.IsFile)
            {
                return uri.LocalPath;
            }

            return raw;
        }

        private static string TrimSlash(string path) =>
            path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
