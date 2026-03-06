namespace GcpvWatcher.App.Models;

public record Race(string RaceNumber, string RaceTitle, decimal NumberOfLaps, Dictionary<string, int> Racers)
{
    public bool IsFromKeepFile { get; init; } = false;

    /// <summary>
    /// True when this race was built by merging single-racer races into the previous race (Consolidate Single-Racer Races).
    /// </summary>
    public bool IsConsolidated { get; init; } = false;

    public override string ToString()
    {
        var racersString = string.Join(", ", Racers.OrderBy(kvp => kvp.Value).Select(kvp => $"Racer {kvp.Key} in Lane {kvp.Value}"));
        return $"Race {RaceNumber}: {RaceTitle} ({NumberOfLaps} laps) - {racersString}";
    }
}
