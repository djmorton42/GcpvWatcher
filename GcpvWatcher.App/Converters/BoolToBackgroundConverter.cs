using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GcpvWatcher.App.Converters;

public class BoolToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isFromKeepFile && isFromKeepFile)
        {
            // Light yellow/cream background for keep file races
            return new SolidColorBrush(Color.FromRgb(255, 255, 200)); // Light yellow
        }
        
        // Default light gray background for regular races
        return new SolidColorBrush(Color.FromRgb(211, 211, 211)); // LightGray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

