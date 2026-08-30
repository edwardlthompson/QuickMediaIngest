#nullable enable
using System.Globalization;
using QuickMediaIngest.Localization;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DynamicLanguageSwitchTests
    {
        [Fact]
        public void SetCulture_ChangesCurrentUICultureAndRaisesEvent()
        {
            bool eventFired = false;
            AppLocalizer.CultureChanged += (s, e) => eventFired = true;

            try
            {
                AppLocalizer.SetCulture("fr");
                Assert.True(eventFired);
                Assert.Equal("fr", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            }
            finally
            {
                AppLocalizer.SetCulture("en");
            }
        }
    }
}
