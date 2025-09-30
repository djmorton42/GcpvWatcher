using CsvHelper;
using CsvHelper.Configuration;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Providers;
using System.Globalization;

namespace GcpvWatcher.App.Parsers;

public class GcpvExportParser
{
    private readonly IGcpvExportDataProvider _dataProvider;
    private readonly Dictionary<string, KeyFieldConfig> _keyFields;

    public GcpvExportParser(IGcpvExportDataProvider dataProvider, Dictionary<string, KeyFieldConfig> keyFields)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _keyFields = keyFields ?? throw new ArgumentNullException(nameof(keyFields));
    }

    public async Task<IEnumerable<GcpvRaceData>> ParseAsync()
    {
        var dataRows = await _dataProvider.GetDataRowsAsync();
        var racerData = new List<GcpvRacerRowData>();

        // Parse each row into racer data
        foreach (var row in dataRows)
        {
            var parsedData = ParseRow(row);
            racerData.Add(parsedData);
        }

        // Group by race number
        var groupedRaces = racerData
            .GroupBy(r => r.RaceNumber)
            .Select(group => new GcpvRaceData(
                group.Key,
                group.First().TrackParams,
                group.First().RaceGroup,
                group.First().Stage,
                group
                    .OrderBy(r => int.Parse(r.Lane))
                    .Select(r => new GcpvRacerData(r.Lane, r.Racer, r.Affiliation))
                    .ToList()
            ))
            .ToList();

        return groupedRaces;
    }

    private GcpvRacerRowData ParseRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
            throw new ArgumentException("Row cannot be null or empty.");

        // Parse CSV row
        var columns = ParseCsvRow(row);
        
        // Extract values using key+offset logic
        var trackParams = GetValueByKey(columns, "track_params");
        var raceGroup = GetValueByKey(columns, "race_group");
        var stage = GetValueByKey(columns, "stage");
        var raceNumber = GetValueByKey(columns, "race_number");
        var lane = GetValueByKey(columns, "lane");
        var racer = GetValueByKey(columns, "racer");
        var affiliation = GetValueByKey(columns, "affiliation");

        return new GcpvRacerRowData(
            raceNumber,
            trackParams,
            raceGroup,
            stage,
            lane,
            racer,
            affiliation
        );
    }

    private List<string> ParseCsvRow(string row)
    {
        // Use CsvHelper to parse the row
        using var reader = new StringReader(row);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            BadDataFound = null // Ignore bad data
        });

        try
        {
            var columns = new List<string>();
            
            // Read the first (and only) record
            if (csv.Read())
            {
                var fieldCount = csv.Parser.Count;
                for (int i = 0; i < fieldCount; i++)
                {
                    var field = csv.GetField(i);
                    columns.Add(field ?? string.Empty);
                }
            }
            else
            {
                throw new ArgumentException($"No data found in row: {row}");
            }

            return columns;
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Error parsing GCPV export row: {ex.Message}. Row: {row}", ex);
        }
    }

    private string GetValueByKey(List<string> columns, string fieldName)
    {
        if (!_keyFields.TryGetValue(fieldName, out var keyField))
        {
            throw new ArgumentException($"Key field '{fieldName}' not found in configuration.");
        }

        // Find the column that matches the key (trimmed)
        var keyIndex = -1;
        for (int i = 0; i < columns.Count; i++)
        {
            if (columns[i].Trim().Equals(keyField.Key, StringComparison.OrdinalIgnoreCase))
            {
                keyIndex = i;
                break;
            }
        }

        if (keyIndex == -1)
        {
            throw new ArgumentException($"Key '{keyField.Key}' not found in row for field '{fieldName}'.");
        }

        // Calculate target column index
        var targetIndex = keyIndex + keyField.Offset;

        if (targetIndex >= columns.Count)
        {
            throw new ArgumentException($"Target column index {targetIndex} is out of bounds for field '{fieldName}'. Row has {columns.Count} columns.");
        }

        var value = columns[targetIndex].Trim();
        
        // Apply suffix stop words if configured
        if (keyField.SuffixStopWords != null && keyField.SuffixStopWords.Length > 0)
        {
            value = RemoveSuffixStopWords(value, keyField.SuffixStopWords);
        }

        return value;
    }

    private string RemoveSuffixStopWords(string value, string[] suffixStopWords)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        // Sort stop words by length in descending order
        var sortedStopWords = suffixStopWords
            .Where(word => !string.IsNullOrWhiteSpace(word))
            .OrderByDescending(word => word.Length)
            .ToArray();

        foreach (var stopWord in sortedStopWords)
        {
            if (value.EndsWith(stopWord, StringComparison.Ordinal))
            {
                // Remove the stop word from the end
                value = value.Substring(0, value.Length - stopWord.Length).Trim();
                break; // Only remove the first (longest) match
            }
        }

        return value;
    }
}
