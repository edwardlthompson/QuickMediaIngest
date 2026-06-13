#nullable enable
using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestVerificationTests
    {
        [Fact]
        public void IsPostImportVerifiedForDelete_StrictMode_RequiresMatchingSha256()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi_verify_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                string source = Path.Combine(dir, "source.jpg");
                string destMatch = Path.Combine(dir, "dest-match.jpg");
                string destMismatch = Path.Combine(dir, "dest-mismatch.jpg");
                File.WriteAllText(source, "same-content");
                File.WriteAllText(destMatch, "same-content");
                File.WriteAllText(destMismatch, "different-content");

                var item = new ImportItem
                {
                    FileName = "source.jpg",
                    SourcePath = source,
                    FileSize = new FileInfo(source).Length,
                };
                var options = new IngestOptions { VerificationMode = ImportVerificationMode.Strict };
                var logger = new Mock<ILogger>().Object;

                Assert.True(IngestVerification.IsPostImportVerifiedForDelete(item, destMatch, options, logger, out _));
                Assert.False(IngestVerification.IsPostImportVerifiedForDelete(item, destMismatch, options, logger, out string? note));
                Assert.Contains("SHA-256", note ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void IsPostImportVerifiedForDelete_FastMode_UsesSizeOnlyForLocalFiles()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi_verify_" + Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                string source = Path.Combine(dir, "source.jpg");
                string dest = Path.Combine(dir, "dest.jpg");
                File.WriteAllBytes(source, Encoding.UTF8.GetBytes("abc"));
                File.WriteAllBytes(dest, Encoding.UTF8.GetBytes("abcd"));

                var item = new ImportItem
                {
                    FileName = "source.jpg",
                    SourcePath = source,
                    FileSize = new FileInfo(source).Length,
                };
                var options = new IngestOptions { VerificationMode = ImportVerificationMode.Fast };
                var logger = new Mock<ILogger>().Object;

                Assert.False(IngestVerification.IsPostImportVerifiedForDelete(item, dest, options, logger, out _));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
