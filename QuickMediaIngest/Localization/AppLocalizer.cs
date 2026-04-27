#nullable enable
using System.Globalization;
using System.Resources;
using System.Reflection;

namespace QuickMediaIngest.Localization
{
    /// <summary>
    /// Central access to localized strings (.resx). Use <see cref="Loc"/> markup extension in XAML.
    /// All new user-visible text should go through here for translation support.
    /// </summary>
    public static class AppLocalizer
    {
        private static readonly ResourceManager ResourceManager = new(
            "QuickMediaIngest.Localization.Strings",
            Assembly.GetExecutingAssembly());

        /// <summary>Gets the localized string for <paramref name="key"/> using <see cref="CultureInfo.CurrentUICulture"/>.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            try
            {
                return ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
                    ?? ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en"))
                    ?? key;
            }
            catch
            {
                return key;
            }
        }

        public static string Format(string key, params object?[] args) =>
            string.Format(CultureInfo.CurrentUICulture, Get(key), args);
    }
}
