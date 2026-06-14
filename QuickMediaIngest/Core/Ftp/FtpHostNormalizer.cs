#nullable enable
using System;

namespace QuickMediaIngest.Core
{
    public static class FtpHostNormalizer
    {
        public static string Normalize(string? host)
        {
            return TryParseHostAndPort(host, out string normalized, out _) ? normalized : string.Empty;
        }

        public static bool TryParseHostAndPort(string? host, out string normalizedHost, out int? port)
        {
            normalizedHost = string.Empty;
            port = null;

            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            string value = host.Trim();

            if (value.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring("ftp://".Length);
            }
            else if (value.StartsWith("ftps://", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring("ftps://".Length);
            }

            int atIndex = value.LastIndexOf('@');
            if (atIndex >= 0)
            {
                value = value.Substring(atIndex + 1);
            }

            value = value.Trim().TrimEnd('/');

            int slashIndex = value.IndexOf('/');
            if (slashIndex >= 0)
            {
                value = value.Substring(0, slashIndex);
            }

            string uriInput = value.Contains("://", StringComparison.Ordinal)
                ? value
                : "ftp://" + value;

            if (Uri.TryCreate(uriInput, UriKind.Absolute, out Uri? uri) &&
                !string.IsNullOrWhiteSpace(uri.Host))
            {
                normalizedHost = uri.Host;
                if (uri.Port > 0)
                {
                    port = uri.Port;
                }

                return true;
            }

            normalizedHost = value.Trim();
            return !string.IsNullOrWhiteSpace(normalizedHost);
        }
    }
}
