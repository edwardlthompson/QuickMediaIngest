#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using QuickMediaIngest.Core.Models;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class ImportReportSanitizationTests
    {
        [Fact]
        public void ExportImportReportArtifact_SanitizesUserPathsInReport()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"report-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                MainViewModel vm = MainViewModelProofTests.CreateViewModel();
                vm.DestinationRoot = tempDir;
                vm.FailedImportRecords.Add(new FailedImportRecord
                {
                    FileName = @"C:\Users\SecretUser\Pictures\photo.jpg",
                    ErrorMessage = @"Failed to copy from C:\Users\SecretUser\Documents\secret.key"
                });

                vm.ExportImportReportArtifact(TimeSpan.FromSeconds(5), new List<ItemGroup>());

                string reportDir = Path.Combine(tempDir, "_ImportReports");
                Assert.True(Directory.Exists(reportDir));
                string[] txtFiles = Directory.GetFiles(reportDir, "*.txt");
                Assert.NotEmpty(txtFiles);

                string content = File.ReadAllText(txtFiles[0]);
                Assert.DoesNotContain("SecretUser", content);
                Assert.Contains("<redacted-home>", content);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
    }
}
