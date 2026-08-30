using QuickMediaIngest;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ConfigPersistenceTests
    {
        [Fact]
        public void DeleteAfterImport_RoundTripsInAppConfig()
        {
            var config = new AppConfig
            {
                DeleteAfterImport = true,
                DeleteAfterImportPromptDismissed = true,
                ThumbnailPerformanceMode = "Ultra",
                ThumbnailSize = 200
            };

            string json = System.Text.Json.JsonSerializer.Serialize(config);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);

            Assert.NotNull(loaded);
            Assert.True(loaded!.DeleteAfterImport);
            Assert.True(loaded.DeleteAfterImportPromptDismissed);
            Assert.Equal("Ultra", loaded.ThumbnailPerformanceMode);
            Assert.Equal(200, loaded.ThumbnailSize);
        }

        [Fact]
        public void SaveCrashDetails_DefaultsOffAndRoundTrips()
        {
            var fresh = new AppConfig();
            Assert.False(fresh.SaveCrashDetails);

            var config = new AppConfig { SaveCrashDetails = true };
            string json = System.Text.Json.JsonSerializer.Serialize(config);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);
            Assert.NotNull(loaded);
            Assert.True(loaded!.SaveCrashDetails);
        }
    }
}
