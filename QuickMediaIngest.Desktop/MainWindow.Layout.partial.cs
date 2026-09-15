#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using QuickMediaIngest.Core.AppModel;
using QuickMediaIngest.Core.Chrome;
using QuickMediaIngest.Core.Models;

namespace QuickMediaIngest.Desktop;

public partial class MainWindow
{
    private int _previewGen;
    private int _previewDecodeWidth;
    private string _previewLoadedPath = string.Empty;
    private Bitmap? _previewBitmap;
    private bool _restoringLayout;
    private bool _haveNormalBounds;
    private double _normalWidth;
    private double _normalHeight;
    private PixelPoint _normalPosition;

    private ColumnDefinition PreviewColumn => ShootSplitGrid.ColumnDefinitions[2];

    private void ApplyWindowIcon()
    {
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://QuickMediaIngest.Desktop/Assets/AppIcon.png"));
            Icon = new WindowIcon(stream);
        }
        catch
        {
            // Welcome image still uses the same asset; the panel can fall back to the .desktop icon.
        }
    }

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Host.Model.CommandBarCompact = CommandBarOverflow.IsCompact(e.NewSize.Width);
        RememberNormalBounds();
    }

    private void RememberNormalBounds()
    {
        if (_restoringLayout || WindowState != WindowState.Normal)
        {
            return;
        }

        if (double.IsNaN(Width) || double.IsNaN(Height) || Width < MinWidth || Height < MinHeight)
        {
            return;
        }

        _normalWidth = Width;
        _normalHeight = Height;
        _normalPosition = Position;
        _haveNormalBounds = true;
        PrefsState prefs = Bench.Prefs;
        prefs.WindowWidth = _normalWidth;
        prefs.WindowHeight = _normalHeight;
        prefs.WindowLeft = _normalPosition.X;
        prefs.WindowTop = _normalPosition.Y;
        prefs.WindowPositionSet = true;
        prefs.WindowMaximized = false;
    }

    private void ApplyWindowLayout()
    {
        PrefsState prefs = Bench.Prefs;
        _restoringLayout = true;
        try
        {
            if (PrefsLayout.HasSavedSize(prefs.WindowWidth, prefs.WindowHeight))
            {
                Width = Math.Max(MinWidth, prefs.WindowWidth);
                Height = Math.Max(MinHeight, prefs.WindowHeight);
                _normalWidth = Width;
                _normalHeight = Height;
                _haveNormalBounds = true;
            }

            if (prefs.WindowPositionSet)
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                PixelPoint point = new((int)Math.Round(prefs.WindowLeft), (int)Math.Round(prefs.WindowTop));
                Position = Screens is null ? point : ClampToScreens(point);
                _normalPosition = Position;
                _haveNormalBounds = _haveNormalBounds || PrefsLayout.HasSavedSize(prefs.WindowWidth, prefs.WindowHeight);
            }
            else if (Screens?.Primary is Screen primary)
            {
                PixelRect area = primary.WorkingArea;
                int w = (int)Math.Round(Width);
                int h = (int)Math.Round(Height);
                Position = new PixelPoint(
                    area.X + Math.Max(0, (area.Width - w) / 2),
                    area.Y + Math.Max(0, (area.Height - h) / 2));
            }

            if (prefs.WindowMaximized)
            {
                WindowState = WindowState.Maximized;
            }

            PreviewColumn.Width = new GridLength(PrefsLayout.ClampPane(prefs.PreviewPaneWidth), GridUnitType.Pixel);
        }
        finally
        {
            _restoringLayout = false;
        }
    }

    private PixelPoint ClampToScreens(PixelPoint point)
    {
        Screen? screen = Screens?.ScreenFromPoint(point) ?? Screens?.Primary;
        if (screen is null)
        {
            return point;
        }

        PixelRect area = screen.WorkingArea;
        int maxX = Math.Max(area.X, area.X + area.Width - 80);
        int maxY = Math.Max(area.Y, area.Y + area.Height - 80);
        return new PixelPoint(Math.Clamp(point.X, area.X, maxX), Math.Clamp(point.Y, area.Y, maxY));
    }

    private void CaptureWindowLayout()
    {
        RememberNormalBounds();
        PrefsState prefs = Bench.Prefs;
        prefs.WindowMaximized = WindowState == WindowState.Maximized;
        if (_haveNormalBounds)
        {
            prefs.WindowWidth = _normalWidth;
            prefs.WindowHeight = _normalHeight;
            prefs.WindowLeft = _normalPosition.X;
            prefs.WindowTop = _normalPosition.Y;
            prefs.WindowPositionSet = true;
        }

        prefs.PreviewPaneWidth = PrefsLayout.CoalescePane(prefs.PreviewPaneWidth, PreviewPane.Bounds.Width);
    }

    private void ThumbTile_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ImportItem item })
        {
            Bench.SelectedCullItem = item;
        }
    }

    private void PreviewPane_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!_restoringLayout && e.NewSize.Width >= PrefsLayout.MinPane)
        {
            Bench.Prefs.PreviewPaneWidth = PrefsLayout.ClampPane(e.NewSize.Width);
        }

        if (!_viewReady || e.NewSize.Width < PrefsLayout.MinPane || !Bench.HasPreview)
        {
            return;
        }

        LoadPreview();
    }

    private async void ShowSelectedPreview()
    {
        int gen = ++_previewGen;
        ImportItem? item = Bench.SelectedCullItem;
        if (item is null)
        {
            Bench.PreviewPath = string.Empty;
            LoadPreview();
            return;
        }

        string? path = await Task.Run(() => IngestBenchThumbs.FullJpeg(Host, item));
        if (gen != _previewGen)
        {
            return;
        }

        _previewLoadedPath = string.Empty;
        _previewDecodeWidth = 0;
        Bench.PreviewPath = path ?? string.Empty;
        LoadPreview();
    }

    private void LoadPreview()
    {
        double dip = PreviewPane.Bounds.Width > 1 ? PreviewPane.Bounds.Width : PreviewColumn.Width.Value;
        double scale = (VisualRoot as TopLevel)?.RenderScaling ?? 1;
        int width = Math.Max(320, (int)Math.Round(dip * Math.Max(1, scale)));
        string path = Bench.PreviewPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _previewLoadedPath = string.Empty;
            SetPreviewBitmap(null);
            return;
        }

        if (_previewBitmap is not null &&
            string.Equals(_previewLoadedPath, path, StringComparison.Ordinal) &&
            Math.Abs(width - _previewDecodeWidth) < 24)
        {
            return;
        }

        try
        {
            using var stream = File.OpenRead(path);
            SetPreviewBitmap(Bitmap.DecodeToWidth(stream, width, BitmapInterpolationMode.HighQuality));
            _previewDecodeWidth = width;
            _previewLoadedPath = path;
        }
        catch
        {
            _previewLoadedPath = string.Empty;
            SetPreviewBitmap(null);
        }
    }

    private void SetPreviewBitmap(Bitmap? bitmap)
    {
        Bitmap? previous = _previewBitmap;
        _previewBitmap = bitmap;
        PreviewImage.Source = bitmap;
        if (!ReferenceEquals(previous, bitmap))
        {
            previous?.Dispose();
        }
    }
}
