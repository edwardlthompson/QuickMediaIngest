#nullable enable
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickMediaIngest.Core.PrivacyReport;

/// <summary>Stable crash fingerprint from a sanitized stack.</summary>
public static class PrivacyReportFingerprint
{
    private static readonly Regex TypePrefix = new(@"^([A-Za-z][A-Za-z0-9_.$]+)", RegexOptions.Compiled);

    public static string FingerprintCrash(string? stack, string? exceptionType = null)
    {
        string cleaned = PrivacyReportSanitize.SanitizeReportText(stack, stack: true);
        var frames = cleaned
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(static line => line.Length > 0)
            .Take(12)
            .ToArray();
        string kind = (exceptionType ?? GuessType(cleaned) ?? "Error").Trim();
        string payload = kind + "\n" + string.Join('\n', frames);
        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(digest)[..12].ToLowerInvariant();
    }

    private static string? GuessType(string stack)
    {
        string first = stack.Split('\n')[0].Trim();
        if (first.Length == 0)
        {
            return "Error";
        }

        Match match = TypePrefix.Match(first);
        return match.Success ? match.Groups[1].Value : "Error";
    }
}
