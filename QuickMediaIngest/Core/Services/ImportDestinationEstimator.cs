#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Core.Services
{
    public static class ImportDestinationEstimator
    {
        public static long SumSelectedBytes(IEnumerable<ItemGroup> groups) =>
            groups.SelectMany(g => g.Items).Where(i => i.IsSelected).Sum(i => (long)Math.Max(0, i.FileSize));

        /// <summary>Best-effort free space on the volume hosting <paramref name="destinationRoot"/>.</summary>
        public static long? TryGetFreeBytes(string destinationRoot)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(destinationRoot))
                {
                    return null;
                }

                string root = Path.GetPathRoot(Path.GetFullPath(destinationRoot)) ?? destinationRoot;
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (drive.Name.Equals(root, StringComparison.OrdinalIgnoreCase) || drive.RootDirectory.FullName.Equals(root, StringComparison.OrdinalIgnoreCase))
                    {
                        return drive.AvailableFreeSpace;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return null;
        }
    }
}
