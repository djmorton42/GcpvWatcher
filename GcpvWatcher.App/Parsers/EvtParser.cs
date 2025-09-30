using CsvHelper;
using CsvHelper.Configuration;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Comparers;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GcpvWatcher.App.Parsers;

public class EvtParser
{
    private readonly IEventDataProvider _dataProvider;

    public EvtParser(IEventDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<IEnumerable<Race>> ParseAsync()
    {
        var dataRows = await _dataProvider.GetDataRowsAsync();
        var races = new List<Race>();

        var lines = dataRows.ToList();
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            
            // Check if this is a race info line (starts with race number)
            if (IsRaceInfoLine(line))
            {
                var race = ParseRaceFromLines(lines, i);
                races.Add(race);
            }
            else if (IsPotentialRaceInfoLine(line))
            {
                // This looks like a race info line but has invalid format
                throw new ArgumentException($"Invalid race info line format. Line: {line}");
            }
        }

        // Sort races by race number (number first, then letter)
        return races.OrderBy(race => race, new RaceNumberComparer());
    }

    private bool IsRaceInfoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // Check if line starts with a race number pattern (number followed by letter)
        // Handle both quoted and unquoted fields
        var trimmedLine = line.Trim();
        var match = Regex.Match(trimmedLine, @"^""?(\d+[A-Z])""?");
        return match.Success;
    }

    private bool IsPotentialRaceInfoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // Check if this looks like a race info line (has the right number of CSV fields)
        // but doesn't have a valid race number
        var trimmedLine = line.Trim();
        var fields = trimmedLine.Split(',');
        
        // Race info lines should have 13 fields (race number, 2 empty, title, 8 empty, laps)
        return fields.Length >= 13;
    }

    private Race ParseRaceFromLines(List<string> lines, int raceInfoLineIndex)
    {
        var raceInfoLine = lines[raceInfoLineIndex];
        var raceInfo = ParseRaceInfoLine(raceInfoLine);
        
        var racers = new Dictionary<int, int>();
        
        // Parse racer lines that follow the race info line
        for (int i = raceInfoLineIndex + 1; i < lines.Count; i++)
        {
            var line = lines[i];
            
            // If we hit another race info line, we're done with this race
            if (IsRaceInfoLine(line))
                break;
                
            // Parse racer line
            var racerLine = ParseRacerLine(line);
            if (racerLine.HasValue)
            {
                racers[racerLine.Value.RacerId] = racerLine.Value.Lane;
            }
        }

        return new Race(raceInfo.RaceNumber, raceInfo.RaceTitle, raceInfo.NumberOfLaps, racers);
    }

    private (string RaceNumber, string RaceTitle, decimal NumberOfLaps) ParseRaceInfoLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            throw new ArgumentException("Race info line cannot be null or empty.");

        // Use CsvHelper to parse the race info line
        using var reader = new StringReader(line);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null
        });

        try
        {
            var records = csv.GetRecords<RaceInfoCsvRecord>().ToList();
            
            if (records.Count != 1)
            {
                throw new ArgumentException($"Invalid race info line format. Expected 1 record, got {records.Count}. Line: {line}");
            }

            var record = records[0];
            
            // Validate race number format
            var raceNumber = record.RaceNumber.Trim();
            if (string.IsNullOrEmpty(raceNumber) || !Regex.IsMatch(raceNumber, @"^\d+[A-Z]$"))
            {
                throw new ArgumentException($"Invalid race number format: {raceNumber}");
            }

            // Validate number of laps
            if (!decimal.TryParse(record.NumberOfLaps.Trim(), out var numberOfLaps))
            {
                throw new ArgumentException($"Invalid number of laps format: {record.NumberOfLaps}");
            }

            return (raceNumber, record.RaceTitle.Trim(), numberOfLaps);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Error parsing EVT race info line: {ex.Message}. Line: {line}", ex);
        }
    }

    private (int RacerId, int Lane)? ParseRacerLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        // Use CsvHelper to parse the racer line
        using var reader = new StringReader(line);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null
        });

        try
        {
            var records = csv.GetRecords<RacerLineCsvRecord>().ToList();
            
            if (records.Count != 1)
            {
                throw new ArgumentException($"Invalid racer line format. Expected 1 record, got {records.Count}. Line: {line}");
            }

            var record = records[0];
            
            // Validate racer ID
            if (!int.TryParse(record.RacerId.Trim(), out var racerId))
            {
                throw new ArgumentException($"Invalid racer ID format: {record.RacerId}");
            }

            // Validate lane
            if (!int.TryParse(record.Lane.Trim(), out var lane))
            {
                throw new ArgumentException($"Invalid lane format: {record.Lane}");
            }

            return (racerId, lane);
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Error parsing EVT racer line: {ex.Message}. Line: {line}", ex);
        }
    }

}
