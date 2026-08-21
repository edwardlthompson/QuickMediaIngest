#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Core.Logging;

namespace QuickMediaIngest.Core
{
    public sealed class FileUpdateDonateStore : IUpdateDonateStore
    {
        public const string FileName = "update-donate.json";
        private const string LegacyStampFile = "last_update_check.txt";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _folder;
        private readonly string _jsonPath;
        private readonly ILogger<FileUpdateDonateStore>? _logger;

        public FileUpdateDonateStore(string? appFolder = null, ILogger<FileUpdateDonateStore>? logger = null)
        {
            _folder = string.IsNullOrWhiteSpace(appFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest")
                : appFolder;
            Directory.CreateDirectory(_folder);
            _jsonPath = Path.Combine(_folder, FileName);
            _logger = logger;
        }

        public UpdateDonatePreferences Load()
        {
            try
            {
                if (File.Exists(_jsonPath))
                {
                    string json = File.ReadAllText(_jsonPath);
                    UpdateDonatePreferences? loaded = JsonSerializer.Deserialize<UpdateDonatePreferences>(json, JsonOptions);
                    return loaded ?? new UpdateDonatePreferences();
                }

                return MigrateLegacyStamp();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Could not read donate/update prefs at {Path}.", LogPathSanitizer.AppData(_jsonPath));
                return new UpdateDonatePreferences();
            }
        }

        public void Save(UpdateDonatePreferences preferences)
        {
            try
            {
                Directory.CreateDirectory(_folder);
                string json = JsonSerializer.Serialize(preferences, JsonOptions);
                File.WriteAllText(_jsonPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Could not write donate/update prefs at {Path}.", LogPathSanitizer.AppData(_jsonPath));
            }
        }

        private UpdateDonatePreferences MigrateLegacyStamp()
        {
            var prefs = new UpdateDonatePreferences();
            string legacy = Path.Combine(_folder, LegacyStampFile);
            if (!File.Exists(legacy))
            {
                return prefs;
            }

            try
            {
                string text = File.ReadAllText(legacy).Trim();
                if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset stamp)
                    || DateTimeOffset.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out stamp))
                {
                    prefs.LastUpdateCheckUtc = stamp.ToUniversalTime();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Could not migrate last update check stamp.");
            }

            return prefs;
        }
    }
}
