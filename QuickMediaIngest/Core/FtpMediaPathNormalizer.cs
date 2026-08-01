#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuickMediaIngest.Core
{
    /// <summary>HEIF→HEIC and related remote-path candidates for FTP/ADB RETR.</summary>
    public static class FtpMediaPathNormalizer
    {
        /// <summary>
        /// Yields RETR/probe candidates for a remote path: prefer existing sibling .heic in
        /// <paramref name="knownFileNames"/>, then .heic rewrite for .heif, then original.
        /// </summary>
        public static IEnumerable<string> GetRetrCandidates(
            string remotePath,
            IReadOnlyCollection<string>? knownFileNames = null)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                yield break;
            }

            string normalized = remotePath.Replace('\\', '/');
            string fileName = Path.GetFileName(normalized);
            string ext = Path.GetExtension(fileName);
            if (!ext.Equals(".heif", StringComparison.OrdinalIgnoreCase))
            {
                yield return normalized;
                yield break;
            }

            int slash = normalized.LastIndexOf('/');
            string directory = slash >= 0 ? normalized[..slash] : string.Empty;
            string stem = Path.GetFileNameWithoutExtension(fileName);
            string heicName = stem + ".heic";
            string heicPath = string.IsNullOrEmpty(directory) ? "/" + heicName : directory + "/" + heicName;

            bool siblingKnown = knownFileNames != null &&
                knownFileNames.Any(n => n.Equals(heicName, StringComparison.OrdinalIgnoreCase));

            if (siblingKnown)
            {
                yield return heicPath;
                yield return normalized;
                yield break;
            }

            // Try .heic first, always fall back to original .heif.
            yield return heicPath;
            yield return normalized;
        }

        /// <summary>Replaces the extension of a remote path (keeps directory).</summary>
        public static string WithExtension(string remotePath, string newExtension)
        {
            string normalized = remotePath.Replace('\\', '/');
            int slash = normalized.LastIndexOf('/');
            string directory = slash >= 0 ? normalized[..slash] : string.Empty;
            string stem = Path.GetFileNameWithoutExtension(Path.GetFileName(normalized));
            string ext = newExtension.StartsWith('.') ? newExtension : "." + newExtension;
            string name = stem + ext.ToLowerInvariant();
            return string.IsNullOrEmpty(directory) ? "/" + name : directory + "/" + name;
        }

        /// <summary>Rendered sibling extensions for RAW companion previews (.heic before .heif).</summary>
        public static IEnumerable<string> GetRenderedSiblingRemotePaths(string remotePath, string fileName)
        {
            int slash = remotePath.LastIndexOf('/');
            string directory = slash >= 0 ? remotePath[..slash] : string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (string siblingExt in new[] { ".heic", ".heif", ".jpg", ".jpeg" })
            {
                yield return string.IsNullOrEmpty(directory)
                    ? "/" + baseName + siblingExt
                    : directory + "/" + baseName + siblingExt;
            }
        }
    }
}
