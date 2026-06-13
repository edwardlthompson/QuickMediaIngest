using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using MaterialDesignThemes.Wpf;

namespace QuickMediaIngest.Converters
{
    public class SourceToIconKindConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UnifiedSourceItem)
            {
                return PackIconKind.ViewGrid;
            }

            if (value is FtpSourceItem)
            {
                return PackIconKind.SourceBranch;
            }

            if (value is string drivePath)
            {
                try
                {
                    var driveInfo = new DriveInfo(drivePath);
                    if (driveInfo.DriveType == DriveType.Removable)
                    {
                        return PackIconKind.Sd;
                    }

                    if (driveInfo.DriveType == DriveType.Fixed)
                    {
                        return PackIconKind.Harddisk;
                    }
                }
                catch
                {
                    // Fallback for non-drive strings.
                }
            }

            return PackIconKind.Folder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
