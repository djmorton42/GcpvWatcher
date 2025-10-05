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
    private bool _hasLoadedExistingRaces = false; // Track if we've already loaded existing races

    public event EventHandler? RacesUpdated;

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

    public IEnumerable<Race> GetAllRaces()
    {
        lock (_lockObject)
        {
            // Get races from CSV files
            var csvRaces = _fileRaces
                .Where(kvp => kvp.Key != "evt_file_races")
                .SelectMany(kvp => kvp.Value)
                .ToList();
            
            // Get races from EVT file (if loaded)
            var evtRaces = _fileRaces.ContainsKey("evt_file_races") 
                ? _fileRaces["evt_file_races"] 
                : new List<Race>();
            
            // Merge races, with CSV races taking precedence over EVT races
            var allRaces = new Dictionary<string, Race>();
            
            // Add EVT races first
            foreach (var race in evtRaces)
            {
                allRaces[race.RaceNumber] = race;
            }
            
            // Add CSV races (this will overwrite any matching EVT races)
            foreach (var race in csvRaces)
            {
                allRaces[race.RaceNumber] = race;
            }
            
            return allRaces.Values
                .OrderBy(race => race, new RaceNumberComparer())
                .ToList();
        }
    }

    /// <summary>
    /// Sets existing races from EVT file without triggering the loading logic
    /// </summary>
    /// <param name="races">The races to set</param>
    public void SetExistingRaces(IEnumerable<Race> races)
    {
        lock (_lockObject)
        {
            var racesList = races?.ToList() ?? new List<Race>();
            _fileRaces["evt_file_races"] = racesList;
            _hasLoadedExistingRaces = true;
        }
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
                _hasLoadedExistingRaces = false; // Reset the flag when directory changes
            }
            
            // Load existing races from EVT file only once, not for every file processed
            if (!_hasLoadedExistingRaces)
            {
                LoadExistingRacesFromEvtFile();
                _hasLoadedExistingRaces = true;
            }
            
            // Get previous races from this specific file for comparison
            var previousFileRaces = _fileRaces.ContainsKey(sourceFilePath) 
                ? _fileRaces[sourceFilePath] 
                : new List<Race>();
            
            // Create dictionary of previous races from this file for comparison
            var existingRacesDict = new Dictionary<string, Race>();
            foreach (var race in previousFileRaces)
            {
                existingRacesDict[race.RaceNumber] = race;
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
                    stats.AddedRaceNumbers.Add(currentRace.RaceNumber);
                }
                else if (!AreRacesEqual(existingRacesDict[currentRace.RaceNumber], currentRace))
                {
                    stats.RacesUpdated++;
                    stats.UpdatedRaceNumbers.Add(currentRace.RaceNumber);
                }
                else
                {
                    stats.RacesUnchanged++;
                    stats.UnchangedRaceNumbers.Add(currentRace.RaceNumber);
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
                        stats.RemovedRaceNumbers.Add(previousRace.RaceNumber);
                    }
                }
            }

            // Store races for this source file
            _fileRaces[sourceFilePath] = racesList;
            
            // Update the EVT races in memory with the merged results
            // This ensures that subsequent comparisons are against the updated EVT races
            var updatedEvtRaces = new Dictionary<string, Race>();
            
            // Start with existing EVT races
            var currentEvtRaces = _fileRaces.ContainsKey("evt_file_races") 
                ? _fileRaces["evt_file_races"] 
                : new List<Race>();
            
            foreach (var race in currentEvtRaces)
            {
                updatedEvtRaces[race.RaceNumber] = race;
            }
            
            // Add/update with CSV races
            foreach (var race in racesList)
            {
                updatedEvtRaces[race.RaceNumber] = race;
            }
            
            // Update the EVT races in memory
            _fileRaces["evt_file_races"] = updatedEvtRaces.Values.ToList();
        }

        await WriteAllRacesToEvtFileAsync();
        
        // Notify that races have been updated
        RacesUpdated?.Invoke(this, EventArgs.Empty);
        
        return stats;
    }

    public async Task CleanupOrphanedRacesAsync(IEnumerable<string> activeSourceFiles)
    {
        var activeFiles = activeSourceFiles.ToHashSet();
        var stats = new RaceProcessingStats();

        lock (_lockObject)
        {
            // Get all races from active CSV files
            var activeRaces = _fileRaces
                .Where(kvp => activeFiles.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToDictionary(race => race.RaceNumber, race => race);

            // Get races from EVT file
            var evtRaces = _fileRaces.ContainsKey("evt_file_races") 
                ? _fileRaces["evt_file_races"] 
                : new List<Race>();

            // Find races in EVT that are not in any active CSV files
            var orphanedRaces = evtRaces
                .Where(race => !activeRaces.ContainsKey(race.RaceNumber))
                .ToList();

            if (orphanedRaces.Count > 0)
            {
                ApplicationLogger.Log($"Found {orphanedRaces.Count} orphaned races to remove: {string.Join(", ", orphanedRaces.Select(r => r.RaceNumber))}");
                
                // Remove orphaned races from EVT races
                var updatedEvtRaces = evtRaces
                    .Where(race => activeRaces.ContainsKey(race.RaceNumber))
                    .ToList();
                
                _fileRaces["evt_file_races"] = updatedEvtRaces;
                stats.RacesRemoved = orphanedRaces.Count;
                stats.RemovedRaceNumbers.AddRange(orphanedRaces.Select(r => r.RaceNumber));
            }
        }

        if (stats.RacesRemoved > 0)
        {
            // Log the removal to WatcherLogger
            WatcherLogger.Log($"Cleaned up orphaned races: {stats.GetDetailedString()}");
            
            // Write updated races to EVT file
            await WriteAllRacesToEvtFileAsync();
            
            // Notify that races have been updated
            RacesUpdated?.Invoke(this, EventArgs.Empty);
        }
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
        
        // Notify that races have been updated
        RacesUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task WriteAllRacesToEvtFileAsync()
    {
        // Use the same deduplication logic as GetAllRaces()
        var allRaces = GetAllRaces().ToList();
        await WriteRacesToEvtFileAsync(allRaces);
    }

    private async Task WriteRacesToEvtFileAsync(IEnumerable<Race> races)
    {
        var evtContent = GenerateEvtContent(races);
        await File.WriteAllTextAsync(_lynxEvtFilePath, evtContent);
        
        //WatcherLogger.Log($"Updated Lynx.evt with {races.Count()} races");
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
                WatcherLogger.Log($"Loaded {racesList.Count} existing races from EVT file");
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
