#nullable enable
using System.Collections.Generic;
using System.Windows;

namespace QuickMediaIngest
{
    public partial class ShortcutsHelpWindow : Window
    {
        public List<ShortcutRowVm> Rows { get; }

        public ShortcutsHelpWindow()
        {
            InitializeComponent();
            DataContext = this;
            Rows =
            [
                new ShortcutRowVm { Key = "Ctrl+I", Description = Localization.AppLocalizer.Get("Help_ShortcutImport") },
                new ShortcutRowVm { Key = "Ctrl+Q", Description = Localization.AppLocalizer.Get("Help_ShortcutQueue") },
                new ShortcutRowVm { Key = "Ctrl+P", Description = Localization.AppLocalizer.Get("Help_ShortcutPreflight") },
                new ShortcutRowVm { Key = "Ctrl+R", Description = Localization.AppLocalizer.Get("Help_ShortcutRefresh") },
                new ShortcutRowVm { Key = "Ctrl+A", Description = Localization.AppLocalizer.Get("Help_ShortcutSelectAll") },
                new ShortcutRowVm { Key = "Ctrl+F", Description = Localization.AppLocalizer.Get("Help_ShortcutFilter") },
                new ShortcutRowVm { Key = "Escape", Description = Localization.AppLocalizer.Get("Help_ShortcutEscape") },
                new ShortcutRowVm { Key = "F1", Description = Localization.AppLocalizer.Get("Help_ShortcutHelp") },
            ];
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class ShortcutRowVm
        {
            public string Key { get; init; } = string.Empty;
            public string Description { get; init; } = string.Empty;
        }
    }
}
