#nullable enable
using System;
using QuickMediaIngest.Core.PrivacyReport;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class PrivacyReportTests
    {
        [Fact]
        public void Sanitize_Null_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, PrivacyReportSanitize.SanitizeReportText(null));
        }

        [Fact]
        public void Sanitize_RedactsSecretsAndHomePaths()
        {
            const string jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIn0.signaturexx";
            string raw = "C:\\Users\\Ada\\secret.env ghp_abcdefghijklmnopqrstuvwxyz012345 " + jwt + " AKIAIOSFODNN7EXAMPLE";
            string cleaned = PrivacyReportSanitize.SanitizeReportText(raw);
            Assert.DoesNotContain("Ada", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("ghp_", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("eyJ", cleaned, StringComparison.Ordinal);
            Assert.DoesNotContain("AKIA", cleaned, StringComparison.Ordinal);
            Assert.Contains("<redacted-home>", cleaned, StringComparison.Ordinal);
            Assert.Contains("<redacted-secret>", cleaned, StringComparison.Ordinal);
        }

        [Fact]
        public void Fingerprint_StableWhenOnlyUsernameChanges()
        {
            string a = PrivacyReportFingerprint.FingerprintCrash(
                "System.Exception\n   at C:\\Users\\Ada\\app\\Main.cs:line 10",
                "System.Exception");
            string b = PrivacyReportFingerprint.FingerprintCrash(
                "System.Exception\n   at C:\\Users\\Bob\\app\\Main.cs:line 10",
                "System.Exception");
            Assert.Equal(12, a.Length);
            Assert.Equal(a, b);
        }

        [Fact]
        public void Markdown_IncludesSanitizedKindAndOmitsSecrets()
        {
            string md = PrivacyReportMarkdown.BuildReportMarkdown(
                "bug",
                "boom ghp_abcdefghijklmnopqrstuvwxyz012345",
                stack: "C:\\Users\\Ada\\app.cs",
                fingerprint: "abc123def456");
            Assert.Contains("## Kind", md, StringComparison.Ordinal);
            Assert.Contains("bug", md, StringComparison.Ordinal);
            Assert.DoesNotContain("ghp_", md, StringComparison.Ordinal);
            Assert.DoesNotContain("Ada", md, StringComparison.Ordinal);
        }
    }
}
