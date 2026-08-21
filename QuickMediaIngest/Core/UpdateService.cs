#nullable enable
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public class UpdateService : IUpdateService
    {
        private const string LatestReleaseUrl = "https://api.github.com/repos/edwardlthompson/QuickMediaIngest/releases/latest";
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);

        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService> _logger;
        private readonly IUpdateDonateStore _store;
        private readonly ISystemClock _clock;
        private readonly Version _currentVersion;

        public UpdateService(
            HttpClient httpClient,
            ILogger<UpdateService> logger,
            IUpdateDonateStore store,
            ISystemClock clock,
            Version? currentVersion = null)
        {
            _httpClient = httpClient;
            _logger = logger;
            _store = store;
            _clock = clock;
            _currentVersion = ReleaseAssetVersion.ToProduct(
                currentVersion ?? typeof(UpdateService).Assembly.GetName().Version ?? new Version(1, 0, 0));
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(int intervalHours = 24, bool force = false, string packageType = "Portable")
        {
            UpdateDonatePreferences prefs = _store.Load();
            DateTimeOffset now = _clock.UtcNow;

            if (!force)
            {
                if (intervalHours < 0)
                {
                    return default;
                }

                if (!UpdateDonateState.ShouldCheckForUpdate(now, prefs.LastUpdateCheckUtc))
                {
                    return default;
                }
            }

            try
            {
                _logger.LogInformation("Checking for updates. Force={Force}, PackageType={PackageType}", force, packageType);
                using var cts = new CancellationTokenSource(RequestTimeout);
                using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
                using HttpResponseMessage response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string body = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                prefs.LastUpdateCheckUtc = now;
                _store.Save(prefs);

                using JsonDocument doc = JsonDocument.Parse(body);
                if (!TrySelectNewestMatchingAsset(doc.RootElement, packageType, out Version remote, out string downloadUrl))
                {
                    return default;
                }

                bool prompt = force
                    ? UpdateDonateState.IsNewerProductVersion(remote, _currentVersion)
                    : UpdateDonateState.ShouldPromptUpdate(remote, _currentVersion, prefs.DismissedProductVersion);
                if (!prompt)
                {
                    return default;
                }

                if (string.IsNullOrWhiteSpace(downloadUrl)
                    && doc.RootElement.TryGetProperty("html_url", out JsonElement html)
                    && html.GetString() is string page
                    && !string.IsNullOrWhiteSpace(page))
                {
                    downloadUrl = page;
                }

                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    return default;
                }

                string product = remote.ToString(3);
                _logger.LogInformation("Update available. LocalVersion={LocalVersion}, ProductVersion={ProductVersion}", _currentVersion, product);
                return new UpdateCheckResult(downloadUrl, product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update check failed.");
                return default;
            }
        }

        private static bool TrySelectNewestMatchingAsset(JsonElement root, string packageType, out Version remote, out string downloadUrl)
        {
            remote = new Version(0, 0, 0);
            downloadUrl = string.Empty;
            Version? best = null;
            string bestUrl = string.Empty;

            if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string name = asset.TryGetProperty("name", out JsonElement nameEl) ? nameEl.GetString() ?? "" : "";
                Version? parsed = ReleaseAssetVersion.TryParse(name);
                if (parsed == null || !ReleaseAssetVersion.MatchesPackage(name, packageType))
                {
                    continue;
                }

                if (best != null && !ReleaseAssetVersion.IsNewer(parsed, best) && !ReleaseAssetVersion.SameProduct(parsed, best))
                {
                    continue;
                }

                if (best != null && ReleaseAssetVersion.SameProduct(parsed, best) && !string.IsNullOrWhiteSpace(bestUrl))
                {
                    continue;
                }

                best = parsed;
                bestUrl = asset.TryGetProperty("browser_download_url", out JsonElement urlEl) ? urlEl.GetString() ?? "" : "";
            }

            if (best == null)
            {
                return false;
            }

            remote = best;
            downloadUrl = bestUrl;
            return true;
        }
    }
}
