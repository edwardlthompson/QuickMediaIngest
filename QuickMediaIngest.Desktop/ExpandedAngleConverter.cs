#nullable enable
using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace QuickMediaIngest.Desktop;

public sealed class ExpandedAngleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? 90d : 0d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
