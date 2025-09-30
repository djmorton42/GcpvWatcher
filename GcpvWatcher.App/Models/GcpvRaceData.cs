namespace GcpvWatcher.App.Models;

public record GcpvRaceData(
    string RaceNumber,
    string TrackParams,
    string RaceGroup,
    string Stage,
    List<GcpvRacerData> Racers
)
{
    public override string ToString()
    {
        return $"Race {RaceNumber}: {TrackParams} {RaceGroup} {Stage} - {Racers.Count} racers";
    }
}
