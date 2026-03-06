using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;

namespace GcpvWatcher.App.Converters;

/// <summary>
/// Returns true when the race number has 2+ alpha characters (e.g. 37CD, 15BC), indicating a consolidated race.
/// </summary>
public class RaceNumberToIsConsolidatedConverter : IValueConverter
{
    private static readonly Regex RaceNumberRegex = new(@"^(\d+)([A-Z]+)$", RegexOptions.Compiled);

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string raceNumber || string.IsNullOrEmpty(raceNumber))
            return false;
        var match = RaceNumberRegex.Match(raceNumber);
        return match.Success && match.Groups[2].Value.Length >= 2;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
