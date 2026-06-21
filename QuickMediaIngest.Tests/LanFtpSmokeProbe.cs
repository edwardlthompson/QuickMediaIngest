#nullable enable
using System;
using System.Net;
using QuickMediaIngest.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace QuickMediaIngest.Tests
{
    /// <summary>Detects the LAN FTP smoke server (default 10.0.0.23:2221/DCIM). Skips gracefully when offline.</summary>
    public sealed record LanFtpEndpoint(string Host, int Port, string User, string Pass, string RemoteFolder)
    {
        public FtpEndpoint ToFtpEndpoint() => new(Host, Port, User, Pass);
    }

    public static class LanFtpSmokeProbe
    {
        private static bool? _cachedReachable;

        public static LanFtpEndpoint FromEnvironment() =>
            new(
                Environment.GetEnvironmentVariable("QMI_SMOKE_FTP_HOST") ?? "10.0.0.23",
                int.TryParse(Environment.GetEnvironmentVariable("QMI_SMOKE_FTP_PORT"), out int port) ? port : 2221,
                Environment.GetEnvironmentVariable("QMI_SMOKE_FTP_USER") ?? "android",
                Environment.GetEnvironmentVariable("QMI_SMOKE_FTP_PASS") ?? "android",
                Environment.GetEnvironmentVariable("QMI_SMOKE_FTP_REMOTE") ?? "/DCIM");

        public static bool IsRequireEnabled() =>
            string.Equals(Environment.GetEnvironmentVariable("QMI_SMOKE_REQUIRE"), "1", StringComparison.Ordinal);

        public static bool TryIsReachable(LanFtpEndpoint? endpoint = null)
        {
            if (_cachedReachable.HasValue)
            {
                return _cachedReachable.Value;
            }

            endpoint ??= FromEnvironment();
            try
            {
                string folder = endpoint.RemoteFolder.TrimEnd('/');
                var uri = new Uri($"ftp://{endpoint.Host}:{endpoint.Port}{folder}/");
                var request = (FtpWebRequest)WebRequest.Create(uri);
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(endpoint.User, endpoint.Pass);
                request.Timeout = 8000;
                request.ReadWriteTimeout = 8000;
                request.UsePassive = true;
                using FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                _cachedReachable = response.StatusCode is FtpStatusCode.OpeningData
                    or FtpStatusCode.DataAlreadyOpen
                    or FtpStatusCode.ClosingData
                    or FtpStatusCode.CommandOK;
            }
            catch
            {
                _cachedReachable = false;
            }

            return _cachedReachable.Value;
        }

        /// <returns>True when tests should continue; false when skipped (offline).</returns>
        public static bool EnsureReachable(ITestOutputHelper output)
        {
            if (TryIsReachable())
            {
                var ep = FromEnvironment();
                output.WriteLine($"LAN FTP smoke server reachable at {ep.Host}:{ep.Port}{ep.RemoteFolder}");
                return true;
            }

            output.WriteLine(
                "SKIP: LAN FTP smoke server unreachable. " +
                "Set QMI_SMOKE_FTP_* env vars or start the test FTP at 10.0.0.23:2221.");
            if (IsRequireEnabled())
            {
                Assert.Fail("QMI_SMOKE_REQUIRE=1 but LAN FTP smoke server is unreachable.");
            }

            return false;
        }
    }
}
