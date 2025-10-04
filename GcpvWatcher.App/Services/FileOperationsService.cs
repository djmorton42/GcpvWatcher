using System.IO;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Comparers;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace GcpvWatcher.App.Services;

public class FileOperationsService
{
    private const string LynxEvtFileName = "Lynx.evt";
    
    /// <summary>
    /// Checks if a Lynx.evt file exists in the specified directory
    /// </summary>
    /// <param name="directoryPath">The directory path to check</param>
    /// <returns>True if the file exists, false otherwise</returns>
    public bool LynxEvtFileExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;
            
        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        return File.Exists(filePath);
    }
    
    /// <summary>
    /// Creates a new Lynx.evt file in the specified directory with the standard template
    /// </summary>
    /// <param name="directoryPath">The directory path where to create the file</param>
    /// <returns>The full path to the created file</returns>
    /// <exception cref="ArgumentException">Thrown when directoryPath is null or empty</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when there's no permission to create the file</exception>
    public string CreateLynxEvtFile(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));
            
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory does not exist: {directoryPath}");
            
        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        
        // Create the template content
        var templateContent = GetLynxEvtTemplate();
        
        // Write the file
        File.WriteAllText(filePath, templateContent);
        
        return filePath;
    }
    
    /// <summary>
    /// Gets the standard Lynx.evt file template content
    /// </summary>
    /// <returns>The template content as a string</returns>
    private static string GetLynxEvtTemplate()
    {
        return string.Empty;
    }

    /// <summary>
    /// Reads all races from the Lynx.evt file
    /// </summary>
    /// <param name="directoryPath">The directory containing the Lynx.evt file</param>
    /// <returns>A collection of races from the file</returns>
    public async Task<IEnumerable<Race>> ReadRacesFromLynxEvtAsync(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        
        if (!File.Exists(filePath))
        {
            return Enumerable.Empty<Race>();
        }

        var dataProvider = new EventDataFileProvider(filePath);
        var parser = new EvtParser(dataProvider);
        
        return await parser.ParseAsync();
    }

    /// <summary>
    /// Writes races to the Lynx.evt file
    /// </summary>
    /// <param name="directoryPath">The directory containing the Lynx.evt file</param>
    /// <param name="races">The races to write</param>
    /// <returns>Statistics about the race processing</returns>
    public async Task<RaceProcessingStats> WriteRacesToLynxEvtAsync(string directoryPath, IEnumerable<Race> races)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

        var filePath = Path.Combine(directoryPath, LynxEvtFileName);
        var evtContent = GenerateEvtContent(races);
        
        await File.WriteAllTextAsync(filePath, evtContent);
        
        // Return basic stats for direct file writing (all races are considered added)
        return new RaceProcessingStats
        {
            RacesAdded = races.Count(),
            RacesUpdated = 0,
            RacesUnchanged = 0,
            RacesRemoved = 0
        };
    }

    private string GenerateEvtContent(IEnumerable<Race> races)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            TrimOptions = TrimOptions.None
        });

        foreach (var race in races.OrderBy(race => race, new RaceNumberComparer()))
        {
            // Write race info line
            WriteRaceInfoLine(csv, race);

            // Write racer lines
            WriteRacerLines(csv, race);
        }

        return writer.ToString() + Environment.NewLine;
    }

    private void WriteRaceInfoLine(CsvWriter csv, Race race)
    {
        var record = new RaceInfoCsvRecord
        {
            RaceNumber = race.RaceNumber,
            Field1 = "",
            Field2 = "",
            RaceTitle = race.RaceTitle,
            Field4 = "",
            Field5 = "",
            Field6 = "",
            Field7 = "",
            Field8 = "",
            Field9 = "",
            Field10 = "",
            Field11 = "",
            NumberOfLaps = race.NumberOfLaps.ToString()
        };

        csv.WriteRecord(record);
        csv.NextRecord();
    }

    private void WriteRacerLines(CsvWriter csv, Race race)
    {
        foreach (var racer in race.Racers.OrderBy(kvp => kvp.Value)) // Order by lane
        {
            var record = new RacerLineCsvRecord
            {
                Field1 = "",
                RacerId = racer.Key.ToString(),
                Lane = racer.Value.ToString()
            };

            csv.WriteRecord(record);
            csv.NextRecord();
        }
    }
}
