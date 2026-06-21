#nullable enable
using System;
using System.IO;
using System.Text.Json;
using QuickMediaIngest;
using Xunit;

namespace QuickMediaIngest.Tests
{
    /// <summary>Automates BUILD_PLAN settings persistence checks (config.json round-trip).</summary>
    public class ConfigFilePersistenceTests
    {
        [Fact]
        public void DeleteAfterImportAndThumbnailSize_SurviveConfigFileRoundTrip()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "qmi-config-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string configPath = Path.Combine(tempDir, "config.json");

            try
            {
                var saved = new AppConfig
                {
                    DeleteAfterImport = true,
                    DeleteAfterImportPromptDismissed = true,
                    ThumbnailPerformanceMode = "Ultra",
                    ThumbnailSize = 200,
                    FtpHost = "10.0.0.23",
                    FtpPort = 2221,
                    FtpRemoteFolder = "/DCIM"
                };

                File.WriteAllText(configPath, JsonSerializer.Serialize(saved));

                var loaded = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                Assert.NotNull(loaded);
                Assert.True(loaded!.DeleteAfterImport);
                Assert.True(loaded.DeleteAfterImportPromptDismissed);
                Assert.Equal(200, loaded.ThumbnailSize);
                Assert.Equal("Ultra", loaded.ThumbnailPerformanceMode);
                string json = File.ReadAllText(configPath);
                Assert.Contains("deleteAfterImport", json, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("thumbnailSize", json, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void SimulatedRestart_ReloadsSameConfigValues()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "qmi-config-smoke-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            string configPath = Path.Combine(tempDir, "config.json");

            try
            {
                var sessionOne = new AppConfig
                {
                    DeleteAfterImport = true,
                    DeleteAfterImportPromptDismissed = true,
                    ThumbnailSize = 200
                };
                File.WriteAllText(configPath, JsonSerializer.Serialize(sessionOne));

                // Simulated app restart: read config.json from disk again.
                var sessionTwo = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(configPath));
                Assert.NotNull(sessionTwo);
                Assert.True(sessionTwo!.DeleteAfterImport);
                Assert.True(sessionTwo.DeleteAfterImportPromptDismissed);
                Assert.Equal(200, sessionTwo.ThumbnailSize);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures on shared temp paths.
            }
        }
    }
}
