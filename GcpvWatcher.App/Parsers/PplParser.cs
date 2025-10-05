using CsvHelper;
using CsvHelper.Configuration;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Services;
using System.Globalization;

namespace GcpvWatcher.App.Parsers;

public class PplParser
{
    private readonly IPeopleDataProvider _dataProvider;

    public PplParser(IPeopleDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async Task<Dictionary<int, Racer>> ParseAsync()
    {
        var dataRows = await _dataProvider.GetDataRowsAsync();
        var racers = new Dictionary<int, Racer>();

        foreach (var row in dataRows)
        {
            var racer = ParseRacerRow(row);
            if (racer != null)
            {
                racers[racer.RacerId] = racer;
            }
        }

        return racers;
    }

    private Racer? ParseRacerRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
            return null;

        // Use CsvHelper to parse the racer row
        using var reader = new StringReader(row);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.Trim | TrimOptions.InsideQuotes,
            IgnoreBlankLines = true,
            MissingFieldFound = null
        });

        try
        {
            var records = csv.GetRecords<PplRacerCsvRecord>().ToList();
            
            if (records.Count != 1)
            {
                WatcherLogger.Log("Warning: Problem parsing PPL file");
                return null;
            }

            var record = records[0];
            
            // Validate racer ID
            if (!int.TryParse(record.RacerId.Trim(), out var racerId))
            {
                WatcherLogger.Log("Warning: Problem parsing PPL file");
                return null;
            }

            // Validate that we have the required fields
            var lastName = record.LastName.Trim();
            var firstName = record.FirstName.Trim();
            var affiliation = record.Affiliation.Trim();

            if (string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(affiliation))
            {
                WatcherLogger.Log("Warning: Problem parsing PPL file");
                return null;
            }

            return new Racer(racerId, lastName, firstName, affiliation);
        }
        catch (Exception)
        {
            WatcherLogger.Log("Warning: Problem parsing PPL file");
            return null;
        }
    }
}