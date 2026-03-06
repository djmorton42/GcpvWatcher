using System;
using System.IO;
using System.Text;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Comparers;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Threading;

namespace GcpvWatcher.App.Services;

public class EvtFileManager : IDisposable
{
    private readonly string _finishLynxDirectory;
    private readonly string _lynxEvtFilePath;
    private readonly string _lynxEvtKeepFilePath;
    private readonly Dictionary<string, List<Race>> _fileRaces; // Maps source file path to races
    private readonly object _lockObject = new object();
    private bool _disposed = false;
    private string _lastFinishLynxDirectory; // Track directory changes
    private bool _hasLoadedExistingRaces = false; // Track if we've already loaded existing races
    private bool _hasLoadedKeepRaces = false; // Track if we've loaded keep races
    private readonly AppConfig _config;

    public event EventHandler? RacesUpdated;

    public EvtFileManager(string finishLynxDirectory, AppConfig config)
    {
        _finishLynxDirectory = finishLynxDirectory ?? throw new ArgumentNullException(nameof(finishLynxDirectory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _lynxEvtFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt");
        _lynxEvtKeepFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt.keep");
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
            // Load keep races if not already loaded
            if (!_hasLoadedKeepRaces)
            {
                LoadKeepRaces();
                _hasLoadedKeepRaces = true;
            }
            
            // Get races from keep file (highest precedence)
            var keepRaces = _fileRaces.ContainsKey("evt_keep_races") 
                ? _fileRaces["evt_keep_races"] 
                : new List<Race>();
            
            // Get races from CSV files
            var csvRaces = _fileRaces
                .Where(kvp => kvp.Key != "evt_file_races" && kvp.Key != "evt_keep_races")
                .SelectMany(kvp => kvp.Value)
                .ToList();
            
            // Get races from EVT file (if loaded)
            var evtRaces = _fileRaces.ContainsKey("evt_file_races") 
                ? _fileRaces["evt_file_races"] 
                : new List<Race>();
            
            // Merge races with precedence: Keep > CSV > EVT
            var allRaces = new Dictionary<string, Race>();
            
            // Add EVT races first (lowest precedence)
            foreach (var race in evtRaces)
            {
                allRaces[race.RaceNumber] = race;
            }
            
            // Add CSV races (overwrites EVT races)
            foreach (var race in csvRaces)
            {
                allRaces[race.RaceNumber] = race;
            }
            
            // Add keep races last (highest precedence - overwrites CSV and EVT races)
            foreach (var race in keepRaces)
            {
                // Mark keep races with IsFromKeepFile flag
                allRaces[race.RaceNumber] = race with { IsFromKeepFile = true };
            }
            
            return allRaces.Values
                .OrderBy(race => race, new RaceNumberComparer())
                .ToList();
        }
    }

    /// <summary>
    /// Returns the races that should be shown in the UI and written to Lynx.evt.
    /// When ConsolidateSingleRacerRaces is enabled, single-racer races are merged into the previous race in the same series.
    /// </summary>
    /// <param name="onConsolidate">Optional callback when a race is consolidated (singleRaceNumber, previousRaceNumber, newCombinedNumber). Used to log only when a source file change triggers the write.</param>
    public IEnumerable<Race> GetRacesForDisplay(Action<string, string, string>? onConsolidate = null)
    {
        lock (_lockObject)
        {
            var allRaces = GetAllRaces().ToList();
            if (_config.ConsolidateSingleRacerRaces)
            {
                // Drop component races when their consolidated form exists so we don't output 37C, 37CD and 37D.
                allRaces = RemoveComponentRacesWhenConsolidatedPresent(allRaces);
                allRaces = ConsolidateSingleRacerRaces(allRaces, onConsolidate).ToList();
            }
            else
            {
                // When consolidation is off: drop consolidated races when we already have all components (avoid duplicates),
                // then expand any remaining consolidated races so toggling the checkbox changes the output.
                allRaces = RemoveConsolidatedRacesWhenComponentsPresent(allRaces);
                allRaces = ExpandConsolidatedRaces(allRaces).ToList();
            }
            return allRaces;
        }
    }

    /// <summary>
    /// When we have both a consolidated race (e.g. 37CD) and its components (37C, 37D) from different sources,
    /// remove the component races so we don't output 37C and 37CD separately.
    /// </summary>
    private static List<Race> RemoveComponentRacesWhenConsolidatedPresent(List<Race> races)
    {
        var componentRaceNumbers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var race in races)
        {
            var (num, letters) = RaceNumberComparer.ParseRaceNumber(race.RaceNumber);
            if (letters.Length > 1)
            {
                foreach (var letter in letters)
                    componentRaceNumbers.Add(num.ToString() + letter);
            }
        }
        if (componentRaceNumbers.Count == 0)
            return races;
        return races.Where(r => !componentRaceNumbers.Contains(r.RaceNumber)).ToList();
    }

    /// <summary>
    /// When consolidation is off: remove consolidated races (e.g. 37CD) when all their components (37C, 37D) are already in the list,
    /// so we don't output 37C, 37D and also an expanded 37C, 37D from 37CD.
    /// </summary>
    private static List<Race> RemoveConsolidatedRacesWhenComponentsPresent(List<Race> races)
    {
        var raceNumbers = races.Select(r => r.RaceNumber).ToHashSet(StringComparer.Ordinal);
        return races.Where(race =>
        {
            var (num, letters) = RaceNumberComparer.ParseRaceNumber(race.RaceNumber);
            if (letters.Length <= 1) return true;
            foreach (var letter in letters)
            {
                if (!raceNumbers.Contains(num.ToString() + letter))
                    return true;
            }
            return false;
        }).ToList();
    }

    /// <summary>
    /// Expands consolidated races (e.g. 37CD) into component races (37C, 37D) by splitting racers by lane.
    /// Last letter gets the last lane (1 racer), previous letters get 1 racer each from the end, first letter gets the rest.
    /// </summary>
    private static IEnumerable<Race> ExpandConsolidatedRaces(List<Race> races)
    {
        var result = new List<Race>();
        var comparer = new RaceNumberComparer();
        foreach (var race in races.OrderBy(r => r, comparer))
        {
            var (num, letters) = RaceNumberComparer.ParseRaceNumber(race.RaceNumber);
            if (letters.Length <= 1)
            {
                result.Add(race);
                continue;
            }
            var orderedRacers = race.Racers.OrderBy(kvp => kvp.Value).ToList();
            var n = letters.Length;
            if (orderedRacers.Count < n)
            {
                result.Add(race);
                continue;
            }
            var start = 0;
            for (var i = 0; i < n; i++)
            {
                var count = i == 0 ? orderedRacers.Count - (n - 1) : 1;
                var segment = orderedRacers.Skip(start).Take(count).ToList();
                start += count;
                var componentNumber = num.ToString() + letters[i];
                var racersByLane = new Dictionary<string, int>();
                for (var j = 0; j < segment.Count; j++)
                    racersByLane[segment[j].Key] = j + 1;
                result.Add(race with { RaceNumber = componentNumber, Racers = racersByLane });
            }
        }
        return result;
    }

    /// <summary>
    /// Rewrites the Lynx.evt file with the current races and current consolidation setting.
    /// Call this when the consolidation option changes so the file on disk matches the display.
    /// </summary>
    public void RefreshEvtFile()
    {
        lock (_lockObject)
        {
            WriteAllRacesToEvtFile();
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

    public RaceProcessingStats UpdateRacesFromFile(string sourceFilePath, IEnumerable<Race> races)
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
                _hasLoadedKeepRaces = false; // Reset keep races flag when directory changes
            }
            
            // Reload keep races if not loaded (they should always be fresh)
            if (!_hasLoadedKeepRaces)
            {
                LoadKeepRaces();
                _hasLoadedKeepRaces = true;
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
            
            // Write to EVT file within the same lock to prevent lost updates
            // If this fails, the caller will remove the file from hash tracking
            // so it will be re-processed on the next polling cycle
            var raceNumbersFromThisFile = racesList.Select(r => r.RaceNumber).ToHashSet();
            WriteAllRacesToEvtFile(logConsolidation: true, raceNumbersFromChangedFile: raceNumbersFromThisFile);
        }

        // Notify that races have been updated
        RacesUpdated?.Invoke(this, EventArgs.Empty);

        return stats;
    }

    /// <summary>
    /// Returns true if the race is in active or keep, or (for consolidated races like 37CD) all component races (37C, 37D) are in active.
    /// </summary>
    private static bool IsRaceCoveredByActiveOrKeep(string raceNumber, Dictionary<string, Race> activeRaces, HashSet<string> keepRaceNumbers)
    {
        if (activeRaces.ContainsKey(raceNumber) || keepRaceNumbers.Contains(raceNumber))
            return true;
        var (num, letters) = RaceNumberComparer.ParseRaceNumber(raceNumber);
        if (letters.Length <= 1)
            return false;
        foreach (var letter in letters)
        {
            var component = num.ToString() + letter;
            if (!activeRaces.ContainsKey(component))
                return false;
        }
        return true;
    }

    public void CleanupOrphanedRaces(IEnumerable<string> activeSourceFiles)
    {
        var activeFiles = activeSourceFiles.ToHashSet();
        var stats = new RaceProcessingStats();

        lock (_lockObject)
        {
            // Ensure keep races are loaded
            if (!_hasLoadedKeepRaces)
            {
                LoadKeepRaces();
                _hasLoadedKeepRaces = true;
            }
            
            // Get all races from active CSV files
            var activeRaces = _fileRaces
                .Where(kvp => activeFiles.Contains(kvp.Key))
                .SelectMany(kvp => kvp.Value)
                .ToDictionary(race => race.RaceNumber, race => race);

            // Get keep races (these should never be considered orphaned)
            var keepRaces = _fileRaces.ContainsKey("evt_keep_races") 
                ? _fileRaces["evt_keep_races"] 
                : new List<Race>();
            var keepRaceNumbers = keepRaces.Select(r => r.RaceNumber).ToHashSet();

            // Get races from EVT file
            var evtRaces = _fileRaces.ContainsKey("evt_file_races") 
                ? _fileRaces["evt_file_races"] 
                : new List<Race>();

            // Find races in EVT that are not in any active CSV files AND not in keep file.
            // Keep races should never be considered orphaned.
            // Consolidated races (e.g. 37CD) are not orphaned when all their component races (37C, 37D) are in active CSVs.
            var orphanedRaces = evtRaces
                .Where(race => !IsRaceCoveredByActiveOrKeep(race.RaceNumber, activeRaces, keepRaceNumbers))
                .ToList();

            if (orphanedRaces.Count > 0)
            {
                ApplicationLogger.Log($"Found {orphanedRaces.Count} orphaned races to remove: {string.Join(", ", orphanedRaces.Select(r => r.RaceNumber))}");
                
                // Remove orphaned races from EVT races
                // Keep races that are in active CSV files, in keep file, or are consolidated and all components are active
                var updatedEvtRaces = evtRaces
                    .Where(race => IsRaceCoveredByActiveOrKeep(race.RaceNumber, activeRaces, keepRaceNumbers))
                    .ToList();
                
                _fileRaces["evt_file_races"] = updatedEvtRaces;
                stats.RacesRemoved = orphanedRaces.Count;
                stats.RemovedRaceNumbers.AddRange(orphanedRaces.Select(r => r.RaceNumber));
                
                // Write updated races to EVT file within the same lock
                WriteAllRacesToEvtFile();
            }
        }

        if (stats.RacesRemoved > 0)
        {
            // Log the removal to WatcherLogger
            WatcherLogger.Log($"Cleaned up orphaned races: {stats.GetDetailedString()}");
            
            // Notify that races have been updated
            RacesUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RemoveRacesFromFile(string sourceFilePath)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
            return;

        lock (_lockObject)
        {
            if (_fileRaces.ContainsKey(sourceFilePath))
            {
                _fileRaces.Remove(sourceFilePath);
                
                // Write updated races to EVT file within the same lock
                WriteAllRacesToEvtFile();
            }
        }
        
        // Notify that races have been updated
        RacesUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void WriteAllRacesToEvtFile(bool logConsolidation = false, IReadOnlySet<string>? raceNumbersFromChangedFile = null)
    {
        Action<string, string, string>? onConsolidate = null;
        if (logConsolidation && raceNumbersFromChangedFile != null)
        {
            onConsolidate = (single, into, combined) =>
            {
                if (raceNumbersFromChangedFile.Contains(single) || raceNumbersFromChangedFile.Contains(into))
                {
                    ApplicationLogger.Log($"Consolidated race {single} (1 racer) into {into} -> {combined}");
                    WatcherLogger.Log($"Consolidated race {single} into {into} -> {combined}");
                }
            };
        }
        var racesToWrite = GetRacesForDisplay(onConsolidate).ToList();
        WriteRacesToEvtFile(racesToWrite);
    }

    /// <summary>
    /// Merges single-racer races into the previous race in the same numerical series.
    /// The previous race's event number becomes combined (e.g. 15B + 15C -> 15BC); the single-racer race is not emitted.
    /// </summary>
    /// <param name="onConsolidate">Optional callback when a race is consolidated (singleRaceNumber, previousRaceNumber, newCombinedNumber).</param>
    private static IEnumerable<Race> ConsolidateSingleRacerRaces(IEnumerable<Race> races, Action<string, string, string>? onConsolidate = null)
    {
        var comparer = new RaceNumberComparer();
        var ordered = races.OrderBy(r => r, comparer).ToList();
        var result = new List<Race>();

        foreach (var race in ordered)
        {
            if (race.Racers.Count != 1)
            {
                result.Add(race);
                continue;
            }

            if (result.Count == 0)
            {
                result.Add(race);
                continue;
            }

            var last = result[result.Count - 1];
            var (num, letter) = RaceNumberComparer.ParseRaceNumber(race.RaceNumber);
            var (lastNum, lastLetters) = RaceNumberComparer.ParseRaceNumber(last.RaceNumber);

            if (num != lastNum)
            {
                result.Add(race);
                continue;
            }

            var newLane = last.Racers.Values.Max() + 1;
            var singleRacer = race.Racers.Single();
            var newRacers = new Dictionary<string, int>(last.Racers) { [singleRacer.Key] = newLane };
            // Avoid duplicating the letter when the single-racer race is already included (e.g. 37CD + 37D -> 37CD not 37CDD)
            var newLetters = lastLetters.EndsWith(letter, StringComparison.Ordinal) ? lastLetters : lastLetters + letter;
            var newRaceNumber = num.ToString() + newLetters;
            onConsolidate?.Invoke(race.RaceNumber, last.RaceNumber, newRaceNumber);
            result[result.Count - 1] = last with
            {
                RaceNumber = newRaceNumber,
                Racers = newRacers
            };
        }

        return result;
    }

    private void CreateBackupIfEvtFileExists()
    {
        if (!File.Exists(_lynxEvtFilePath))
        {
            return; // No existing EVT file to backup
        }

        try
        {
            // Double-check file still exists
            if (!File.Exists(_lynxEvtFilePath))
            {
                return; // File was deleted
            }

            // Create backup directory if it doesn't exist
            var backupDirectory = Path.Combine(_finishLynxDirectory, _config.EvtBackupDirectory);
            if (!Directory.Exists(backupDirectory))
            {
                Directory.CreateDirectory(backupDirectory);
                ApplicationLogger.Log($"Created backup directory: {backupDirectory}");
            }

            // Generate timestamp in YYYYMMDD_HHMMSS format
            var timestamp = GetCurrentTimestamp();
            var baseFileName = $"Lynx.evt.{timestamp}";
            var backupFilePath = Path.Combine(backupDirectory, baseFileName);

            // If file already exists, add a unique suffix
            var counter = 1;
            while (File.Exists(backupFilePath))
            {
                var fileNameWithSuffix = $"{baseFileName}.{counter}";
                backupFilePath = Path.Combine(backupDirectory, fileNameWithSuffix);
                counter++;
            }

            // Use File.Copy for atomic operation - this is safer on Windows
            // File.Copy handles file locking internally and won't cause directory locking issues
            File.Copy(_lynxEvtFilePath, backupFilePath, true);

            var finalFileName = Path.GetFileName(backupFilePath);
            ApplicationLogger.Log($"Created EVT backup: {finalFileName}");
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error creating EVT backup", ex);
            // Rethrow lock-related errors so WriteRacesToEvtFile can retry; swallow others so backup failure doesn't block updates
            if (IsLockRelatedException(ex))
                throw;
        }
    }

    /// <summary>
    /// Returns true if the exception is likely a file/directory lock (transient); used to decide whether to retry.
    /// </summary>
    private static bool IsLockRelatedException(Exception ex)
    {
        var t = ex.GetType();
        if (t != typeof(IOException) && t != typeof(UnauthorizedAccessException))
            return false;
        var msg = (ex.Message ?? "").ToLowerInvariant();
        return msg.Contains("being used by another process") ||
               msg.Contains("cannot access the file") ||
               msg.Contains("access to the path is denied") ||
               msg.Contains("file is locked") ||
               msg.Contains("sharing violation") ||
               msg.Contains("the process cannot access") ||
               msg.Contains("used by another process");
    }

    private void WriteRacesToEvtFile(IEnumerable<Race> races)
    {
        const int maxAttempts = 3;
        var backoffMs = new[] { 50, 100, 200 };

        // Use the main lock to ensure only one thread writes to the EVT file at a time
        lock (_lockObject)
        {
            Exception? lastException = null;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 0)
                    {
                        ApplicationLogger.Log($"Lynx.evt write attempt {attempt} failed (lock). Retrying in {backoffMs[attempt - 1]}ms.");
                        Thread.Sleep(backoffMs[attempt - 1]);
                    }

                    // Create backup before writing (now inside the main lock)
                    CreateBackupIfEvtFileExists();

                    var evtContent = GenerateEvtContent(races);
                    var encoding = AppConfigService.GetOutputEncoding(_config.OutputEncoding);

                    // Write to a unique temporary file first, then move atomically
                    // This prevents directory locking issues on Windows and avoids temp file conflicts
                    var tempFilePath = _lynxEvtFilePath + "." + Guid.NewGuid().ToString("N")[..8] + ".tmp";
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (var writer = new StreamWriter(fileStream, encoding))
                    {
                        writer.Write(evtContent);
                    }

                    // Atomic move operation - this is safe on Windows and prevents partial reads
                    File.Move(tempFilePath, _lynxEvtFilePath, true);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (attempt >= maxAttempts - 1 || !IsLockRelatedException(ex))
                        throw;
                    ApplicationLogger.Log($"Lynx.evt file locked (attempt {attempt + 1}/{maxAttempts}), will retry after backoff: {ex.Message}");
                }
            }
        }
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

        return writer.ToString();
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

    private void LoadKeepRaces(bool suppressUserLog = false)
    {
        try
        {
            if (!File.Exists(_lynxEvtKeepFilePath))
            {
                // Keep file doesn't exist - clear any previously loaded keep races
                if (_fileRaces.ContainsKey("evt_keep_races"))
                {
                    _fileRaces.Remove("evt_keep_races");
                }
                return;
            }

            // Check if file is empty or very small
            var fileInfo = new FileInfo(_lynxEvtKeepFilePath);
            if (fileInfo.Length < 10) // Less than 10 bytes, likely empty
            {
                ApplicationLogger.Log("Lynx.evt.keep file exists but appears to be empty");
                if (_fileRaces.ContainsKey("evt_keep_races"))
                {
                    _fileRaces.Remove("evt_keep_races");
                }
                return;
            }

            ApplicationLogger.Log("Loading races from Lynx.evt.keep file...");
            
            // Use a timeout to prevent hanging
            var loadTask = Task.Run(async () =>
            {
                // Bypass extension validation for .keep file
                var dataProvider = new EventDataFileProvider(_lynxEvtKeepFilePath, validateExtension: false);
                var parser = new EvtParser(dataProvider);
                return await parser.ParseAsync();
            });
            
            if (!loadTask.Wait(TimeSpan.FromSeconds(5))) // 5 second timeout
            {
                ApplicationLogger.Log("Lynx.evt.keep file parsing timed out");
                return;
            }
            
            var keepRaces = loadTask.Result;
            ApplicationLogger.Log("Lynx.evt.keep file parsing completed");

            var racesList = keepRaces.ToList();
            var keepSourceKey = "evt_keep_races";
            
            if (racesList.Count > 0)
            {
                _fileRaces[keepSourceKey] = racesList;
                var raceNumbers = string.Join(", ", racesList.Select(r => r.RaceNumber));
                ApplicationLogger.Log($"Loaded {racesList.Count} races from Lynx.evt.keep file: {raceNumbers}");
                if (!suppressUserLog)
                {
                    WatcherLogger.Log($"Loaded {racesList.Count} races from Lynx.evt.keep file ({raceNumbers})");
                }
            }
            else
            {
                ApplicationLogger.Log("Lynx.evt.keep file exists but contains no races");
                if (_fileRaces.ContainsKey(keepSourceKey))
                {
                    _fileRaces.Remove(keepSourceKey);
                }
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error loading races from Lynx.evt.keep file", ex);
            // Continue without keep races - don't fail
        }
    }

    /// <summary>
    /// Reloads races from the Lynx.evt.keep file
    /// </summary>
    public void ReloadKeepRaces()
    {
        lock (_lockObject)
        {
            _hasLoadedKeepRaces = false;
            var previousKeepRaces = _fileRaces.ContainsKey("evt_keep_races") 
                ? _fileRaces["evt_keep_races"].Select(r => r.RaceNumber).ToHashSet()
                : new HashSet<string>();
            
            // Suppress user log from LoadKeepRaces since we'll log our own message
            LoadKeepRaces(suppressUserLog: true);
            _hasLoadedKeepRaces = true;
            
            // Get current keep race numbers
            var currentKeepRaces = _fileRaces.ContainsKey("evt_keep_races") 
                ? _fileRaces["evt_keep_races"].Select(r => r.RaceNumber).ToHashSet()
                : new HashSet<string>();
            
            // If keep races were removed, also remove them from evt_file_races
            // (they might have been written to the EVT file previously)
            var removedKeepRaces = previousKeepRaces.Except(currentKeepRaces).ToList();
            if (removedKeepRaces.Count > 0)
            {
                if (_fileRaces.ContainsKey("evt_file_races"))
                {
                    var evtRaces = _fileRaces["evt_file_races"];
                    var updatedEvtRaces = evtRaces
                        .Where(race => !removedKeepRaces.Contains(race.RaceNumber))
                        .ToList();
                    _fileRaces["evt_file_races"] = updatedEvtRaces;
                    ApplicationLogger.Log($"Removed {removedKeepRaces.Count} keep races from EVT races: {string.Join(", ", removedKeepRaces)}");
                }
            }
            
            // Log if keep races changed
            if (!previousKeepRaces.SetEquals(currentKeepRaces))
            {
                if (currentKeepRaces.Count > 0)
                {
                    var raceNumbers = string.Join(", ", currentKeepRaces.OrderBy(r => r));
                    WatcherLogger.Log($"Reloaded {currentKeepRaces.Count} races from Lynx.evt.keep file ({raceNumbers})");
                }
                else
                {
                    WatcherLogger.Log("Lynx.evt.keep file cleared - no keep races");
                }
            }
            
            // Trigger update to EVT file with new keep races
            WriteAllRacesToEvtFile();
        }
        
        // Notify that races have been updated
        RacesUpdated?.Invoke(this, EventArgs.Empty);
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

    protected virtual string GetCurrentTimestamp()
    {
        return DateTime.Now.ToString("yyyyMMdd_HHmmss");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}
