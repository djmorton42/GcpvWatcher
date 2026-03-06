using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Converters;

/// <summary>
/// Converts a Race to a background brush for the races list: keep file = light yellow, consolidated = light green, otherwise light gray.
/// </summary>
public class RaceToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Race race)
        {
            return new SolidColorBrush(Color.FromRgb(211, 211, 211)); // LightGray default
        }

        if (race.IsFromKeepFile)
        {
            return new SolidColorBrush(Color.FromRgb(255, 255, 200)); // Light yellow (keep file)
        }

        if (race.IsConsolidated)
        {
            return new SolidColorBrush(Color.FromRgb(200, 255, 220)); // Light green (consolidated)
        }

        return new SolidColorBrush(Color.FromRgb(211, 211, 211)); // LightGray
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
