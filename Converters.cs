using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace LocalLibrary.Converters
{
    public class BoolToFontStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontStyles.Italic : FontStyles.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPrimary)
            { 
                var resourceKey = isPrimary ? "PositiveRatingBrush" : "TextBrush";

                return Application.Current.TryFindResource(resourceKey);
            }
            // Fallback for invalid values
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}