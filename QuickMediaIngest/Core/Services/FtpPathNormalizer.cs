#nullable enable

namespace QuickMediaIngest.Core.Services
{
    public static class FtpPathNormalizer
    {
        /// <summary>
        /// Normalize an FTP remote path to absolute form, collapsing <c>.</c> and <c>..</c>
        /// without climbing above the FTP root.
        /// </summary>
        public static string Normalize(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            string trimmed = path.Trim().Replace("\\", "/");
            var parts = new System.Collections.Generic.List<string>();
            foreach (string segment in trimmed.Split('/', System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }

                    continue;
                }

                parts.Add(segment);
            }

            return parts.Count == 0 ? "/" : "/" + string.Join("/", parts);
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
