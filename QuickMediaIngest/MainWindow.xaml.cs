using QuickMediaIngest.ViewModels;
using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using DragEventArgs = System.Windows.DragEventArgs;
using Point = System.Windows.Point;
using System.Windows.Input;
using System.Windows.Media;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using Color = System.Windows.Media.Color;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) { }
        private void Window_StateChanged(object sender, EventArgs e) { }
        private void AddFtpOverlay_Loaded(object sender, RoutedEventArgs e) { }
        private void Window_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e) { }
        public void ApplyWindowStateFromViewModel() { }
        private bool _deleteAfterImportConfirmed = false;
        private readonly ILogger<MainWindow> _logger;
        private const string TokenDragFormat = "QuickMediaIngest.TokenPayload";
        private const string RibbonTileDragFormat = "QuickMediaIngest.RibbonTile";
        // private Point _tokenDragStartPoint;
        // private bool _isTokenDragInProgress;
        // private bool _ribbonLayoutRefreshQueued;
        // private Point _ribbonTileDragStartPoint;
        // private bool _isRibbonTileDragInProgress;
        // private Border? _activeRibbonDraggedTile;
        // private int _activeRibbonPreviewIndex = -1;

        public MainWindow(MainViewModel viewModel, ILogger<MainWindow> logger)
        {
            _logger = logger;
            InitializeComponent();
        }

        void DeleteAfterImportCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!_deleteAfterImportConfirmed)
            {
                var result = MessageBox.Show("Warning: This will permanently remove files from the source after successful copy.", "Delete After Import", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result != MessageBoxResult.OK)
                {
                    DeleteAfterImportCheckBox.IsChecked = false;
                    return;
                }
                _deleteAfterImportConfirmed = true;
            }
        }

        void HourIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Debounce handled in ViewModel, just update property
            if (DataContext is MainViewModel vm)
                vm.GroupingHours = (int)HourIntervalSlider.Value;
        }

        void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
                vm.SelectAllVisible();
        }

        void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QuickMediaIngest", "logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);
                Process.Start(new ProcessStartInfo("explorer.exe", logPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open logs folder.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = "https://github.com/edwardlthompson/QuickMediaIngest/issues";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open browser.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Token_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            if (!e.Data.GetDataPresent(TokenDragFormat) || DataContext is not MainViewModel vm)
            {
                return;
            }

            if (e.Data.GetData(TokenDragFormat) is not TokenDragPayload payload || string.IsNullOrEmpty(payload.Token))
            {
                return;
            }
            int insertIndex = GetTokenInsertIndex(vm, e.OriginalSource as DependencyObject);
            if (insertIndex < 0 || insertIndex > vm.SelectedTokens.Count)
                insertIndex = vm.SelectedTokens.Count;

            // Call ViewModel command to handle insertion/move
            var payloadType = Type.GetType("QuickMediaIngest.ViewModels.TokenInsertPayload, QuickMediaIngest");
            if (payloadType != null)
            {
                var insertPayload = Activator.CreateInstance(payloadType);
                payloadType.GetProperty("Token")?.SetValue(insertPayload, payload.Token);
                payloadType.GetProperty("Index")?.SetValue(insertPayload, insertIndex);
                payloadType.GetProperty("FromSelected")?.SetValue(insertPayload, payload.FromSelected);
                if (vm.InsertTokenCommand.CanExecute(insertPayload))
                    vm.InsertTokenCommand.Execute(insertPayload);
            }
        }

        private static int GetTokenInsertIndex(MainViewModel vm, DependencyObject? origin)
        {
            while (origin != null)
            {
                if (origin is FrameworkElement element && element.DataContext is TokenItem targetToken)
                {
                    int targetIndex = vm.SelectedTokens.IndexOf(targetToken);
                    if (targetIndex >= 0)
                    {
                        return targetIndex;
                    }
                }
                origin = VisualTreeHelper.GetParent(origin);
                    void DeleteAfterImportCheckBox_Checked(object sender, RoutedEventArgs e)
                    {
                        if (!_deleteAfterImportConfirmed)
                        {
                            var result = MessageBox.Show("Warning: This will permanently remove files from the source after successful copy.", "Delete After Import", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            if (result != MessageBoxResult.OK)
                            {
                                DeleteAfterImportCheckBox.IsChecked = false;
                                return;
                            }
                            _deleteAfterImportConfirmed = true;
                        }
                    }
            }
        }

        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.Dispatcher.BeginInvoke(new Action(textBox.SelectAll), DispatcherPriority.Input);
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.TextBox textBox)
            {
                return;
            }

            if (!textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is PasswordBox pb)
            {
                // Keep the viewmodel in sync with password box securely
                vm.FtpPass = pb.Password;
            }
        }

        private void ThemeToggle_Checked(object sender, RoutedEventArgs e)
        {
            // Checked == Dark mode
            App.ApplyTheme(false); // false => use dark (ApplyTheme param is useLightTheme)
        }

        private void ThemeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // Unchecked == Light mode
            App.ApplyTheme(true);
        }

        private void PillToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // Determine dark mode from the toggle
            bool isDark = ThemeToggle?.IsChecked ?? false;

            // Update application-level brushes immediately to avoid any flash of invisible text
            if (isDark)
            {
                System.Windows.Application.Current.Resources["MenuForegroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
                System.Windows.Application.Current.Resources["MenuBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            }
            else
            {
                System.Windows.Application.Current.Resources["MenuForegroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 26, 26));
                System.Windows.Application.Current.Resources["MenuBackgroundBrush"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
            }

            // Persist and apply theme through the viewmodel so SaveConfig() is invoked
            if (DataContext is MainViewModel vm)
            {
                vm.IsDarkTheme = isDark;
            }
            else
            {
                // Fallback: ensure the palette is applied
                App.ApplyTheme(!isDark);
            }
        }

        private void Settings_BrowseDestination(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainViewModel vm)
                {
                    string initial = vm.DestinationRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                    Process.Start(new ProcessStartInfo("explorer.exe", initial) { UseShellExecute = true });
                    System.Windows.MessageBox.Show("Folder picker is unavailable in this build. Explorer opened — navigate to the folder and paste its path into the Destination field.", "Select Folder", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch { }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            // Reuse existing explorer-fallback browse logic
            Settings_BrowseDestination(sender, e);
        }

        private void CloseSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ViewModels.MainViewModel vm)
                {
                    // Persist settings in the ViewModel
                    try { vm.SaveConfig(); } catch { }
                    vm.ShowSettingsDialog = false;
                }
            }
            catch { }

            // Hide overlay explicitly
            try { SettingsOverlay.Visibility = Visibility.Collapsed; } catch { }
        }

        // Show settings overlay from menu or code-behind
        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SettingsOverlay.Visibility = Visibility.Visible;
                SettingsOverlay.BringIntoView();
                // Focus the destination textbox so tabbing stays inside overlay
                try { DestPathDisplay?.Focus(); } catch { }
            }
            catch { }
        }

        private void NamingBubble_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tokens = new System.Collections.Generic.List<string>();
                foreach (var item in NamingBubblesItemsControl.Items)
                {
                    if (item is System.Windows.Controls.Primitives.ToggleButton tb && tb.IsChecked == true)
                    {
                        tokens.Add((tb.Content ?? "").ToString()!);
                    }
                }

                var parts = new System.Collections.Generic.List<string>();
                if (tokens.Contains("Date")) parts.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                if (tokens.Contains("Time")) parts.Add(DateTime.Now.ToString("HHmmss"));
                if (tokens.Contains("Shoot Name")) parts.Add("MyShoot");
                if (tokens.Contains("Sequence")) parts.Add("001");

                string preview = "Preview: ";
                preview += parts.Count > 0 ? string.Join("_", parts) + ".jpg" : "2026-03-29_001.jpg";

                try { NamingPreview.Text = preview; } catch { }
            }
            catch { }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (DataContext is ViewModels.MainViewModel vm && vm.ShowSettingsDialog)
                {
                    vm.ShowSettingsDialog = false;
                    e.Handled = true;
                }
            }
        }

        private void Settings_MoveTokenUp(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedTokens.Count > 0)
            {
                var lb = FindVisualChild<System.Windows.Controls.ListBox>(this, "");
                // Fallback: move first selected token up; if none selected, do nothing
                var token = vm.SelectedTokens.FirstOrDefault();
                if (token != null)
                {
                    int idx = vm.SelectedTokens.IndexOf(token);
                    if (idx > 0)
                    {
                        vm.SelectedTokens.Move(idx, idx - 1);
                        vm.UpdateNamingFromTokens();
                    }
                }
            }
        }

        private void Settings_MoveTokenDown(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedTokens.Count > 0)
            {
                var token = vm.SelectedTokens.FirstOrDefault();
                if (token != null)
                {
                    int idx = vm.SelectedTokens.IndexOf(token);
                    if (idx < vm.SelectedTokens.Count - 1)
                    {
                        vm.SelectedTokens.Move(idx, idx + 1);
                        vm.UpdateNamingFromTokens();
                    }
                }
            }
        }

        private void Settings_Save(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveConfig();
                vm.ShowSettingsDialog = false;
            }
        }

        private void Settings_Close(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.ShowSettingsDialog = false;
            }
        }

        private void OpenCrashLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logsDir = Path.Combine(appData, "QuickMediaIngest", "Logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
                Process.Start(new ProcessStartInfo("explorer.exe", logsDir) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open crash logs folder.");
                System.Windows.MessageBox.Show("Unable to open crash logs folder." + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static T? FindVisualChild<T>(DependencyObject depObj, string name) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                {
                    return t;
                }

                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private sealed record TokenDragPayload(string Token, bool FromSelected);
    }

    // Converter to invert boolean values (True -> False, False -> True)
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }
    }
    // Converter to show "Import" or "Importing..." based on IsImporting state
    public class BoolToImportingTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool isImporting && isImporting ? "Importing..." : "Import";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
