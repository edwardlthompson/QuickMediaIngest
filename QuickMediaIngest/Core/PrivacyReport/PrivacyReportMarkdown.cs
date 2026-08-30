#nullable enable
namespace QuickMediaIngest.Core.PrivacyReport;

/// <summary>Sanitized markdown for a GitHub crash/bug/feature report.</summary>
public static class PrivacyReportMarkdown
{
    private static readonly HashSet<string> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        "crash", "bug", "feature"
    };

    public static string BuildReportMarkdown(
        string kind,
        string? description,
        string? stack = null,
        string? exceptionType = null,
        string? fingerprint = null,
        string? appVersion = null,
        string? osFamily = null)
    {
        string reportKind = Kinds.Contains(kind) ? kind.ToLowerInvariant() : "bug";
        string desc = PrivacyReportSanitize.SanitizeReportText(description);
        string stackSanitized = PrivacyReportSanitize.SanitizeReportText(stack, stack: true);
        var parts = new List<string>
        {
            "## What happened",
            string.IsNullOrEmpty(desc) ? "(no description)" : desc,
            "",
            "## Kind",
            reportKind
        };

        if (!string.IsNullOrEmpty(fingerprint))
        {
            parts.Add("");
            parts.Add("## Fingerprint");
            parts.Add($"`{PrivacyReportSanitize.SanitizeReportText(fingerprint)}`");
        }

        if (!string.IsNullOrEmpty(exceptionType))
        {
            parts.Add("");
            parts.Add("## Exception");
            parts.Add(PrivacyReportSanitize.SanitizeReportText(exceptionType));
        }

        if (!string.IsNullOrEmpty(appVersion))
        {
            parts.Add("");
            parts.Add("## App version");
            parts.Add(PrivacyReportSanitize.SanitizeReportText(appVersion));
        }

        if (!string.IsNullOrEmpty(osFamily))
        {
            parts.Add("");
            parts.Add("## OS family");
            parts.Add(PrivacyReportSanitize.SanitizeReportText(osFamily));
        }

        if (!string.IsNullOrEmpty(stackSanitized))
        {
            parts.Add("");
            parts.Add("## Stack");
            parts.Add("```");
            parts.Add(stackSanitized);
            parts.Add("```");
        }

        return string.Join('\n', parts).Trim() + "\n";
    }
}
