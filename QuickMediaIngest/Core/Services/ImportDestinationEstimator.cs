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

        /// <summary>Calculates forecasted remaining free space after importing selected items.</summary>
        public static (long selectedBytes, long? availableFreeBytes, long? forecastRemainingBytes, bool isSufficientSpace) ForecastSpace(IEnumerable<ItemGroup> groups, string destinationRoot)
        {
            long selectedBytes = SumSelectedBytes(groups);
            long? freeBytes = TryGetFreeBytes(destinationRoot);
            if (!freeBytes.HasValue)
            {
                return (selectedBytes, null, null, true);
            }

            long remaining = freeBytes.Value - selectedBytes;
            return (selectedBytes, freeBytes.Value, remaining, remaining >= 0);
        }

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
