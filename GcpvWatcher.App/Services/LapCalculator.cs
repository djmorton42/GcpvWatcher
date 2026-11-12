using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

public readonly record struct LapCalculation(double Laps, string? Warning, string? Caution = null);

public class LapCalculator
{
    /// <summary>
    /// The labelled "111M" track is 111.11 m, so 1000 m is 9 laps.
    /// 777 m is 7 × 111 m, about 0.007 laps short of 7 × 111.11 m.
    /// </summary>
    private const double ShortTrackMeters = 111.11;

    private const double StandardTrackMeters = 100;

    /// <summary>
    /// How far a quotient may sit from a whole or half lap and still be that lap count.
    /// Wide enough for the 111.11 m measurement, narrow enough to reject 3.6 or 5.4.
    /// </summary>
    private const double HalfLapTolerance = 0.02;

    private static readonly List<int> distances = [
        5000, 3000, 2000, 1500, 1000, 800, 777, 600, 500, 400, 333, 300, 200, 100, 50
    ];

    public static double CalculateLaps(string raceParams) =>
        ResolveLaps(raceParams, ForcedTrackLength.Auto).Laps;

    /// <summary>
    /// Lap count for an event string.
    /// Auto follows the file: an explicit 100M or 111M marker wins, and a length that is not a
    /// whole or half lap is the other track. A forced length is used for every race, including
    /// when the file names the other track. If that quotient is not a whole or half lap, it is
    /// kept and <see cref="LapCalculation.Warning"/> explains why.
    /// When the file names the other track and the forced count is still a whole or half lap,
    /// <see cref="LapCalculation.Caution"/> records the disagreement.
    /// </summary>
    public static LapCalculation ResolveLaps(string raceParams, ForcedTrackLength forcedTrack)
    {
        if (string.IsNullOrWhiteSpace(raceParams))
            return new LapCalculation(0, null);

        // "500 No Pts 111M" and "400 No Pts 100M" name the track explicitly.
        // Take that distance and track size rather than searching the whole string,
        // so the "100" in "100M" is not mistaken for the race distance.
        int? distance = null;
        double? statedTrack = null;
        if (TrackParamsFormatter.TryParse(raceParams, out var parsed))
        {
            distance = parsed.DistanceMeters;
            statedTrack = parsed.TrackMeters == 111 ? ShortTrackMeters : StandardTrackMeters;
        }
        else
        {
            distance = FindDistance(raceParams);
        }

        if (distance is null)
            return new LapCalculation(0, null);

        if (forcedTrack != ForcedTrackLength.Auto)
            return ForceTrack(distance.Value, forcedTrack, statedTrack);

        if (statedTrack is double stated)
        {
            var other = stated == ShortTrackMeters ? StandardTrackMeters : ShortTrackMeters;
            return new LapCalculation(ChooseTrack(distance.Value, stated, other), null);
        }

        var prefersShort = raceParams.Contains("111m", StringComparison.OrdinalIgnoreCase);
        var primary = prefersShort ? ShortTrackMeters : StandardTrackMeters;
        var secondary = prefersShort ? StandardTrackMeters : ShortTrackMeters;
        return new LapCalculation(ChooseTrack(distance.Value, primary, secondary), null);
    }

    private static LapCalculation ForceTrack(int distance, ForcedTrackLength forcedTrack, double? statedTrack)
    {
        var meters = forcedTrack == ForcedTrackLength.Meters111 ? ShortTrackMeters : StandardTrackMeters;
        var label = forcedTrack == ForcedTrackLength.Meters111 ? "111 m" : "100 m";
        if (TryWholeOrHalfLaps(distance, meters, out var laps))
            return new LapCalculation(laps, null, CautionForDisagreement(statedTrack, meters, laps, label));

        var rounded = Math.Round(distance / meters, 2, MidpointRounding.AwayFromZero);
        var warning = $"{distance} m is {rounded:0.00} laps on the {label} track, not a whole or half lap.";
        return new LapCalculation(rounded, warning);
    }

    private static string? CautionForDisagreement(double? statedTrack, double forcedMeters, double laps, string forcedLabel)
    {
        if (statedTrack is null || statedTrack == forcedMeters)
            return null;

        var statedLabel = statedTrack == ShortTrackMeters ? "111 m" : "100 m";
        return $"File says {statedLabel}. Using {laps:0.#} laps on the {forcedLabel} track.";
    }

    private static int? FindDistance(string raceParams)
    {
        foreach (var candidate in distances)
        {
            if (raceParams.Contains(candidate.ToString()))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Uses <paramref name="preferredTrack"/> when that quotient is a whole or half lap.
    /// A 400 m race on the 111.11 m track is 3.6 laps, which is not, so the other track is used.
    /// </summary>
    private static double ChooseTrack(int distance, double preferredTrack, double otherTrack) {
        if (TryWholeOrHalfLaps(distance, preferredTrack, out var laps)) {
            return laps;
        }

        if (TryWholeOrHalfLaps(distance, otherTrack, out laps)) {
            return laps;
        }

        return 0;
    }

    private static bool TryWholeOrHalfLaps(int distance, double trackLength, out double laps) {
        laps = 0;
        if (trackLength == 0) {
            return false;
        }

        var raw = distance / trackLength;
        var nearestHalf = Math.Round(raw * 2, MidpointRounding.AwayFromZero) / 2.0;
        if (Math.Abs(raw - nearestHalf) > HalfLapTolerance) {
            return false;
        }

        laps = nearestHalf;
        return true;
    }

    public static double CalculateLaps(int distance, int trackLength) {
        if (trackLength == 0) {
            throw new ArgumentException("Track length cannot be zero.", nameof(trackLength));
        }

        if (distance < 0) {
            throw new ArgumentException("Distance cannot be negative.", nameof(distance));
        }

        if (trackLength < 0) {
            throw new ArgumentException("Track length cannot be negative.", nameof(trackLength));
        }

        return Math.Round(distance * 1.0 / trackLength, 1);
    }
}

