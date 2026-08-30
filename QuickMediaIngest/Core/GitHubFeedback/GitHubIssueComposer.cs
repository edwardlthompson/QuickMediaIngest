#nullable enable
using QuickMediaIngest.Core.PrivacyReport;

namespace QuickMediaIngest.Core.GitHubFeedback;

/// <summary>Compose GitHub issue-form URLs. OWNER/REPO never hits the network.</summary>
public static class GitHubIssueComposer
{
    public const string DefaultOwnerRepo = "edwardlthompson/QuickMediaIngest";
    private const int SmallBodyLimit = 1500;

    public static bool IsPlaceholderRepo(string? ownerRepo)
    {
        if (string.IsNullOrWhiteSpace(ownerRepo))
        {
            return true;
        }

        string trimmed = ownerRepo.Trim();
        return trimmed.Equals("OWNER/REPO", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("acme/app", StringComparison.OrdinalIgnoreCase)
               || !trimmed.Contains('/', StringComparison.Ordinal);
    }

    public static string BuildCrashTitle(string? fingerprint, string? exceptionType)
    {
        string fp = PrivacyReportSanitize.SanitizeReportText(fingerprint);
        string kind = PrivacyReportSanitize.SanitizeReportText(exceptionType);
        if (string.IsNullOrEmpty(kind))
        {
            kind = "Error";
        }

        return $"[crash] {fp} {kind}".Trim();
    }

    public static GitHubIssueLink Compose(
        string kind,
        string? description,
        string? stack = null,
        string? exceptionType = null,
        string? fingerprint = null,
        string? appVersion = null,
        string ownerRepo = DefaultOwnerRepo)
    {
        string repo = IsPlaceholderRepo(ownerRepo) ? DefaultOwnerRepo : ownerRepo.Trim();
        string body = PrivacyReportMarkdown.BuildReportMarkdown(
            kind, description, stack, exceptionType, fingerprint, appVersion, osFamily: "Windows");
        string title = kind.Equals("crash", StringComparison.OrdinalIgnoreCase)
            ? BuildCrashTitle(fingerprint, exceptionType)
            : PrivacyReportSanitize.SanitizeReportText(description);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = kind.Equals("feature", StringComparison.OrdinalIgnoreCase) ? "Feature request" : "Bug report";
        }

        string template = kind.Equals("feature", StringComparison.OrdinalIgnoreCase) ? "feature.yml" : "bug.yml";
        string baseUrl = $"https://github.com/{repo}/issues/new";
        if (body.Length <= SmallBodyLimit)
        {
            string url = $"{baseUrl}?template={Uri.EscapeDataString(template)}&title={Uri.EscapeDataString(title)}&body={Uri.EscapeDataString(body)}";
            return new GitHubIssueLink(EnsureHttps(url), useClipboard: false, body);
        }

        return new GitHubIssueLink(EnsureHttps(baseUrl), useClipboard: true, body);
    }

    public static IReadOnlyList<string> SearchDuplicatesFailSoft() => Array.Empty<string>();

    private static string EnsureHttps(string url)
    {
        if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("GitHub links must be https.");
        }

        return url;
    }
}

public readonly record struct GitHubIssueLink(string Url, bool UseClipboard, string SanitizedBody);
