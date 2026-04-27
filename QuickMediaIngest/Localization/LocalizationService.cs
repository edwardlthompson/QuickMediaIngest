#nullable enable
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace QuickMediaIngest.Localization
{
    /// <summary>
    /// Applies UI culture from app settings or leaves Windows default.
    /// Changing language in settings requires restarting the app to reload all localized strings.
    /// </summary>
    public static class LocalizationService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickMediaIngest",
            "config.json");

        /// <summary>
        /// Reads optional <c>UiLanguage</c> from config and applies before UI is shown.
        /// Empty or missing = use the Windows display language (<see cref="CultureInfo.CurrentUICulture"/>).
        /// </summary>
        public static void ApplyCultureFromConfigFileEarly()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    ApplyCultureTag(null);
                    return;
                }

                using var doc = JsonDocument.Parse(File.ReadAllText(ConfigPath));
                // System.Text.Json serializes as camelCase by default (uiLanguage).
                if (doc.RootElement.TryGetProperty("uiLanguage", out JsonElement el) && el.ValueKind == JsonValueKind.String)
                {
                    ApplyCultureTag(el.GetString());
                }
                else
                {
                    ApplyCultureTag(null);
                }
            }
            catch
            {
                ApplyCultureTag(null);
            }
        }

        /// <summary>
        /// <paramref name="cultureTag"/>: null/empty/"system" use Windows UI culture; otherwise e.g. en, fr-FR.
        /// </summary>
        public static void ApplyCultureTag(string? cultureTag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cultureTag)
                    || string.Equals(cultureTag.Trim(), "system", StringComparison.OrdinalIgnoreCase))
                {
                    CultureInfo.DefaultThreadCurrentUICulture = null;
                    CultureInfo.DefaultThreadCurrentCulture = null;
                    return;
                }

                var culture = CultureInfo.GetCultureInfo(cultureTag.Trim());
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
            }
            catch
            {
                // Fall back without throwing during startup.
            }
        }
    }
}
