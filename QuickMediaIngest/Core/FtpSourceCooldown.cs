#nullable enable
using System;
using System.Collections.Concurrent;

namespace QuickMediaIngest.Core
{
    /// <summary>Process-local cooldown after unified FTP soft-fail (no disk persistence).</summary>
    public static class FtpSourceCooldown
    {
        public static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(60);

        private static readonly ConcurrentDictionary<string, DateTimeOffset> UnavailableUntil =
            new(StringComparer.OrdinalIgnoreCase);

        public static string HostPortKey(string host, int port) => $"{host}:{port}";

        public static void MarkFailed(string host, int port, TimeSpan? duration = null)
        {
            TimeSpan d = duration ?? DefaultDuration;
            UnavailableUntil[HostPortKey(host, port)] = DateTimeOffset.UtcNow.Add(d);
        }

        public static bool IsCoolingDown(string host, int port, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (!UnavailableUntil.TryGetValue(HostPortKey(host, port), out DateTimeOffset until))
            {
                return false;
            }

            remaining = until - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                UnavailableUntil.TryRemove(HostPortKey(host, port), out _);
                remaining = TimeSpan.Zero;
                return false;
            }

            return true;
        }

        /// <summary>Test helper — clears all cooldown entries.</summary>
        public static void ClearAll() => UnavailableUntil.Clear();
    }
}
