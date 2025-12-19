using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

public static class RacerDataService
{
    private static Dictionary<string, Racer> _racers = new Dictionary<string, Racer>();

    public static void UpdateRacers(Dictionary<string, Racer> racers)
    {
        _racers = racers ?? new Dictionary<string, Racer>();
    }

    public static Dictionary<string, Racer> GetRacers()
    {
        return _racers;
    }
}
