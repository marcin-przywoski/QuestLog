using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace QuestLog.GUI.Converters
{
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? FontWeight.Normal : FontWeight.Bold;
            }

            return FontWeight.Normal;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
