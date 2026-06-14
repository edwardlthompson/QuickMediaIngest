using QuickMediaIngest;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class SavedFtpSourceEntryTests
    {
        [Fact]
        public void SavedFtpSourceEntry_RoundTripsInAppConfig()
        {
            var config = new AppConfig
            {
                FtpHost = "10.0.0.23",
                FtpPort = 2221,
                FtpUser = "android",
                SavedFtpSources =
                {
                    new SavedFtpSourceEntry
                    {
                        Host = "10.0.0.23",
                        Port = 2221,
                        User = "android",
                        RemoteFolder = "/DCIM"
                    }
                }
            };

            string json = System.Text.Json.JsonSerializer.Serialize(config);
            var loaded = System.Text.Json.JsonSerializer.Deserialize<AppConfig>(json);

            Assert.NotNull(loaded);
            Assert.Equal("10.0.0.23", loaded!.FtpHost);
            Assert.Single(loaded.SavedFtpSources);
            Assert.Equal(2221, loaded.SavedFtpSources[0].Port);
            Assert.Equal("/DCIM", loaded.SavedFtpSources[0].RemoteFolder);
        }
    }
}
