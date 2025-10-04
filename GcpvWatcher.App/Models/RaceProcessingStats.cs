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
}
