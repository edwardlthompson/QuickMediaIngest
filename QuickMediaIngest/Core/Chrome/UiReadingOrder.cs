#nullable enable
using System;
using System.Globalization;

namespace QuickMediaIngest.Core.Chrome
{
    /// <summary>RTL when the UI culture is right-to-left; ja/de/es/fr/en stay LTR.</summary>
    public static class UiReadingOrder
    {
        public static bool IsRightToLeft(string? cultureTag)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cultureTag)
                    || string.Equals(cultureTag.Trim(), "system", StringComparison.OrdinalIgnoreCase))
                {
                    return CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                }

                return CultureInfo.GetCultureInfo(cultureTag.Trim()).TextInfo.IsRightToLeft;
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        }
    }
}
