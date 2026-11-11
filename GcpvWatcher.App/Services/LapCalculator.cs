namespace GcpvWatcher.App.Services;

public class LapCalculator
{
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
            return CalculateLaps(parsed.DistanceMeters, parsed.TrackMeters);
        }

        var trackLength = 100;
        if (raceParams.Contains("111m", StringComparison.OrdinalIgnoreCase)) {
            trackLength = 111;
        }

        foreach (var distance in distances) {
            if (raceParams.Contains(distance.ToString())) {
                return CalculateLaps(distance, trackLength);
            }
        }

        return 0;
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

