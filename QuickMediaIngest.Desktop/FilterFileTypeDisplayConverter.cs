#nullable enable
using System;
using System.Globalization;
using Avalonia.Data.Converters;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Desktop;

public sealed class FilterFileTypeDisplayConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string stored ? FilterFileTypeLocalization.GetDisplayLabel(stored) : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
