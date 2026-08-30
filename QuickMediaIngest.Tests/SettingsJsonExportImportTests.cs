#nullable enable
using System;
using System.IO;
using System.Text.Json;
using QuickMediaIngest;
using QuickMediaIngest.ViewModels;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class SettingsJsonExportImportTests
    {
        [Fact]
        public void ExportSettingsJson_ProducesValidJsonWithoutFtpPassword()
        {
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();
            vm.FtpHost = "192.168.1.50";
            vm.FtpUser = "testuser";
            vm.FtpPass = "SuperSecretPassword";
            vm.DestinationRoot = @"C:\Photos";

            string json = vm.ExportSettingsJson();

            Assert.NotNull(json);
            Assert.Contains("\"SchemaVersion\": 1", json);
            Assert.Contains("\"FtpHost\": \"192.168.1.50\"", json);
            Assert.DoesNotContain("SuperSecretPassword", json);
            Assert.Contains("\"FtpPass\": \"\"", json);

            using var doc = JsonDocument.Parse(json);
            Assert.Equal(1, doc.RootElement.GetProperty("SchemaVersion").GetInt32());
        }

        [Fact]
        public void ImportSettingsJson_ValidJson_AppliesPreferences()
        {
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            var config = new AppConfig
            {
                SchemaVersion = 1,
                DestinationRoot = @"D:\ImportedMedia",
                DuplicatePolicy = "Skip",
                VerificationMode = "Strict",
                EmbedKeywordsOnImport = true,
                StripGpsAndPiiOnEmbed = true,
            };

            string json = JsonSerializer.Serialize(config);
            bool imported = vm.ImportSettingsJson(json);

            Assert.True(imported);
            Assert.Equal(@"D:\ImportedMedia", vm.DestinationRoot);
            Assert.Equal("Skip", vm.DuplicatePolicy);
            Assert.Equal("Strict", vm.VerificationMode);
            Assert.True(vm.EmbedKeywordsOnImport);
            Assert.True(vm.StripGpsAndPiiOnEmbed);
        }

        [Fact]
        public void ImportSettingsJson_LegacySchema_MigratesToSchemaVersion1()
        {
            MainViewModel vm = MainViewModelProofTests.CreateViewModel();

            string legacyJson = "{\"DestinationRoot\": \"E:\\\\Legacy\", \"DuplicatePolicy\": \"Suffix\"}";
            bool imported = vm.ImportSettingsJson(legacyJson);

            Assert.True(imported);
            Assert.Equal(@"E:\Legacy", vm.DestinationRoot);
        }
    }
}
