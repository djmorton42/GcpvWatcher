using System.IO;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Comparers;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace GcpvWatcher.App.Services;

public class EvtFileManager : IDisposable
{
    private readonly string _finishLynxDirectory;
    private readonly string _lynxEvtFilePath;
    private readonly Dictionary<string, List<Race>> _fileRaces; // Maps source file path to races
    private readonly object _lockObject = new object();
    private bool _disposed = false;
    private string _lastFinishLynxDirectory; // Track directory changes

    public EvtFileManager(string finishLynxDirectory)
    {
        _finishLynxDirectory = finishLynxDirectory ?? throw new ArgumentNullException(nameof(finishLynxDirectory));
        _lynxEvtFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt");
        _fileRaces = new Dictionary<string, List<Race>>();
        _lastFinishLynxDirectory = finishLynxDirectory;
        
        // Don't load existing races on startup to avoid duplicates
        // Races will be loaded from EVT file when needed for comparison
        ApplicationLogger.Log("EvtFileManager initialized - will load existing races when needed");
    }

    public async Task<RaceProcessingStats> UpdateRacesFromFileAsync(string sourceFilePath, IEnumerable<Race> races)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            throw new ArgumentException("Source file path cannot be null or empty.", nameof(sourceFilePath));

        var racesList = races?.ToList() ?? new List<Race>();
        var stats = new RaceProcessingStats();

        lock (_lockObject)
        {
            // Check if the FinishLynx directory has changed - if so, clear all state
            if (_lastFinishLynxDirectory != _finishLynxDirectory)
            {
                ApplicationLogger.Log("FinishLynx directory changed - clearing all race state");
                _fileRaces.Clear();
                _lastFinishLynxDirectory = _finishLynxDirectory;
            }
            
            // Load existing races from EVT file if we don't have any in memory yet
            if (_fileRaces.Count == 0)
            {
                LoadExistingRacesFromEvtFile();
            }
            
            // Get all existing races from all source files for comparison
            var allExistingRaces = _fileRaces.Values.SelectMany(races => races).ToList();
            
            // Handle duplicate race numbers by keeping the last occurrence
            var existingRacesDict = new Dictionary<string, Race>();
            foreach (var race in allExistingRaces)
            {
                existingRacesDict[race.RaceNumber] = race; // This will overwrite duplicates
            }
            
            // Handle duplicate race numbers by keeping the last occurrence
            var currentRacesDict = new Dictionary<string, Race>();
            var duplicateRaceNumbers = new HashSet<string>();
            foreach (var race in racesList)
            {
                if (currentRacesDict.ContainsKey(race.RaceNumber))
                {
                    duplicateRaceNumbers.Add(race.RaceNumber);
                    ApplicationLogger.Log($"Duplicate race number found: {race.RaceNumber} - keeping last occurrence");
                }
                currentRacesDict[race.RaceNumber] = race; // This will overwrite duplicates
            }
            
            if (duplicateRaceNumbers.Count > 0)
            {
                ApplicationLogger.Log($"Found {duplicateRaceNumbers.Count} duplicate race numbers in file: {string.Join(", ", duplicateRaceNumbers)}");
            }

            // Calculate statistics by comparing against all existing races
            foreach (var currentRace in racesList)
            {
                if (!existingRacesDict.ContainsKey(currentRace.RaceNumber))
                {
                    stats.RacesAdded++;
                }
                else if (!AreRacesEqual(existingRacesDict[currentRace.RaceNumber], currentRace))
                {
                    stats.RacesUpdated++;
                }
                else
                {
                    stats.RacesUnchanged++;
                }
            }

            // Count races that were removed from this specific file
            var hadPreviousRaces = _fileRaces.ContainsKey(sourceFilePath);
            if (hadPreviousRaces)
            {
                var previousRaces = _fileRaces[sourceFilePath];
                foreach (var previousRace in previousRaces)
                {
                    if (!currentRacesDict.ContainsKey(previousRace.RaceNumber))
                    {
                        stats.RacesRemoved++;
                    }
                }
            }

            // Store races for this source file
            _fileRaces[sourceFilePath] = racesList;
        }

        await WriteAllRacesToEvtFileAsync();
        return stats;
    }

    public async Task RemoveRacesFromFileAsync(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return;

        lock (_lockObject)
        {
            if (_fileRaces.ContainsKey(sourceFilePath))
            {
                _fileRaces.Remove(sourceFilePath);
            }
        }

        await WriteAllRacesToEvtFileAsync();
    }

    private async Task WriteAllRacesToEvtFileAsync()
    {
        List<Race> allRaces;

        lock (_lockObject)
        {
            allRaces = _fileRaces.Values
                .SelectMany(races => races)
                .OrderBy(race => race, new RaceNumberComparer())
                .ToList();
        }

        await WriteRacesToEvtFileAsync(allRaces);
    }

    private async Task WriteRacesToEvtFileAsync(IEnumerable<Race> races)
    {
        var evtContent = GenerateEvtContent(races);
        await File.WriteAllTextAsync(_lynxEvtFilePath, evtContent);
        
        WatcherLogger.Log($"Updated Lynx.evt with {races.Count()} races");
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

    private static bool AreRacesEqual(Race race1, Race race2)
    {
        if (race1.RaceNumber != race2.RaceNumber) return false;
        if (race1.RaceTitle != race2.RaceTitle) return false;
        if (race1.NumberOfLaps != race2.NumberOfLaps) return false;
        
        // Compare racers dictionaries
        if (race1.Racers.Count != race2.Racers.Count) return false;
        
        foreach (var kvp in race1.Racers)
        {
            if (!race2.Racers.TryGetValue(kvp.Key, out var lane) || lane != kvp.Value)
                return false;
        }
        
        return true;
    }

    private void LoadExistingRacesFromEvtFile()
    {
        try
        {
            if (!File.Exists(_lynxEvtFilePath))
            {
                ApplicationLogger.Log("No existing EVT file found, starting with empty race list");
                return;
            }

            // Check if file is empty or very small
            var fileInfo = new FileInfo(_lynxEvtFilePath);
            if (fileInfo.Length < 10) // Less than 10 bytes, likely empty
            {
                ApplicationLogger.Log("EVT file exists but appears to be empty, starting with empty race list");
                return;
            }

            ApplicationLogger.Log("Loading existing races from EVT file...");
            
            // Use a timeout to prevent hanging
            var loadTask = Task.Run(async () =>
            {
                var dataProvider = new EventDataFileProvider(_lynxEvtFilePath);
                var parser = new EvtParser(dataProvider);
                return await parser.ParseAsync();
            });
            
            if (!loadTask.Wait(TimeSpan.FromSeconds(5))) // 5 second timeout
            {
                ApplicationLogger.Log("EVT file parsing timed out, starting with empty race list");
                return;
            }
            
            var existingRaces = loadTask.Result;
            ApplicationLogger.Log("EVT file parsing completed");

            // Group races by a synthetic source file name since we don't know the original source
            // We'll use a special key to represent "unknown source" races
            var unknownSourceKey = "evt_file_races";
            var racesList = existingRaces.ToList();
            
            if (racesList.Count > 0)
            {
                _fileRaces[unknownSourceKey] = racesList;
                ApplicationLogger.Log($"Loaded {racesList.Count} existing races from EVT file");
            }
            else
            {
                ApplicationLogger.Log("EVT file exists but contains no races");
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error loading existing races from EVT file", ex);
            // Continue with empty race list - don't fail startup
        }
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
