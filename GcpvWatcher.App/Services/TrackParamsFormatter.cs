using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Formats GCPV track parameter strings for display in race titles.
/// Raw values (e.g. "1000 111M") are left unchanged for lap calculation.
/// </summary>
public static partial class TrackParamsFormatter
{
    /// <summary>
    /// Matches "{distance} {100|111}m/M" (optional surrounding whitespace).
    /// </summary>
    [GeneratedRegex(@"^\s*(\d+)\s+(?:100|111)[mM]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex DistanceAndTrackRegex();

    /// <summary>
    /// Returns a display-friendly distance (e.g. "1000 111M" → "1000m").
    /// Unrecognized formats are returned trimmed and unchanged.
    /// </summary>
    public static string FormatForRaceTitle(string trackParams)
    {
        if (string.IsNullOrWhiteSpace(trackParams))
            return trackParams ?? string.Empty;

        var match = DistanceAndTrackRegex().Match(trackParams);
        if (match.Success)
            return $"{match.Groups[1].Value}m";

        return trackParams.Trim();
    }
}
