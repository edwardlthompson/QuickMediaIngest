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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PersistWindowState();
        }

        private void RibbonTileHandle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isRibbonTileDragInProgress || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var currentPosition = e.GetPosition(this);
            if (Math.Abs(currentPosition.X - _ribbonTileDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _ribbonTileDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            var tile = GetRibbonTile(sender as DependencyObject);
            if (tile == null)
            {
                return;
            }

            var data = new System.Windows.DataObject(RibbonTileDragFormat, tile);

            try
            {
                _isRibbonTileDragInProgress = true;
                _activeRibbonDraggedTile = tile;
                System.Windows.DragDrop.DoDragDrop(tile, data, System.Windows.DragDropEffects.Move);
            }
            finally
            {
                _isRibbonTileDragInProgress = false;
                _activeRibbonDraggedTile = null;
                _activeRibbonPreviewIndex = -1;
                ClearRibbonTileOffsets();
            }
        }

        private void RibbonTilePanel_DragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(RibbonTileDragFormat) || RibbonTilePanel == null)
            {
                e.Effects = System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.Effects = System.Windows.DragDropEffects.Move;

            if (e.Data.GetData(RibbonTileDragFormat) is Border draggedTile)
            {
                int dropIndex = GetRibbonDropIndex(e.GetPosition(RibbonTilePanel), draggedTile);
                if (dropIndex != _activeRibbonPreviewIndex)
                {
                    _activeRibbonPreviewIndex = dropIndex;
                    PreviewRibbonTileReorderAtIndex(draggedTile, dropIndex);
                }
            }

            e.Handled = true;
        }

        private void RibbonTilePanel_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            _activeRibbonPreviewIndex = -1;

            if (!e.Data.GetDataPresent(RibbonTileDragFormat) || RibbonTilePanel == null)
            {
                ClearRibbonTileOffsets();
                return;
            }

            if (e.Data.GetData(RibbonTileDragFormat) is not Border draggedTile)
            {
                ClearRibbonTileOffsets();
                return;
            }

            int sourceIndex = RibbonTilePanel.Children.IndexOf(draggedTile);
            if (sourceIndex < 0)
            {
                ClearRibbonTileOffsets();
                return;
            }

            int dropIndex = GetRibbonDropIndex(e.GetPosition(RibbonTilePanel), draggedTile);
            int insertIndex = dropIndex;
            if (sourceIndex < insertIndex)
            {
                insertIndex--;
            }

            if (insertIndex < 0) insertIndex = 0;
            if (insertIndex > RibbonTilePanel.Children.Count - 1) insertIndex = RibbonTilePanel.Children.Count - 1;

            if (insertIndex == sourceIndex)
            {
                ClearRibbonTileOffsets();
                return;
            }

            RibbonTilePanel.Children.RemoveAt(sourceIndex);
            RibbonTilePanel.Children.Insert(insertIndex, draggedTile);

            // Persist the new order
            if (DataContext is MainViewModel vm)
            {
                var order = RibbonTilePanel.Children
                    .OfType<System.Windows.Controls.Border>()
                    .Where(b => b.Tag is string)
                    .Select(b => (string)b.Tag!);
                vm.SaveTileOrder(order);
            }

            ClearRibbonTileOffsets();
        }

        private void RibbonTilePanel_DragLeave(object sender, DragEventArgs e)
        {
            _activeRibbonPreviewIndex = -1;
            ClearRibbonTileOffsets();
        }

        private int GetRibbonDropIndex(Point panelPosition, Border draggedTile)
        {
            if (RibbonTilePanel == null)
            {
                return 0;
            }

            var tiles = RibbonTilePanel.Children.OfType<Border>().ToList();
            if (tiles.Count == 0)
            {
                return 0;
            }

            double x = panelPosition.X;

            foreach (var tile in tiles)
            {
                if (tile == draggedTile)
                {
                    continue;
                }

                var tileLeft = tile.TranslatePoint(new Point(0, 0), RibbonTilePanel).X;
                double midpoint = tileLeft + (tile.ActualWidth / 2);
                if (x < midpoint)
                {
                    return RibbonTilePanel.Children.IndexOf(tile);
                }
            }

            return RibbonTilePanel.Children.Count;
        }

        private void PreviewRibbonTileReorderAtIndex(Border draggedTile, int dropIndex)
        {
            if (RibbonTilePanel == null)
            {
                return;
            }

            int sourceIndex = RibbonTilePanel.Children.IndexOf(draggedTile);
            if (sourceIndex < 0)
            {
                ClearRibbonTileOffsets();
                return;
            }

            if (dropIndex < 0) dropIndex = 0;
            if (dropIndex > RibbonTilePanel.Children.Count) dropIndex = RibbonTilePanel.Children.Count;

            if (dropIndex == sourceIndex || dropIndex == sourceIndex + 1)
            {
                ClearRibbonTileOffsets();
                return;
            }

            double shift = draggedTile.ActualWidth;
            if (shift <= 0)
            {
                shift = 140;
            }

            if (draggedTile.Margin.Left > 0)
            {
                shift += draggedTile.Margin.Left;
            }
            if (draggedTile.Margin.Right > 0)
            {
                shift += draggedTile.Margin.Right;
            }

            foreach (var tile in RibbonTilePanel.Children.OfType<Border>())
            {
                if (tile == draggedTile)
                {
                    continue;
                }

                int tileIndex = RibbonTilePanel.Children.IndexOf(tile);
                double targetOffset = 0;

                if (sourceIndex < dropIndex)
                {
                    if (tileIndex > sourceIndex && tileIndex < dropIndex)
                    {
                        targetOffset = -shift;
                    }
                }
                else if (sourceIndex > dropIndex)
                {
                    if (tileIndex >= dropIndex && tileIndex < sourceIndex)
                    {
                        targetOffset = shift;
                    }
                }

                AnimateRibbonTileOffset(tile, targetOffset);
            }
        }

        private void ClearRibbonTileOffsets()
        {
            if (RibbonTilePanel == null)
            {
                return;
            }

            foreach (var tile in RibbonTilePanel.Children.OfType<Border>())
            {
                AnimateRibbonTileOffset(tile, 0);
            }
        }

        private static void AnimateRibbonTileOffset(Border tile, double offset)
        {
            if (tile.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                tile.RenderTransform = transform;
            }

            var animation = new DoubleAnimation
            {
                To = offset,
                Duration = TimeSpan.FromMilliseconds(140),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            transform.BeginAnimation(TranslateTransform.XProperty, animation, HandoffBehavior.SnapshotAndReplace);
        }

        private Border? GetRibbonTile(DependencyObject? origin)
        {
            while (origin != null)
            {
                if (origin is Border border && border.Parent == RibbonTilePanel)
                {
                    return border;
                }

                origin = VisualTreeHelper.GetParent(origin);
            }

            return null;
        }
    }
}
