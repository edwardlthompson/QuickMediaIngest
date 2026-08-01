#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace QuickMediaIngest.Core
{
    /// <summary>Maps FTP-style remote paths to Android device paths for ADB pull/delete.</summary>
    public static class AdbAndroidPath
    {
        private static readonly string[] RootPrefixes =
        {
            "/sdcard",
            "/storage/emulated/0",
        };

        /// <summary>Normalizes an FTP remote path; rejects empty and path traversal.</summary>
        public static bool TryNormalizeRemote(string? ftpRemotePath, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(ftpRemotePath))
            {
                return false;
            }

            string trimmed = ftpRemotePath.Trim().Replace('\\', '/');
            if (trimmed.Contains("..", StringComparison.Ordinal))
            {
                return false;
            }

            while (trimmed.Contains("//", StringComparison.Ordinal))
            {
                trimmed = trimmed.Replace("//", "/", StringComparison.Ordinal);
            }

            if (!trimmed.StartsWith('/'))
            {
                trimmed = "/" + trimmed;
            }

            if (trimmed.Length > 1)
            {
                trimmed = trimmed.TrimEnd('/');
            }

            normalized = trimmed;
            return true;
        }

        /// <summary>
        /// Candidate absolute directories to probe for the FTP remote folder
        /// (e.g. /DCIM → /sdcard/DCIM, /storage/emulated/0/DCIM).
        /// </summary>
        public static IReadOnlyList<string> CandidateDirectoryRoots(string ftpRemoteFolder)
        {
            if (!TryNormalizeRemote(ftpRemoteFolder, out string folder))
            {
                return Array.Empty<string>();
            }

            var list = new List<string>(RootPrefixes.Length);
            foreach (string prefix in RootPrefixes)
            {
                list.Add(prefix + folder);
            }

            return list;
        }

        /// <summary>
        /// Derives the media root prefix (/sdcard or /storage/emulated/0) from a probed full directory.
        /// </summary>
        public static bool TryGetMediaRootPrefix(string probedFullDirectory, string ftpRemoteFolder, out string mediaRootPrefix)
        {
            mediaRootPrefix = string.Empty;
            if (!TryNormalizeRemote(ftpRemoteFolder, out string folder) ||
                !TryNormalizeRemote(probedFullDirectory, out string full))
            {
                return false;
            }

            if (!full.EndsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string prefix = full[..^folder.Length];
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }

            mediaRootPrefix = prefix.TrimEnd('/');
            return mediaRootPrefix.Length > 0;
        }

        /// <summary>Maps an FTP remote file path onto the chosen media root prefix.</summary>
        public static string ToDevicePath(string mediaRootPrefix, string ftpRemotePath)
        {
            if (string.IsNullOrWhiteSpace(mediaRootPrefix))
            {
                throw new ArgumentException("Media root prefix is required.", nameof(mediaRootPrefix));
            }

            if (!TryNormalizeRemote(ftpRemotePath, out string remote))
            {
                throw new ArgumentException("Invalid FTP remote path.", nameof(ftpRemotePath));
            }

            // Already absolute under a known Android root — leave as-is.
            foreach (string prefix in RootPrefixes)
            {
                if (remote.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase) ||
                    remote.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return remote;
                }
            }

            string root = mediaRootPrefix.Trim().Replace('\\', '/').TrimEnd('/');
            return root + remote;
        }
    }
}
