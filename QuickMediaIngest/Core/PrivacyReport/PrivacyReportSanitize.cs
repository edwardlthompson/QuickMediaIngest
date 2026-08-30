#nullable enable
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickMediaIngest.Core.PrivacyReport;

/// <summary>Redact PII and secrets from crash/bug report text. No UI, no network.</summary>
public static class PrivacyReportSanitize
{
    public const int MaxBodyBytes = 8192;
    public const int MaxStackLines = 200;

    private static readonly Regex Pem = new(
        @"-----BEGIN [A-Z ]*PRIVATE KEY-----.*?-----END [A-Z ]*PRIVATE KEY-----",
        RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex GitHub = new(@"\b(?:ghp|gho|github_pat)_[A-Za-z0-9_]+", RegexOptions.Compiled);
    private static readonly Regex Bearer = new(@"Bearer\s+\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Jwt = new(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.Compiled);
    private static readonly Regex Aws = new(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled);
    private static readonly Regex Api = new(@"(?i)(?:api[_-]?key|token)\s*[:=]\s*\S+", RegexOptions.Compiled);
    private static readonly Regex Email = new(@"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", RegexOptions.Compiled);
    private static readonly Regex WinHome = new(@"(?i)C:\\Users\\[^\\]+\\", RegexOptions.Compiled);
    private static readonly Regex UnixHome = new(@"/(?:home|Users)/[^/\s]+/", RegexOptions.Compiled);
    private static readonly Regex Unc = new(@"\\\\[^\\\s]+\\[^\\\s]+\\", RegexOptions.Compiled);
    private static readonly Regex Ipv4 = new(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);
    private static readonly Regex Ipv6 = new(@"\b(?:[0-9a-f]{1,4}:){2,7}[0-9a-f]{1,4}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UrlQ = new(@"([?&])(token|key|code|access_token)=[^&\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string SanitizeReportText(string? text, bool stack = false)
    {
        if (text is null)
        {
            return string.Empty;
        }

        string output = Pem.Replace(text, "<redacted-secret>");
        output = GitHub.Replace(output, "<redacted-secret>");
        output = Bearer.Replace(output, "<redacted-secret>");
        output = Jwt.Replace(output, "<redacted-secret>");
        output = Aws.Replace(output, "<redacted-secret>");
        output = Api.Replace(output, "<redacted-secret>");
        output = Email.Replace(output, "<redacted-email>");
        output = WinHome.Replace(output, "<redacted-home>");
        output = UnixHome.Replace(output, "<redacted-home>/");
        output = Unc.Replace(output, "<redacted-unc>");
        output = Ipv4.Replace(output, "<redacted-ip>");
        output = Ipv6.Replace(output, "<redacted-ip>");
        output = UrlQ.Replace(output, "$1$2=<redacted-secret>");
        if (stack)
        {
            string[] lines = output.Split('\n');
            if (lines.Length > MaxStackLines)
            {
                output = string.Join('\n', lines, 0, MaxStackLines);
            }
        }

        return CapWholeLines(output);
    }

    private static string CapWholeLines(string text)
    {
        int byteCount = Encoding.UTF8.GetByteCount(text);
        if (byteCount <= MaxBodyBytes)
        {
            return text;
        }

        var kept = new List<string>();
        int size = 0;
        foreach (string line in text.Split('\n'))
        {
            int add = Encoding.UTF8.GetByteCount(line) + 1;
            if (size + add > MaxBodyBytes)
            {
                break;
            }

            kept.Add(line);
            size += add;
        }

        return string.Join('\n', kept);
    }
}
