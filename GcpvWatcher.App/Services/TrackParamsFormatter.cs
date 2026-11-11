using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Parses GCPV track parameter strings such as "1500 111M", "500A 111M", and "500 No Pts 111M".
/// The trailing 100M or 111M is the track. Words between the distance and the track are qualifiers.
/// </summary>
public static partial class TrackParamsFormatter
{
    /// <summary>
    /// Matches "{distance}{optional series letter} {optional qualifiers} {100|111}m/M".
    /// The series letter is glued to the distance ("500A 111M"): one letter, not M/m,
    /// so a meters marker is not treated as a series.
    /// </summary>
    [GeneratedRegex(@"^\s*(\d+)([A-Za-z-[Mm]])?(?:\s+(.+?))?\s+(100|111)[mM]\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex DistanceAndTrackRegex();

    /// <summary>
    /// Distance, track length, optional series letter, and optional qualifier words
    /// (for example "No Pts") from a recognized track-parameter string.
    /// </summary>
    public readonly record struct ParsedTrackParams(
        int DistanceMeters,
        int TrackMeters,
        string? SeriesLetter,
        string? Qualifiers);

    /// <summary>
    /// Parses a track-parameter string when it ends in a 100 m or 111 m track size.
    /// </summary>
    public static bool TryParse(string trackParams, out ParsedTrackParams parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(trackParams))
            return false;

        var match = DistanceAndTrackRegex().Match(trackParams);
        if (!match.Success)
            return false;

        if (!int.TryParse(match.Groups[1].Value, out var distanceMeters))
            return false;

        if (!int.TryParse(match.Groups[4].Value, out var trackMeters))
            return false;

        var letter = match.Groups[2].Value;
        var qualifiers = NormalizeQualifiers(match.Groups[3].Value);

        parsed = new ParsedTrackParams(
            distanceMeters,
            trackMeters,
            string.IsNullOrEmpty(letter) ? null : letter,
            qualifiers);
        return true;
    }

    /// <summary>
    /// Returns a display-friendly distance (e.g. "1000 111M" → "1000m", "500 No Pts 111M" → "500m No Pts").
    /// Unrecognized formats are returned trimmed and unchanged.
    /// </summary>
    public static string FormatForRaceTitle(string trackParams)
    {
        if (!TryParse(trackParams, out var parsed))
            return string.IsNullOrWhiteSpace(trackParams) ? trackParams ?? string.Empty : trackParams.Trim();

        var distance = $"{parsed.DistanceMeters}m";
        return parsed.Qualifiers is null ? distance : $"{distance} {parsed.Qualifiers}";
    }

    /// <summary>
    /// Series letter glued to the distance (e.g. "500A 111M" → "A", "500A No Pts 111M" → "A").
    /// Used when a meet splits one distance into A/B/C races instead of heats.
    /// </summary>
    public static string? GetSeriesLetter(string trackParams)
    {
        return TryParse(trackParams, out var parsed) ? parsed.SeriesLetter : null;
    }

    private static string? NormalizeQualifiers(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return string.Join(' ', raw.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
