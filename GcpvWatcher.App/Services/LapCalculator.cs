namespace GcpvWatcher.App.Services;

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

    public static double CalculateLaps(string raceParams) {
        if (string.IsNullOrWhiteSpace(raceParams)) {
            return 0;
        }

        // "500 No Pts 111M" and "400 No Pts 100M" name the track explicitly.
        // Take that distance and track size rather than searching the whole string,
        // so the "100" in "100M" is not mistaken for the race distance.
        if (TrackParamsFormatter.TryParse(raceParams, out var parsed)) {
            var stated = parsed.TrackMeters == 111 ? ShortTrackMeters : StandardTrackMeters;
            var other = stated == ShortTrackMeters ? StandardTrackMeters : ShortTrackMeters;
            return ChooseTrack(parsed.DistanceMeters, stated, other);
        }

        int? distance = null;
        foreach (var candidate in distances) {
            if (raceParams.Contains(candidate.ToString())) {
                distance = candidate;
                break;
            }
        }

        if (distance is null) {
            return 0;
        }

        // No track token. Prefer 100 m unless the text mentions the short track,
        // then keep whichever length lands on a whole or half lap.
        var prefersShort = raceParams.Contains("111m", StringComparison.OrdinalIgnoreCase);
        var primary = prefersShort ? ShortTrackMeters : StandardTrackMeters;
        var secondary = prefersShort ? StandardTrackMeters : ShortTrackMeters;
        return ChooseTrack(distance.Value, primary, secondary);
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

