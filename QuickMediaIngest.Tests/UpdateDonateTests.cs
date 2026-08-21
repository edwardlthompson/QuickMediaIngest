#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class UpdateDonateTests
    {
        [Theory]
        [InlineData("QuickMediaIngest-1.3.27-x64-setup.msi", "1.3.27")]
        [InlineData("QuickMediaIngest-1.3.27-x64.exe", "1.3.27")]
        [InlineData("app-name-2.0.0-foss.apk", "2.0.0")]
        public void TryParse_ExtractsFirstProductVersion(string fileName, string expected)
        {
            Version? parsed = ReleaseAssetVersion.TryParse(fileName);
            Assert.NotNull(parsed);
            Assert.Equal(new Version(expected), parsed);
        }

        [Theory]
        [InlineData("QuickMediaIngest.exe")]
        [InlineData("QuickMediaIngest.msi")]
        [InlineData("QuickMediaIngest-Portable.zip")]
        [InlineData("")]
        [InlineData(null)]
        public void TryParse_UnversionedName_ReturnsNull(string? fileName)
        {
            Assert.Null(ReleaseAssetVersion.TryParse(fileName));
        }

        [Fact]
        public void IsNewerProductVersion_WhenRemoteGreater_IsTrue()
        {
            Assert.True(UpdateDonateState.IsNewerProductVersion(new Version(1, 3, 27), new Version(1, 3, 26)));
            Assert.False(UpdateDonateState.IsNewerProductVersion(new Version(1, 3, 26), new Version(1, 3, 26)));
            Assert.False(UpdateDonateState.IsNewerProductVersion(new Version(1, 3, 25), new Version(1, 3, 26)));
        }

        [Fact]
        public void ShouldCheckForUpdate_RespectsTwentyFourHourInterval()
        {
            var now = new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero);
            Assert.True(UpdateDonateState.ShouldCheckForUpdate(now, lastCheck: null));
            Assert.False(UpdateDonateState.ShouldCheckForUpdate(now, now.AddHours(-23)));
            Assert.True(UpdateDonateState.ShouldCheckForUpdate(now, now.AddHours(-24)));
            Assert.True(UpdateDonateState.ShouldCheckForUpdate(now, now.AddHours(-25)));
        }

        [Fact]
        public void ShouldPromptUpdate_DismissSilencesThatVersionOnly()
        {
            var current = new Version(1, 3, 26);
            var remote = new Version(1, 3, 27);
            Assert.True(UpdateDonateState.ShouldPromptUpdate(remote, current, dismissedProductVersion: null));
            Assert.False(UpdateDonateState.ShouldPromptUpdate(remote, current, "1.3.27"));
            Assert.True(UpdateDonateState.ShouldPromptUpdate(new Version(1, 3, 28), current, "1.3.27"));
            Assert.False(UpdateDonateState.ShouldPromptUpdate(current, current, dismissedProductVersion: null));
            Assert.False(UpdateDonateState.ShouldPromptUpdate(null, current, dismissedProductVersion: null));
        }

        [Fact]
        public void ShouldShowDonateNudge_OnlyOnVersionChange()
        {
            Assert.False(UpdateDonateState.ShouldShowDonateNudge("1.3.26", recordedInstalledVersion: null, donateNudgeSeenForVersion: null));
            Assert.False(UpdateDonateState.ShouldShowDonateNudge("1.3.26", "1.3.26", donateNudgeSeenForVersion: null));
            Assert.True(UpdateDonateState.ShouldShowDonateNudge("1.3.27", "1.3.26", donateNudgeSeenForVersion: null));
            Assert.True(UpdateDonateState.ShouldShowDonateNudge("1.3.27", "1.3.26", "1.3.26"));
            Assert.False(UpdateDonateState.ShouldShowDonateNudge("1.3.27", "1.3.26", "1.3.27"));
        }

        [Fact]
        public void TryRecordFirstInstalledVersion_RecordsOnceAndDoesNotNudge()
        {
            var prefs = new UpdateDonatePreferences();
            Assert.True(UpdateDonateState.TryRecordFirstInstalledVersion(prefs, "1.3.26"));
            Assert.Equal("1.3.26", prefs.RecordedInstalledVersion);
            Assert.False(UpdateDonateState.ShouldShowDonateNudge("1.3.26", prefs.RecordedInstalledVersion, prefs.DonateNudgeSeenForVersion));
            Assert.False(UpdateDonateState.TryRecordFirstInstalledVersion(prefs, "1.3.27"));
            Assert.Equal("1.3.26", prefs.RecordedInstalledVersion);
        }

        [Fact]
        public void FileStore_RoundTripsAndMigratesLegacyStamp()
        {
            string dir = Path.Combine(Path.GetTempPath(), "qmi-update-donate-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var stamp = new DateTimeOffset(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);
                File.WriteAllText(Path.Combine(dir, "last_update_check.txt"), stamp.ToString("o"));

                var store = new FileUpdateDonateStore(dir);
                UpdateDonatePreferences migrated = store.Load();
                Assert.Equal(stamp, migrated.LastUpdateCheckUtc);

                migrated.DismissedProductVersion = "1.3.27";
                migrated.RecordedInstalledVersion = "1.3.26";
                store.Save(migrated);

                UpdateDonatePreferences loaded = new FileUpdateDonateStore(dir).Load();
                Assert.Equal("1.3.27", loaded.DismissedProductVersion);
                Assert.Equal("1.3.26", loaded.RecordedInstalledVersion);
                Assert.Equal(stamp, loaded.LastUpdateCheckUtc);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* ignore */ }
            }
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenFilenameVersionNewer_ReturnsDownloadUrl()
        {
            string json = """
                {
                  "tag_name": "v-template-9.9.9",
                  "html_url": "https://example.com/releases/latest",
                  "assets": [
                    {
                      "name": "QuickMediaIngest-9.9.9-x64.exe",
                      "browser_download_url": "https://example.com/QuickMediaIngest-9.9.9-x64.exe"
                    },
                    {
                      "name": "QuickMediaIngest.exe",
                      "browser_download_url": "https://example.com/QuickMediaIngest.exe"
                    }
                  ]
                }
                """;

            using var http = new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));
            var store = new MemoryUpdateDonateStore();
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero));
            var svc = new UpdateService(http, new Mock<ILogger<UpdateService>>().Object, store, clock, new Version(1, 3, 26));

            UpdateCheckResult result = await svc.CheckForUpdateAsync(intervalHours: 24, force: true, packageType: "Portable");

            Assert.Equal("https://example.com/QuickMediaIngest-9.9.9-x64.exe", result.DownloadUrl);
            Assert.Equal("9.9.9", result.RemoteVersionTag);
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenUnversionedAssetsOnly_StaysSilent()
        {
            string json = """
                {
                  "tag_name": "v99.0.0",
                  "html_url": "https://example.com/releases/v99.0.0",
                  "assets": [
                    {
                      "name": "QuickMediaIngest.exe",
                      "browser_download_url": "https://example.com/QuickMediaIngest.exe"
                    }
                  ]
                }
                """;

            using var http = new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                }));
            var svc = new UpdateService(
                http,
                new Mock<ILogger<UpdateService>>().Object,
                new MemoryUpdateDonateStore(),
                new FixedClock(DateTimeOffset.UtcNow),
                new Version(1, 3, 26));

            UpdateCheckResult result = await svc.CheckForUpdateAsync(force: true, packageType: "Portable");
            Assert.Null(result.DownloadUrl);
            Assert.Null(result.RemoteVersionTag);
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenInsideDailyInterval_SkipsFetch()
        {
            int calls = 0;
            using var http = new HttpClient(new StubHandler(_ =>
            {
                calls++;
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("{}") };
            }));
            var now = new DateTimeOffset(2026, 8, 20, 22, 0, 0, TimeSpan.Zero);
            var store = new MemoryUpdateDonateStore
            {
                Preferences = { LastUpdateCheckUtc = now.AddHours(-1) }
            };
            var svc = new UpdateService(http, new Mock<ILogger<UpdateService>>().Object, store, new FixedClock(now), new Version(1, 3, 26));

            UpdateCheckResult result = await svc.CheckForUpdateAsync(intervalHours: 24, force: false);
            Assert.Equal(0, calls);
            Assert.Null(result.DownloadUrl);
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenDismissed_StaysSilent()
        {
            string json = """
                {
                  "html_url": "https://example.com/releases/latest",
                  "assets": [
                    {
                      "name": "QuickMediaIngest-9.9.9-x64.exe",
                      "browser_download_url": "https://example.com/QuickMediaIngest-9.9.9-x64.exe"
                    }
                  ]
                }
                """;
            using var http = new HttpClient(new StubHandler(_ =>
                new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(json) }));
            var store = new MemoryUpdateDonateStore
            {
                Preferences = { DismissedProductVersion = "9.9.9" }
            };
            var svc = new UpdateService(http, new Mock<ILogger<UpdateService>>().Object, store, new FixedClock(DateTimeOffset.UtcNow), new Version(1, 3, 26));

            UpdateCheckResult result = await svc.CheckForUpdateAsync(force: false);
            Assert.Null(result.DownloadUrl);
        }

        [Fact]
        public async Task CheckForUpdateAsync_WhenHttpFails_ReturnsDefaultWithoutThrowing()
        {
            using var http = new HttpClient(new StubHandler(_ => throw new HttpRequestException("network down")));
            var svc = new UpdateService(
                http,
                new Mock<ILogger<UpdateService>>().Object,
                new MemoryUpdateDonateStore(),
                new FixedClock(DateTimeOffset.UtcNow),
                new Version(1, 3, 26));

            UpdateCheckResult result = await svc.CheckForUpdateAsync(force: true);
            Assert.Null(result.DownloadUrl);
            Assert.Null(result.RemoteVersionTag);
        }

        private sealed class MemoryUpdateDonateStore : IUpdateDonateStore
        {
            public UpdateDonatePreferences Preferences { get; } = new();

            public UpdateDonatePreferences Load() => Preferences;

            public void Save(UpdateDonatePreferences preferences)
            {
                Preferences.LastUpdateCheckUtc = preferences.LastUpdateCheckUtc;
                Preferences.DismissedProductVersion = preferences.DismissedProductVersion;
                Preferences.RecordedInstalledVersion = preferences.RecordedInstalledVersion;
                Preferences.DonateNudgeSeenForVersion = preferences.DonateNudgeSeenForVersion;
            }
        }

        private sealed class FixedClock : ISystemClock
        {
            public FixedClock(DateTimeOffset utcNow) => UtcNow = utcNow;

            public DateTimeOffset UtcNow { get; }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                Task.FromResult(_responder(request));
        }
    }
}
