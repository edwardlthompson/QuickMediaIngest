using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickMediaIngest.Core
{
    public class UpdateService
    {
        private const string RepoOwner = "edwardlthompson";
        private const string RepoName = "QuickMediaIngest";
        private readonly string _cacheFile;

        public UpdateService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "QuickMediaIngest");
            Directory.CreateDirectory(appFolder);
            _cacheFile = Path.Combine(appFolder, "last_update_check.txt");
        }

        public async Task<string?> CheckForUpdateAsync()
        {
            if (!ShouldCheck()) return null;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("QuickMediaIngest-Updater");
                
                var response = await client.GetStringAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
                var doc = JsonDocument.Parse(response);
                
                string remoteTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                string downloadUrl = doc.RootElement.GetProperty("html_url").GetString() ?? "";

                SaveLastCheck();

                var localVersion = typeof(UpdateService).Assembly.GetName().Version;
                if (localVersion == null) return null;

                // Strip 'v' prefix if present: e.g., "v1.2.3" -> "1.2.3"
                string versionText = remoteTag.TrimStart('v');

                if (Version.TryParse(versionText, out var remoteVersion))
                {
                    if (remoteVersion > localVersion)
                    {
                        return downloadUrl;
                    }
                }
            }
            catch (Exception)
            {
                // Suppress updates errors due to rate limiting or offline statuses
            }

            return null;
        }

        private bool ShouldCheck()
        {
            if (!File.Exists(_cacheFile)) return true;
            
            try
            {
                string text = File.ReadAllText(_cacheFile);
                if (DateTime.TryParse(text, out var lastCheck))
                {
                    return (DateTime.Now - lastCheck).TotalHours >= 24;
                }
            }
            catch { }
            
            return true;
        }

        private void SaveLastCheck()
        {
            try
            {
                File.WriteAllText(_cacheFile, DateTime.Now.ToString("o"));
            }
            catch { }
        }
    }
}
