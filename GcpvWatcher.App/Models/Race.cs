namespace GcpvWatcher.App.Models;

public record Race(string RaceNumber, string RaceTitle, decimal NumberOfLaps, Dictionary<string, int> Racers)
{
    /// <summary>
    /// Last name, first name, and affiliation from an evt row, keyed by racer id.
    /// Absent when that row did not include any of those fields.
    /// </summary>
    public Dictionary<string, KeepRacerName> RacerNames { get; init; } = new();

    public bool IsFromKeepFile { get; init; } = false;

    /// <summary>
    /// True when this race was built by merging single-racer races into the previous race (Consolidate Single-Racer Races).
    /// </summary>
    public bool IsConsolidated { get; init; } = false;

    /// <summary>
    /// Set when a forced track length does not produce a whole or half lap.
    /// Shown on the race list. Not written to Lynx.evt.
    /// </summary>
    public string? TrackWarning { get; init; }

    public bool HasTrackWarning => !string.IsNullOrEmpty(TrackWarning);

    /// <summary>
    /// Set when a forced track length disagrees with the track named in the file,
    /// and the forced lap count is still a whole or half lap.
    /// Shown on the race list, milder than <see cref="TrackWarning"/>. Not written to Lynx.evt.
    /// </summary>
    public string? TrackCaution { get; init; }

    public bool HasTrackCaution => !string.IsNullOrEmpty(TrackCaution);

    /// <summary>
    /// True when a saved operator edit replaced the calculated name, laps, or both.
    /// </summary>
    public bool IsOverridden { get; init; }

    public override string ToString()
    {
        var racersString = string.Join(", ", Racers.OrderBy(kvp => kvp.Value).Select(kvp => $"Racer {kvp.Key} in Lane {kvp.Value}"));
        return $"Race {RaceNumber}: {RaceTitle} ({NumberOfLaps} laps) - {racersString}";
    }
}
