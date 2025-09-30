using GcpvWatcher.App.Models;
using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Comparers;

public class RaceNumberComparer : IComparer<Race>
{
    public int Compare(Race? x, Race? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        var xRaceNumber = ParseRaceNumber(x.RaceNumber);
        var yRaceNumber = ParseRaceNumber(y.RaceNumber);

        // First compare by number
        var numberComparison = xRaceNumber.Number.CompareTo(yRaceNumber.Number);
        if (numberComparison != 0)
            return numberComparison;

        // If numbers are equal, compare by letter
        return string.Compare(xRaceNumber.Letter, yRaceNumber.Letter, StringComparison.Ordinal);
    }

    private static (int Number, string Letter) ParseRaceNumber(string raceNumber)
    {
        var match = Regex.Match(raceNumber, @"^(\d+)([A-Z])$");
        if (!match.Success)
            throw new ArgumentException($"Invalid race number format: {raceNumber}");

        var number = int.Parse(match.Groups[1].Value);
        var letter = match.Groups[2].Value;
        
        return (number, letter);
    }
}
