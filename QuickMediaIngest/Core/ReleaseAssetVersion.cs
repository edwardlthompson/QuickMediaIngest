#nullable enable
using System;
using System.Text.RegularExpressions;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Parses product installer versions from release asset filenames (not git tags).
    /// </summary>
    public static class ReleaseAssetVersion
    {
        private static readonly Regex SemVer = new(@"\d+\.\d+\.\d+", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static Version? TryParse(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            Match match = SemVer.Match(fileName);
            if (!match.Success || !Version.TryParse(match.Value, out Version? parsed) || parsed == null)
            {
                return null;
            }

            return ToProduct(parsed);
        }

        public static Version ToProduct(Version version)
        {
            int build = version.Build < 0 ? 0 : version.Build;
            return new Version(version.Major, version.Minor, build);
        }

        public static bool IsNewer(Version remote, Version current) => ToProduct(remote) > ToProduct(current);

        public static bool SameProduct(Version left, Version right) => ToProduct(left) == ToProduct(right);

        /// <summary>
        /// Portable: versioned .exe that is not a setup wrapper. Installer: .msi or a name containing "setup".
        /// </summary>
        public static bool MatchesPackage(string fileName, string? packageType)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            bool installer = string.Equals(packageType, "Installer", StringComparison.OrdinalIgnoreCase);
            bool setup = fileName.Contains("setup", StringComparison.OrdinalIgnoreCase);
            bool exe = fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            bool msi = fileName.EndsWith(".msi", StringComparison.OrdinalIgnoreCase);

            if (installer)
            {
                return msi || setup;
            }

            return exe && !setup;
        }
    }
}
