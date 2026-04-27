#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest.Core
{
    public class UpdateService : IUpdateService
    {
        private const string RepoOwner = "edwardlthompson";
        private const string RepoName = "QuickMediaIngest";
        private readonly string _cacheFile;
        private readonly HttpClient _httpClient;
        private readonly ILogger<UpdateService> _logger;

        public UpdateService(HttpClient httpClient, ILogger<UpdateService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "QuickMediaIngest");
            Directory.CreateDirectory(appFolder);
            _cacheFile = Path.Combine(appFolder, "last_update_check.txt");
        }

        public async Task<UpdateCheckResult> CheckForUpdateAsync(int intervalHours = 24, bool force = false, string packageType = "Portable")
        {
            if (!force && !ShouldCheck(intervalHours))
            {
                return default;
            }

            try
            {
                _logger.LogInformation("Checking for updates. Force={Force}, IntervalHours={IntervalHours}, PackageType={PackageType}", force, intervalHours, packageType);
                var response = await _httpClient.GetStringAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
                var doc = JsonDocument.Parse(response);
                
                string remoteTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                
                // Find the preferred asset based on package type selection.
                // Portable should prioritize the standalone .exe, Installer should use .msi.
                string targetName = packageType == "Installer" ? "QuickMediaIngest.msi" : "QuickMediaIngest.exe";
                string fallbackExt = packageType == "Installer" ? ".msi" : ".exe";
                string downloadUrl = string.Empty;
                if (doc.RootElement.TryGetProperty("assets", out var assets))
                {
                    // First pass: exact asset match.
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (string.Equals(name, targetName, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                            break;
                        }
                    }

                    // Second pass: extension fallback for non-standard asset names.
                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        foreach (var asset in assets.EnumerateArray())
                        {
                            string name = asset.GetProperty("name").GetString() ?? "";
                            if (name.EndsWith(fallbackExt, StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? "";
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl))
                {
                    downloadUrl = doc.RootElement.GetProperty("html_url").GetString() ?? "";
                }

                SaveLastCheck();

                var localVersion = typeof(UpdateService).Assembly.GetName().Version;
                if (localVersion == null)
                {
                    return default;
                }

                string versionText = remoteTag.TrimStart('v');

                if (Version.TryParse(versionText, out var remoteVersion))
                {
                    if (remoteVersion > localVersion)
                    {
                        _logger.LogInformation("Update available. LocalVersion={LocalVersion}, RemoteVersion={RemoteVersion}", localVersion, remoteVersion);
                        return new UpdateCheckResult(downloadUrl, remoteTag);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update check failed.");
            }

            return default;
        }

        private bool ShouldCheck(int intervalHours)
        {
            if (intervalHours < 0) return false; // -1 means Off / Manual Check Only
            if (intervalHours == 0) return true; // 0 means always check on startup
            if (!File.Exists(_cacheFile)) return true;
            
            try
            {
                string text = File.ReadAllText(_cacheFile);
                if (DateTime.TryParse(text, out var lastCheck))
                {
                    return (DateTime.Now - lastCheck).TotalHours >= intervalHours;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read last update check timestamp; will check again.");
            }
            
            return true;
        }

        private void SaveLastCheck()
        {
            try
            {
                File.WriteAllText(_cacheFile, DateTime.Now.ToString("o"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write last update check cache: {Path}.", _cacheFile);
            }
        }
    }
}
