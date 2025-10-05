using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.App.Converters;

public class RacersToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Dictionary<int, int> racers)
        {
            if (racers.Count == 0)
                return "No racers";
            
            // Get racer data from static service
            var racerData = RacerDataService.GetRacers();
                
            var racerStrings = racers
                .OrderBy(kvp => kvp.Value) // Order by lane
                .Select(kvp => 
                {
                    var racerId = kvp.Key;
                    var lane = kvp.Value;
                    
                    if (racerData != null && racerData.TryGetValue(racerId, out var racer))
                    {
                        // Use fixed-width formatting optimized for monospace font
                        return $"Lane {lane,2}, {racerId,4} - {racer.FirstName} {racer.LastName} ({racer.Affiliation})";
                    }
                    else
                    {
                        return $"Lane {lane,2}, {racerId,4}";
                    }
                })
                .ToArray();
                
            return string.Join(Environment.NewLine, racerStrings);
        }
        
        return "No racers";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
