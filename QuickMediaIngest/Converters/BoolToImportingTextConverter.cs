using System;
using System.Globalization;
using System.Windows.Data;

namespace QuickMediaIngest.Converters
{
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
