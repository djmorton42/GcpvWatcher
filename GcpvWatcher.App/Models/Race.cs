namespace GcpvWatcher.App.Models;

public record Race(string RaceNumber, string RaceTitle, decimal NumberOfLaps, Dictionary<int, int> Racers)
{
    public override string ToString()
    {
        var racersString = string.Join(", ", Racers.OrderBy(kvp => kvp.Value).Select(kvp => $"Racer {kvp.Key} in Lane {kvp.Value}"));
        return $"Race {RaceNumber}: {RaceTitle} ({NumberOfLaps} laps) - {racersString}";
    }
}
