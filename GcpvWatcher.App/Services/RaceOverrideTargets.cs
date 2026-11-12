using GcpvWatcher.App.Comparers;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Consolidated cards such as 37CD are built from source races 37C and 37D.
/// Overrides are stored on those source numbers so they survive the next parse.
/// </summary>
public static class RaceOverrideTargets
{
    public static string TitleTarget(string raceNumber) => Components(raceNumber)[0];

    public static IReadOnlyList<string> LapTargets(string raceNumber) => Components(raceNumber);

    public static IReadOnlyList<string> ClearTargets(string raceNumber) => Components(raceNumber);

    public static IReadOnlyList<string> Components(string raceNumber)
    {
        var (number, letters) = RaceNumberComparer.ParseRaceNumber(raceNumber);
        if (letters.Length <= 1)
            return [raceNumber];

        return letters.Select(letter => number.ToString() + letter).ToList();
    }
}
