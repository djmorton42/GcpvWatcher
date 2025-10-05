using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

public static class RacerDataService
{
    private static Dictionary<int, Racer> _racers = new Dictionary<int, Racer>();

    public static void UpdateRacers(Dictionary<int, Racer> racers)
    {
        _racers = racers ?? new Dictionary<int, Racer>();
    }

    public static Dictionary<int, Racer> GetRacers()
    {
        return _racers;
    }
}
