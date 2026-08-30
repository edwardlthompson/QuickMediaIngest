#nullable enable
using System.Linq;
using QuickMediaIngest;
using Xunit;

namespace QuickMediaIngest.Tests
{
    [Collection("Wpf")]
    public class ShortcutsHelpRowsTests
    {
        [Fact]
        public void ShortcutsHelpWindow_RowsIncludeFeedbackAndEject()
        {
            WpfTestHost.EnsureInitialized();
            WpfTestHost.RunOnUiThread(() =>
            {
                var window = new ShortcutsHelpWindow();
                Assert.Contains(window.Rows, r => r.Key == "Ctrl+E");
                Assert.Contains(window.Rows, r => r.Key == "F9");
            });
        }
    }
}
