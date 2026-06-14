#nullable enable
using System;
using System.Linq;

namespace QuickMediaIngest.Core
{
    /// <summary>Canonical FTP URI construction shared by listing, download, and thumbnail fetch.</summary>
    internal static class FtpUriBuilder
    {
        public static Uri Build(string host, int port, string remotePath)
        {
            host = FtpHostNormalizer.Normalize(host);
            string normalized = FtpListingParser.NormalizeRemotePath(remotePath);
            string encodedPath = string.Join(
                "/",
                normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

            string uriText = string.IsNullOrEmpty(encodedPath)
                ? $"ftp://{host}:{port}/"
                : $"ftp://{host}:{port}/{encodedPath}";

            return new Uri(uriText);
        }
    }
}
