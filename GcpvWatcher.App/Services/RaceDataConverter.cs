using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

public class RaceDataConverter
{
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
        // Calculate number of laps using LapCalculator
        var numberOfLaps = LapCalculator.CalculateLaps(gcpvRace.TrackParams);
        
        if (numberOfLaps <= 0)
        {
            throw new InvalidOperationException($"Could not determine number of laps for race {gcpvRace.RaceNumber} with track params: {gcpvRace.TrackParams}");
        }

        // Create event title in format "RaceGroup (Track Params) Stage"
        var eventTitle = $"{gcpvRace.RaceGroup} ({gcpvRace.TrackParams}) {gcpvRace.Stage}";

        // Convert racers to the format expected by Race object
        // Race object expects Dictionary<int, int> where key is racer ID and value is lane
        var racers = new Dictionary<int, int>();
        
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

        return new Race(gcpvRace.RaceNumber, eventTitle, (decimal)numberOfLaps, racers);
    }

    private static bool TryParseRacerId(string racerField, out int racerId)
    {
        racerId = 0;
        
        if (string.IsNullOrWhiteSpace(racerField))
            return false;

        // Parse format: "{racer_id} {last_name},{first_name}"
        // Find the first space to separate racer_id from the name
        var firstSpaceIndex = racerField.IndexOf(' ');
        if (firstSpaceIndex <= 0)
            return false;

        // Extract the racer ID (everything before the first space)
        var racerIdString = racerField.Substring(0, firstSpaceIndex).Trim();
        
        return int.TryParse(racerIdString, out racerId);
    }
}
