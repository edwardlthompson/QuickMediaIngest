#nullable enable
using System;
using System.Globalization;
using System.Windows.Data;
using QuickMediaIngest.Localization;

namespace QuickMediaIngest.Converters
{
    public sealed class FilterFileTypeToDisplayConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is string s ? FilterFileTypeLocalization.GetDisplayLabel(s) : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
