#nullable enable

namespace QuickMediaIngest.Core.Services
{
    public static class FtpPathNormalizer
    {
        public static string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            string normalized = path.Trim().Replace("\\", "/");
            if (!normalized.StartsWith("/", System.StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return normalized;
        }

        public static string BuildLocalSourceKey(string localPath)
        {
            return $"local|{localPath}";
        }

        /// <summary>Stable cache key for FTP sources (same format as <c>MainViewModel</c> source cache keys).</summary>
        public static string BuildFtpSourceKey(string host, int port, string remoteFolder)
        {
            return $"ftp|{host}|{port}|{Normalize(remoteFolder)}";
        }
    }
}
