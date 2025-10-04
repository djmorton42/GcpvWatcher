using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Converters;

public class RacersToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<int, int> racers)
        {
            if (racers.Count == 0)
                return "No racers";
                
            var racerStrings = racers
                .OrderBy(kvp => kvp.Value) // Order by lane
                .Select(kvp => $"Racer {kvp.Key} (Lane {kvp.Value})")
                .ToArray();
                
            return string.Join(", ", racerStrings);
        }
        
        return "No racers";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
