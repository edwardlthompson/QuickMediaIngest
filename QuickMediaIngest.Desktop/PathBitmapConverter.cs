#nullable enable
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace QuickMediaIngest.Desktop;

public sealed class PathBitmapConverter : IValueConverter
{
    internal const int DecodeWidth = 320;

    private static readonly ConcurrentDictionary<string, Bitmap> Cache = new(StringComparer.Ordinal);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        return Cache.GetOrAdd(path, LoadScaled);
    }

    internal static Bitmap LoadScaled(string path)
    {
        using var stream = File.OpenRead(path);
        return Bitmap.DecodeToWidth(stream, DecodeWidth, BitmapInterpolationMode.LowQuality);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
