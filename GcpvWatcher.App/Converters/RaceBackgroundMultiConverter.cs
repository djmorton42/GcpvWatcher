using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GcpvWatcher.App.Converters;

/// <summary>
/// Converts (IsFromKeepFile, RaceNumber) to a background brush: keep file = light yellow,
/// race number with 2+ alpha chars (e.g. 37CD) = light green (consolidated), otherwise light gray.
/// </summary>
public class RaceBackgroundMultiConverter : IMultiValueConverter
{
    private static readonly Regex RaceNumberRegex = new(@"^(\d+)([A-Z]+)$", RegexOptions.Compiled);

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
            return new SolidColorBrush(Color.FromRgb(211, 211, 211));

        // During binding init, values can be UnsetValueType or null
        var isFromKeepFile = values[0] is true;
        var raceNumber = values[1] as string;

        if (isFromKeepFile)
            return new SolidColorBrush(Color.FromRgb(255, 255, 200)); // Light yellow (keep file)

        // Consolidated = race number has 2+ alpha characters (e.g. 37CD, 15BC)
        if (!string.IsNullOrEmpty(raceNumber))
        {
            var match = RaceNumberRegex.Match(raceNumber);
            if (match.Success && match.Groups[2].Value.Length >= 2)
                return new SolidColorBrush(Color.FromRgb(200, 255, 220)); // Light green (consolidated)
        }

        return new SolidColorBrush(Color.FromRgb(211, 211, 211)); // LightGray
    }
}
