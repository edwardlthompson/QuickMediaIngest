#nullable enable
using System;
using System.Collections.Concurrent;

namespace QuickMediaIngest.Core
{
    /// <summary>
    /// Session-scoped cache of permanent FTP failures (550 / 5xx file unavailable)
    /// so parallel thumbnail workers do not retry the same path.
    /// </summary>
    public static class FtpPermanentFailureCache
    {
        private static readonly ConcurrentDictionary<string, byte> Failed =
            new(StringComparer.OrdinalIgnoreCase);

        public static string BuildKey(string host, int port, string remotePath) =>
            $"{host}|{port}|{NormalizePath(remotePath)}";

        public static bool IsFailed(string host, int port, string remotePath) =>
            Failed.ContainsKey(BuildKey(host, port, remotePath));

        public static void MarkFailed(string host, int port, string remotePath) =>
            Failed[BuildKey(host, port, remotePath)] = 0;

        /// <summary>Clear all entries for a host:port (call on successful reconnect).</summary>
        public static void ClearEndpoint(string host, int port)
        {
            string prefix = $"{host}|{port}|";
            foreach (string key in Failed.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    Failed.TryRemove(key, out _);
                }
            }
        }

        public static void ClearAll() => Failed.Clear();

        private static string NormalizePath(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                return "/";
            }

            string trimmed = remotePath.Trim().Replace('\\', '/');
            while (trimmed.Contains("//", StringComparison.Ordinal))
            {
                trimmed = trimmed.Replace("//", "/", StringComparison.Ordinal);
            }

            if (!trimmed.StartsWith('/'))
            {
                trimmed = "/" + trimmed;
            }

            return trimmed;
        }
    }
}
