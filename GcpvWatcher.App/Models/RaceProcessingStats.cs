namespace GcpvWatcher.App.Models;

/// <summary>
/// Statistics about race processing operations
/// </summary>
public class RaceProcessingStats
{
    public int RacesAdded { get; set; }
    public int RacesUpdated { get; set; }
    public int RacesUnchanged { get; set; }
    public int RacesRemoved { get; set; }

    public List<string> AddedRaceNumbers { get; set; } = new List<string>();
    public List<string> UpdatedRaceNumbers { get; set; } = new List<string>();
    public List<string> UnchangedRaceNumbers { get; set; } = new List<string>();
    public List<string> RemovedRaceNumbers { get; set; } = new List<string>();

    public int TotalProcessed => RacesAdded + RacesUpdated + RacesUnchanged;

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (RacesAdded > 0) parts.Add($"{RacesAdded} added");
        if (RacesUpdated > 0) parts.Add($"{RacesUpdated} updated");
        if (RacesUnchanged > 0) parts.Add($"{RacesUnchanged} unchanged");
        if (RacesRemoved > 0) parts.Add($"{RacesRemoved} removed");
        
        return parts.Count > 0 ? string.Join(", ", parts) : "no races";
    }

    public string GetDetailedString()
    {
        var parts = new List<string>();
        
        if (RacesAdded > 0) 
        {
            var raceNumbers = AddedRaceNumbers.Count > 0 ? $" ({string.Join(", ", AddedRaceNumbers)})" : "";
            parts.Add($"{RacesAdded} added{raceNumbers}");
        }
        if (RacesUpdated > 0) 
        {
            var raceNumbers = UpdatedRaceNumbers.Count > 0 ? $" ({string.Join(", ", UpdatedRaceNumbers)})" : "";
            parts.Add($"{RacesUpdated} updated{raceNumbers}");
        }
        if (RacesUnchanged > 0) 
        {
            var raceNumbers = UnchangedRaceNumbers.Count > 0 ? $" ({string.Join(", ", UnchangedRaceNumbers)})" : "";
            parts.Add($"{RacesUnchanged} unchanged{raceNumbers}");
        }
        if (RacesRemoved > 0) 
        {
            var raceNumbers = RemovedRaceNumbers.Count > 0 ? $" ({string.Join(", ", RemovedRaceNumbers)})" : "";
            parts.Add($"{RacesRemoved} removed{raceNumbers}");
        }
        
        return parts.Count > 0 ? string.Join(", ", parts) : "no races";
    }
}
