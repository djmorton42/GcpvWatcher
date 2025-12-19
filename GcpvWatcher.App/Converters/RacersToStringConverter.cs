using System;
using System.Collections.Generic;
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
        if (value is Dictionary<string, int> racers)
        {
            if (racers.Count == 0)
                return "No racers";
            
            // Get racer data from static service
            var racerData = RacerDataService.GetRacers();
                
            // Calculate the maximum racer ID length for consistent alignment
            var maxRacerIdLength = racers.Keys.DefaultIfEmpty("").Max(id => id.Length);
            var racerIdWidth = Math.Max(maxRacerIdLength, 4); // Minimum 4 characters for alignment
            
            var racerStrings = racers
                .OrderBy(kvp => kvp.Value) // Order by lane
                .Select(kvp => 
                {
                    var racerId = kvp.Key;
                    var lane = kvp.Value;
                    
                    if (racerData != null && racerData.TryGetValue(racerId, out var racer))
                    {
                        // Use fixed-width formatting optimized for monospace font
                        // Format racer ID with padding to align all IDs consistently
                        var paddedRacerId = racerId.PadLeft(racerIdWidth);
                        
                        // Build name part - only include what's available
                        var nameParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(racer.FirstName))
                            nameParts.Add(racer.FirstName);
                        if (!string.IsNullOrWhiteSpace(racer.LastName))
                            nameParts.Add(racer.LastName);
                        var namePart = nameParts.Count > 0 ? string.Join(" ", nameParts) : null;
                        
                        // Build affiliation part - only include if available
                        var affiliationPart = !string.IsNullOrWhiteSpace(racer.Affiliation) 
                            ? $" ({racer.Affiliation})" 
                            : string.Empty;
                        
                        // Build the full string - only include name and affiliation if available
                        if (namePart != null)
                        {
                            return $"Lane {lane,2}, {paddedRacerId} - {namePart}{affiliationPart}";
                        }
                        else if (!string.IsNullOrWhiteSpace(racer.Affiliation))
                        {
                            // Only affiliation available
                            return $"Lane {lane,2}, {paddedRacerId} - {racer.Affiliation}";
                        }
                        else
                        {
                            // Only racer ID available
                            return $"Lane {lane,2}, {paddedRacerId}";
                        }
                    }
                    else
                    {
                        var paddedRacerId = racerId.PadLeft(racerIdWidth);
                        return $"Lane {lane,2}, {paddedRacerId}";
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
