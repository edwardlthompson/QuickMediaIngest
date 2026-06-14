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
using QuickMediaIngest.Services;
using QuickMediaIngest.ViewModels;

namespace QuickMediaIngest
{
    public partial class MainWindow : Window
    {

        internal void OpenLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "QuickMediaIngest",
                    "logs");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                Process.Start(new ProcessStartInfo("explorer.exe", logPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open logs folder.");
                System.Windows.MessageBox.Show(AppLocalizer.Format("Msg_OpenLogsFailed_Body", ex.Message), AppLocalizer.Get("Msg_Error_Title"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        internal void ReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/edwardlthompson/QuickMediaIngest/issues")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to open bug report URL.");
            }
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectAllShootsCommand.Execute(null);
            }
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DeselectAllShootsCommand.Execute(null);
            }
        }

        private void DeleteAfterImportCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _deleteAfterImportUserInitiated = true;
        }

        private void DeleteAfterImportCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            DeleteAfterImportConfirmHelper.HandleChecked(
                vm,
                ref _deleteAfterImportUserInitiated,
                () =>
                {
                    if (sender is System.Windows.Controls.Primitives.ToggleButton tb)
                    {
                        tb.IsChecked = false;
                    }
                });
        }

        private void SidebarCollapseToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ApplySidebarCollapsedChrome(SidebarCollapseToggle?.IsChecked == true);
        }

        /// <summary>
        /// Collapsed rail: narrow column; centered collapse control; Exit only as icon in SettingsCollapsedRail (footer Exit hidden).
        /// </summary>
        private void ApplySidebarCollapsedChrome(bool isCollapsed)
        {
            if (SidebarColumn != null)
            {
                SidebarColumn.Width = new GridLength(isCollapsed ? 64 : 260);
            }

            if (SidebarExpandedContent != null)
            {
                SidebarExpandedContent.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            if (SidebarCollapsedRail != null)
            {
                SidebarCollapsedRail.Visibility = isCollapsed ? Visibility.Visible : Visibility.Collapsed;
            }

            if (SettingsExpandedPanel != null)
            {
                SettingsExpandedPanel.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            if (SettingsCollapsedRail != null)
            {
                SettingsCollapsedRail.Visibility = isCollapsed ? Visibility.Visible : Visibility.Collapsed;
            }

            if (SidebarAppNameText != null)
            {
                SidebarAppNameText.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            if (SidebarAppVersionText != null)
            {
                SidebarAppVersionText.Visibility = Visibility.Visible;
            }

            if (SidebarLogoCollapsed != null)
            {
                SidebarLogoCollapsed.Visibility = isCollapsed ? Visibility.Visible : Visibility.Collapsed;
            }

            if (SidebarHeaderLabelText != null)
            {
                SidebarHeaderLabelText.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            // Footer Exit visibility is bound via Style + DataTrigger to SidebarCollapseToggle.IsChecked (declarative).

            // Pin collapse toggle to center column when narrow; span full width when expanded (layout beats Style Stretch alone).
            if (SidebarCollapseToggle != null && SidebarCollapseToggleHostGrid != null)
            {
                if (isCollapsed)
                {
                    Grid.SetColumn(SidebarCollapseToggle, 1);
                    Grid.SetColumnSpan(SidebarCollapseToggle, 1);
                    SidebarCollapseToggle.HorizontalAlignment = HorizontalAlignment.Center;
                    SidebarCollapseToggle.HorizontalContentAlignment = HorizontalAlignment.Center;
                }
                else
                {
                    Grid.SetColumn(SidebarCollapseToggle, 0);
                    Grid.SetColumnSpan(SidebarCollapseToggle, 3);
                    SidebarCollapseToggle.HorizontalAlignment = HorizontalAlignment.Stretch;
                    SidebarCollapseToggle.HorizontalContentAlignment = HorizontalAlignment.Left;
                }
            }

            if (SidebarHeaderToggleIcon != null)
            {
                SidebarHeaderToggleIcon.Margin = isCollapsed ? new Thickness(0) : new Thickness(0, 0, 6, 0);
            }

            if (SidebarHeaderGrid != null)
            {
                SidebarHeaderGrid.Margin = isCollapsed ? new Thickness(6, 12, 6, 8) : new Thickness(12, 12, 12, 8);
            }

            if (SidebarTitlePanel != null)
            {
                SidebarTitlePanel.HorizontalAlignment = isCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Stretch;
            }

            if (SidebarAppVersionText != null)
            {
                SidebarAppVersionText.HorizontalAlignment = isCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            }

            if (SidebarLogoCollapsed != null)
            {
                SidebarLogoCollapsed.HorizontalAlignment = isCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            }

            // Icon-only theme lives on SidebarCollapsedRail when collapsed; hide label row to avoid duplicate + layout overflow.
            if (SidebarThemeHeaderBorder != null)
            {
                SidebarThemeHeaderBorder.Visibility = isCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ThemeIconButton_Click(object sender, RoutedEventArgs e)
        {
            if (ThemeToggle == null)
            {
                return;
            }

            ThemeToggle.IsChecked = !(ThemeToggle.IsChecked ?? false);
        }

        private void CollapsedSourceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MainViewModel vm || sender is not FrameworkElement element || element.Tag is null)
            {
                return;
            }

            vm.SelectedSource = element.Tag;
        }
    }
}
