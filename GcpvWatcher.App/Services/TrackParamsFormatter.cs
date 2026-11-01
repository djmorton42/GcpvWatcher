using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Formats GCPV track parameter strings for display in race titles.
/// Raw values (e.g. "1000 111M") are left unchanged for lap calculation.
/// </summary>
public static partial class TrackParamsFormatter
{
    /// <summary>
    /// Matches "{distance}{optional series letter} {100|111}m/M".
    /// The series letter is glued to the distance ("500A 111M"): one letter, not M/m,
    /// so a meters marker is not treated as a series.
    /// </summary>
    [GeneratedRegex(@"^\s*(\d+)([A-Za-z-[Mm]])?\s+(?:100|111)[mM]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex DistanceAndTrackRegex();

    /// <summary>
    /// Returns a display-friendly distance (e.g. "1000 111M" → "1000m", "500A 111M" → "500m").
    /// Unrecognized formats are returned trimmed and unchanged.
    /// </summary>
    public static string FormatForRaceTitle(string trackParams)
    {
        var match = MatchDistanceAndTrack(trackParams);
        if (match?.Success == true)
            return $"{match.Groups[1].Value}m";

        return string.IsNullOrWhiteSpace(trackParams) ? trackParams ?? string.Empty : trackParams.Trim();
    }

    /// <summary>
    /// Series letter glued to the distance (e.g. "500A 111M" → "A").
    /// Used when a meet splits one distance into A/B/C races instead of heats.
    /// </summary>
    public static string? GetSeriesLetter(string trackParams)
    {
        var match = MatchDistanceAndTrack(trackParams);
        if (match?.Success != true)
            return null;

        var letter = match.Groups[2].Value;
        return string.IsNullOrEmpty(letter) ? null : letter;
    }

    private static Match? MatchDistanceAndTrack(string trackParams)
    {
        if (string.IsNullOrWhiteSpace(trackParams))
            return null;

        var match = DistanceAndTrackRegex().Match(trackParams);
        return match.Success ? match : null;
    }
}
