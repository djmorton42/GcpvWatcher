using CsvHelper;
using CsvHelper.Configuration;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Providers;
using System.Globalization;

namespace GcpvWatcher.App.Parsers;

public class PplParser
{
    private readonly IPeopleDataProvider _dataProvider;

    public PplParser(IPeopleDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<IEnumerable<Racer>> ParseAsync()
    {
        var dataRows = await _dataProvider.GetDataRowsAsync();
        var racers = new List<Racer>();

        foreach (var row in dataRows)
        {
            try
            {
                var racer = ParseRacerFromRow(row);
                if (racer != null)
                {
                    racers.Add(racer);
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other rows
                // In a real application, you might want to use a proper logging framework
                Console.WriteLine($"Error parsing row '{row}': {ex.Message}");
            }
        }

        return racers;
    }

    private Racer? ParseRacerFromRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
            return null;

        // Use CsvHelper to parse the row
        using var reader = new StringReader(row);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null // Don't throw on missing fields, just ignore them
        });

        try
        {
            var records = csv.GetRecords<RacerCsvRecord>().ToList();
            
            if (records.Count != 1)
            {
                throw new ArgumentException($"Invalid row format. Expected 1 record, got {records.Count}. Row: {row}");
            }

            var record = records[0];
            
            // Validate and convert RacerId
            if (!int.TryParse(record.RacerId.Trim(), out var racerId))
            {
                throw new ArgumentException($"Invalid racer ID format: {record.RacerId}");
            }

            return new Racer(
                racerId,
                record.LastName.Trim(),
                record.FirstName.Trim(),
                record.Affiliation.Trim()
            );
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new ArgumentException($"Error parsing CSV row: {ex.Message}. Row: {row}", ex);
        }
    }
}
