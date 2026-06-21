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
using System.Windows.Automation;
using System.Windows.Media.Animation;
using System.Threading;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using QuickMediaIngest.Localization;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {

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

        private void PillToggle_Toggled(object sender, RoutedEventArgs e)
        {
            // IsChecked == dark UI (matches App.CurrentIsDarkTheme)
            bool isDark = ThemeToggle?.IsChecked ?? false;

            // Persist and apply theme through the viewmodel so SaveConfig() is invoked
            if (DataContext is MainViewModel vm)
            {
                vm.IsDarkTheme = isDark;
            }
            else
            {
                App.ApplyTheme(!isDark);
            }
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F
                && Keyboard.Modifiers == ModifierKeys.Control)
            {
                try
                {
                    FilterKeywordTextBox?.Focus();
                    FilterKeywordTextBox?.SelectAll();
                    e.Handled = true;
                }
                catch
                {
                    // Ignore focus issues during startup or template changes.
                }
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
                System.Windows.MessageBox.Show(AppLocalizer.Format("Msg_OpenLogsFailed_Body", ex.Message), AppLocalizer.Get("Msg_Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
