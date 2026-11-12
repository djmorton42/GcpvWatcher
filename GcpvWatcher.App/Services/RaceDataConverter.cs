using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

public class RaceDataConverter
{
    private readonly AppConfig? _config;

    public RaceDataConverter(AppConfig? config = null)
    {
        _config = config;
    }

    public IEnumerable<Race> ConvertGcpvRacesToRaces(IEnumerable<GcpvRaceData> gcpvRaces)
    {
        var races = new List<Race>();

        foreach (var gcpvRace in gcpvRaces)
        {
            try
            {
                var race = ConvertGcpvRaceToRace(gcpvRace);
                races.Add(race);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogException($"Error converting race {gcpvRace.RaceNumber}", ex);
                // Continue processing other races even if one fails
            }
        }

        return races;
    }

    private Race ConvertGcpvRaceToRace(GcpvRaceData gcpvRace)
    {
        var forcedTrack = _config?.ForcedTrackLength ?? ForcedTrackLength.Auto;
        var calculation = LapCalculator.ResolveLaps(gcpvRace.TrackParams, forcedTrack);
        if (!string.IsNullOrEmpty(calculation.Warning))
            WatcherLogger.Log($"Race {gcpvRace.RaceNumber}: {calculation.Warning}");
        else if (!string.IsNullOrEmpty(calculation.Caution))
            WatcherLogger.Log($"Race {gcpvRace.RaceNumber}: {calculation.Caution}");

        var numberOfLaps = calculation.Laps;
        if (numberOfLaps <= 0)
        {
            throw new InvalidOperationException($"Could not determine number of laps for race {gcpvRace.RaceNumber} with track params: {gcpvRace.TrackParams}");
        }

        // Create event title in format "RaceGroup Distance [qualifiers] Stage [series]"
        // e.g. "Open Men B 1500m Heat", "All Skaters 500m No Pts Final", "All Skaters 500m Final A".
        // Lap count above uses the same parse. The 111 m track is 111.11 m.
        // Auto substitutes the other track when a length is not a whole or half lap.
        // A forced length is used as-is. A bad lap count is a warning; a file that names the other track is a caution.
        var distanceDisplay = TrackParamsFormatter.FormatForRaceTitle(gcpvRace.TrackParams);
        var seriesLetter = TrackParamsFormatter.GetSeriesLetter(gcpvRace.TrackParams);
        var stage = string.IsNullOrEmpty(seriesLetter)
            ? gcpvRace.Stage
            : $"{gcpvRace.Stage} {seriesLetter}";
        var eventTitle = $"{gcpvRace.RaceGroup} {distanceDisplay} {stage}";

        // Convert racers to the format expected by Race object
        // Race object expects Dictionary<string, int> where key is racer ID (can be alphanumeric) and value is lane
        var racers = new Dictionary<string, int>();
        
        for (int i = 0; i < gcpvRace.Racers.Count; i++)
        {
            var racer = gcpvRace.Racers[i];
            
            // Parse lane number
            if (!int.TryParse(racer.Lane, out var laneNumber))
            {
                ApplicationLogger.Log($"Warning: Could not parse lane number '{racer.Lane}' for racer '{racer.Racer}' in race {gcpvRace.RaceNumber}");
                continue;
            }

            // Parse racer ID from the racer field format: "{racer_id} {last_name},{first_name}"
            if (!TryParseRacerId(racer.Racer, out var racerId))
            {
                ApplicationLogger.Log($"Warning: Could not parse racer ID from '{racer.Racer}' in race {gcpvRace.RaceNumber}");
                continue;
            }
            
            racers[racerId] = laneNumber;
        }

        return new Race(gcpvRace.RaceNumber, eventTitle, (decimal)numberOfLaps, racers)
        {
            TrackWarning = calculation.Warning,
            TrackCaution = calculation.Caution
        };
    }

    private static bool TryParseRacerId(string racerField, out string racerId)
    {
        racerId = string.Empty;
        
        if (string.IsNullOrWhiteSpace(racerField))
            return false;

        // Parse format: "{racer_id} {last_name},{first_name}"
        // Find the first space to separate racer_id from the name
        var firstSpaceIndex = racerField.IndexOf(' ');
        if (firstSpaceIndex <= 0)
            return false;

        // Extract the racer ID (everything before the first space) - can be alphanumeric
        racerId = racerField.Substring(0, firstSpaceIndex).Trim();
        
        // Validate that we extracted a non-empty racer ID
        return !string.IsNullOrEmpty(racerId);
    }
}
