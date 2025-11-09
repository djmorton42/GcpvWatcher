using System.IO;
using System.Security.Cryptography;
using System.Text;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;

namespace GcpvWatcher.App.Services;

/// <summary>
/// A polling-based file watcher service that checks the watch directory every 10 seconds
/// and detects file changes by comparing file hashes.
/// </summary>
public class PollingFileWatcherService : IFileWatcherService
{
    private Timer? _pollingTimer;
    private readonly AppConfig _config;
    private readonly string _watchDirectory;
    private readonly string _finishLynxDirectory;
    private readonly EvtFileManager _evtFileManager;
    private readonly RaceDataConverter _raceDataConverter;
    private readonly Dictionary<string, string> _fileHashes; // Maps file path to hash
    private string? _keepFileHash; // Hash of the keep file to detect changes
    private readonly object _lockObject = new object();
    private bool _disposed = false;
    private bool _isWatching = false;
    private Timer? _cleanupTimer;
    private Dictionary<int, Racer> _racers = new Dictionary<int, Racer>();
    private SoundNotificationService? _soundNotificationService;
    private readonly SemaphoreSlim _pollingSemaphore = new SemaphoreSlim(1, 1); // Prevents overlapping polling cycles
    private const int PollingIntervalSeconds = 10;

    public event EventHandler<string>? FileProcessed;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? RacesUpdated;
    public event EventHandler? RacersUpdated;

    public IReadOnlyDictionary<int, Racer> Racers
    {
        get
        {
            lock (_lockObject)
            {
                return _racers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }

    public PollingFileWatcherService(AppConfig config, string watchDirectory, string finishLynxDirectory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _watchDirectory = watchDirectory ?? throw new ArgumentNullException(nameof(watchDirectory));
        _finishLynxDirectory = finishLynxDirectory ?? throw new ArgumentNullException(nameof(finishLynxDirectory));
        _evtFileManager = new EvtFileManager(_finishLynxDirectory, _config);
        _evtFileManager.RacesUpdated += OnRacesUpdated;
        _raceDataConverter = new RaceDataConverter();
        _fileHashes = new Dictionary<string, string>();
        
        // Initialize sound notification service if path is configured
        if (!string.IsNullOrEmpty(_config.NotificationSoundPath))
        {
            var soundPath = Path.IsPathRooted(_config.NotificationSoundPath) 
                ? _config.NotificationSoundPath 
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _config.NotificationSoundPath);
            
            _soundNotificationService = new SoundNotificationService(soundPath, _config.EnableNotificationSound);
        }
    }

    public async Task StartWatchingAsync()
    {
        lock (_lockObject)
        {
            if (_isWatching || _disposed)
                return;
        }

        if (!Directory.Exists(_watchDirectory))
        {
            throw new DirectoryNotFoundException($"Watch directory does not exist: {_watchDirectory}");
        }

        if (!Directory.Exists(_finishLynxDirectory))
        {
            throw new DirectoryNotFoundException($"FinishLynx directory does not exist: {_finishLynxDirectory}");
        }

        // Check if Lynx.evt file exists, create it if it doesn't
        var fileOperationsService = new FileOperationsService();
        if (!fileOperationsService.LynxEvtFileExists(_finishLynxDirectory))
        {
            try
            {
                var createdFilePath = fileOperationsService.CreateLynxEvtFile(_finishLynxDirectory);
                WatcherLogger.Log($"Lynx.evt file not found. Created.");
            }
            catch (Exception ex)
            {
                WatcherLogger.Log($"Error creating Lynx.evt file: {ex.Message}");
                throw;
            }
        }
        else
        {
            WatcherLogger.Log("Lynx.evt file found.");
        }

        WatcherLogger.Log($"Starting polling watcher: {_watchDirectory} for pattern: {_config.GcpvExportFilePattern}");
        
        try
        {
            // Load existing races from EVT file first
            await LoadExistingRacesFromEvtFileAsync();
            
            // Load keep races (these take precedence over all other races)
            _evtFileManager.ReloadKeepRaces();
            
            // Initialize keep file hash
            var keepFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt.keep");
            if (File.Exists(keepFilePath))
            {
                _keepFileHash = await CalculateFileHashAsync(keepFilePath);
            }
            
            // Load existing racers from PPL file if it exists
            await LoadExistingRacersFromPplFileAsync();
            
            // Process existing files immediately and build initial hash list
            await ProcessExistingFilesAsync();
            
            // Start polling timer
            lock (_lockObject)
            {
                if (_disposed)
                    return;
                _pollingTimer = new Timer(OnPollingTimer, null, TimeSpan.Zero, TimeSpan.FromSeconds(PollingIntervalSeconds));
                _isWatching = true;
            }
            
            WatcherLogger.Log($"Polling watcher started. Checking every {PollingIntervalSeconds} seconds.");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("timed out"))
        {
            WatcherLogger.Log("Polling watcher stopped due to parsing timeout");
            throw; // Re-throw to indicate startup failure
        }
    }

    public void StartWatching()
    {
        StartWatchingAsync().Wait();
    }

    private async Task LoadExistingRacesFromEvtFileAsync()
    {
        try
        {
            var lynxEvtFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt");
            
            if (!File.Exists(lynxEvtFilePath))
            {
                WatcherLogger.Log("No existing EVT file found, starting with empty race list");
                return;
            }

            // Check if file is empty or very small
            var fileInfo = new FileInfo(lynxEvtFilePath);
            if (fileInfo.Length < 10) // Less than 10 bytes, likely empty
            {
                WatcherLogger.Log("EVT file exists but appears to be empty, starting with empty race list");
                return;
            }
            
            // Use a timeout to prevent hanging
            var loadTask = Task.Run(async () =>
            {
                var dataProvider = new EventDataFileProvider(lynxEvtFilePath);
                var parser = new EvtParser(dataProvider);
                return await parser.ParseAsync();
            });
            
            var existingRaces = await loadTask.WaitAsync(TimeSpan.FromSeconds(5)); // 5 second timeout
            var racesList = existingRaces.ToList();
            
            if (racesList.Count > 0)
            {
                // Load the races directly into the EvtFileManager without triggering the loading logic
                _evtFileManager.SetExistingRaces(racesList);
                WatcherLogger.Log($"Loaded {racesList.Count} existing races from EVT file");
            }
            else
            {
                WatcherLogger.Log("EVT file exists but contains no races");
            }
        }
        catch (TimeoutException)
        {
            var errorMessage = "Unable to read existing EVT file. Watching stopped.";
            WatcherLogger.Log(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        catch (Exception ex)
        {
            WatcherLogger.Log($"Error loading existing races from EVT file: {ex.Message}");
            // Continue with empty race list - don't fail startup
        }
    }

    private async Task LoadExistingRacersFromPplFileAsync()
    {
        try
        {
            var lynxPplFilePath = Path.Combine(_finishLynxDirectory, "Lynx.ppl");
            
            if (!File.Exists(lynxPplFilePath))
            {
                WatcherLogger.Log("No existing PPL file found, starting with empty racer list");
                return;
            }

            // Check if file is empty or very small
            var fileInfo = new FileInfo(lynxPplFilePath);
            if (fileInfo.Length < 5) // Less than 5 bytes, likely empty
            {
                WatcherLogger.Log("PPL file exists but appears to be empty, starting with empty racer list");
                return;
            }
            
            // Use a timeout to prevent hanging
            var loadTask = Task.Run(async () =>
            {
                var dataProvider = new PeopleDataFileProvider(lynxPplFilePath);
                var parser = new PplParser(dataProvider);
                return await parser.ParseAsync();
            });
            
            var racers = await loadTask.WaitAsync(TimeSpan.FromSeconds(5)); // 5 second timeout
            
            lock (_lockObject)
            {
                _racers = racers;
            }
            WatcherLogger.Log($"Loaded {_racers.Count} existing racers from PPL file");
            RacersUpdated?.Invoke(this, EventArgs.Empty);
        }
        catch (TimeoutException)
        {
            var errorMessage = "PPL file parsing timed out - this indicates a serious issue with the file. Stopping file watcher.";
            WatcherLogger.Log(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        catch (Exception ex)
        {
            WatcherLogger.Log($"Error loading existing racers from PPL file: {ex.Message}");
            // Continue with empty racer list - don't fail startup
        }
    }

    public void StopWatching()
    {
        lock (_lockObject)
        {
            if (!_isWatching)
                return;

            _isWatching = false;
        }

        _pollingTimer?.Dispose();
        _pollingTimer = null;

        WatcherLogger.Log("Stopped polling watcher");
    }

    private async void OnPollingTimer(object? state)
    {
        // Check if we should continue
        lock (_lockObject)
        {
            if (_disposed || !_isWatching)
                return;
        }

        // Use semaphore to prevent overlapping polling cycles
        if (!await _pollingSemaphore.WaitAsync(0))
        {
            // Previous polling cycle still running, skip this one
            return;
        }

        try
        {
            await CheckForFileChangesAsync();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error during polling check: {ex.Message}";
            WatcherLogger.Log(errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
        finally
        {
            _pollingSemaphore.Release();
        }
    }

    private async Task CheckForFileChangesAsync()
    {
        if (!Directory.Exists(_watchDirectory))
        {
            return;
        }

        // Check for changes to the keep file
        await CheckKeepFileChangesAsync();

        // Get all files matching the pattern
        var currentFiles = Directory.GetFiles(_watchDirectory, _config.GcpvExportFilePattern)
            .ToHashSet();

        Dictionary<string, string> currentHashes;
        lock (_lockObject)
        {
            currentHashes = new Dictionary<string, string>(_fileHashes);
        }

        // Calculate hashes for current files
        var newHashes = new Dictionary<string, string>();
        var changesToProcess = new List<FileChange>();

        foreach (var filePath in currentFiles)
        {
            try
            {
                var hash = await CalculateFileHashAsync(filePath);
                
                // If hash is null, file is locked - skip change detection for this file
                // but keep the previous hash (if any) to avoid false positives
                if (hash == null)
                {
                    // File is locked - preserve previous hash if it exists to avoid false change detection
                    // This ensures previously loaded race events remain in the system
                    if (currentHashes.ContainsKey(filePath))
                    {
                        newHashes[filePath] = currentHashes[filePath]; // Keep previous hash
                        // Races from this file remain in EvtFileManager and will continue to be available
                        // They will be updated on the next poll when the file is unlocked
                    }
                    // If no previous hash, don't add to newHashes - will be treated as new file next time
                    continue;
                }
                
                newHashes[filePath] = hash;

                // Check if this is a new file or a changed file
                if (!currentHashes.ContainsKey(filePath))
                {
                    // New file
                    changesToProcess.Add(new FileChange
                    {
                        FilePath = filePath,
                        ChangeType = FileChangeType.Added
                    });
                }
                else if (currentHashes[filePath] != hash)
                {
                    // Changed file
                    changesToProcess.Add(new FileChange
                    {
                        FilePath = filePath,
                        ChangeType = FileChangeType.Changed
                    });
                }
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogException($"Error calculating hash for file {filePath}", ex);
                // Continue with other files
            }
        }

        // Check for removed files
        foreach (var filePath in currentHashes.Keys)
        {
            if (!currentFiles.Contains(filePath))
            {
                changesToProcess.Add(new FileChange
                {
                    FilePath = filePath,
                    ChangeType = FileChangeType.Removed
                });
            }
        }

        // Update the stored hashes
        lock (_lockObject)
        {
            _fileHashes.Clear();
            foreach (var kvp in newHashes)
            {
                _fileHashes[kvp.Key] = kvp.Value;
            }
        }

        // Process changes
        if (changesToProcess.Count > 0)
        {
            await ProcessFileChangesAsync(changesToProcess);
        }
    }

    private async Task CheckKeepFileChangesAsync()
    {
        var keepFilePath = Path.Combine(_finishLynxDirectory, "Lynx.evt.keep");
        
        if (!File.Exists(keepFilePath))
        {
            // Keep file doesn't exist - clear hash if it was set
            lock (_lockObject)
            {
                if (_keepFileHash != null)
                {
                    _keepFileHash = null;
                    // Reload to clear keep races (inside lock for consistency)
                    _evtFileManager.ReloadKeepRaces();
                }
            }
            return;
        }

        try
        {
            var currentHash = await CalculateFileHashAsync(keepFilePath);
            
            lock (_lockObject)
            {
                if (currentHash != null && currentHash != _keepFileHash)
                {
                    // Keep file has changed
                    _keepFileHash = currentHash;
                    // Reload keep races (inside lock for consistency)
                    _evtFileManager.ReloadKeepRaces();
                    WatcherLogger.Log("Lynx.evt.keep file changed - reloaded keep races");
                }
                else if (_keepFileHash == null && currentHash != null)
                {
                    // Keep file was just created or first time checking
                    _keepFileHash = currentHash;
                    // Reload keep races (inside lock for consistency)
                    _evtFileManager.ReloadKeepRaces();
                }
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error checking keep file for changes", ex);
        }
    }

    private async Task<string?> CalculateFileHashAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await md5.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes);
        }
        catch (IOException ex)
        {
            // File might be locked by another process
            // Return null to indicate we couldn't read it (different from empty string)
            // This prevents false change detection
            ApplicationLogger.Log($"File is locked, cannot calculate hash: {Path.GetFileName(filePath)} - {ex.Message}");
            return null!; // Use null to indicate locked file (will be handled specially)
        }
        catch (UnauthorizedAccessException ex)
        {
            // File access denied
            ApplicationLogger.Log($"Access denied to file: {Path.GetFileName(filePath)} - {ex.Message}");
            return null!;
        }
    }

    private async Task ProcessFileChangesAsync(List<FileChange> changes)
    {
        var logMessages = new List<string>();
        var eventsToRaise = new List<(EventHandler<string>? handler, string filePath)>();

        foreach (var change in changes)
        {
            var fileName = Path.GetFileName(change.FilePath);

            try
            {
                switch (change.ChangeType)
                {
                    case FileChangeType.Added:
                        logMessages.Add($"New file detected: \"{fileName}\"");
                        _soundNotificationService?.PlayNotificationSound();
                        await ProcessFileAsync(change.FilePath);
                        eventsToRaise.Add((FileProcessed, change.FilePath));
                        break;

                    case FileChangeType.Changed:
                        logMessages.Add($"File changed: \"{fileName}\"");
                        _soundNotificationService?.PlayNotificationSound();
                        await ProcessFileAsync(change.FilePath);
                        eventsToRaise.Add((FileProcessed, change.FilePath));
                        break;

                    case FileChangeType.Removed:
                        logMessages.Add($"File deleted: \"{fileName}\"");
                        _soundNotificationService?.PlayNotificationSound();
                        RemoveRacesFromFile(change.FilePath);
                        eventsToRaise.Add((FileProcessed, change.FilePath));
                        
                        // Schedule cleanup of orphaned races
                        _cleanupTimer?.Dispose();
                        _cleanupTimer = new Timer(_ =>
                        {
                            CleanupOrphanedRaces();
                            _cleanupTimer?.Dispose();
                            _cleanupTimer = null;
                        }, null, 200, Timeout.Infinite);
                        break;
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing file change for {change.FilePath}: {ex.Message}";
                logMessages.Add(errorMessage);
                ApplicationLogger.LogException($"Error processing file change for {change.FilePath}", ex);
                ErrorOccurred?.Invoke(this, errorMessage);
            }
        }

        // Log all changes
        foreach (var message in logMessages)
        {
            WatcherLogger.Log(message);
        }

        // Raise events
        foreach (var (handler, filePath) in eventsToRaise)
        {
            handler?.Invoke(this, filePath);
        }
    }

    private async Task ProcessExistingFilesAsync()
    {
        try
        {
            ApplicationLogger.Log("Processing existing files in watch directory...");
            
            // Get all files matching the pattern
            var files = Directory.GetFiles(_watchDirectory, _config.GcpvExportFilePattern);
            
            if (files.Length == 0)
            {
                ApplicationLogger.Log("No existing files found matching the pattern");
                return;
            }

            WatcherLogger.Log($"Found {files.Length} existing files to process");

            // Process each file and calculate initial hashes
            var hashesToAdd = new Dictionary<string, string>();
            foreach (var filePath in files)
            {
                try
                {
                    var hash = await CalculateFileHashAsync(filePath);
                    if (hash != null)
                    {
                        hashesToAdd[filePath] = hash;
                    }
                    
                    await ProcessFileAsync(filePath);
                    FileProcessed?.Invoke(this, filePath);
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error processing existing file {filePath}: {ex.Message}";
                    ApplicationLogger.LogException($"Error processing existing file {filePath}", ex);
                    ErrorOccurred?.Invoke(this, errorMessage);
                }
            }

            // Batch update hashes in a single lock
            lock (_lockObject)
            {
                foreach (var kvp in hashesToAdd)
                {
                    _fileHashes[kvp.Key] = kvp.Value;
                }
            }

            ApplicationLogger.Log("Finished processing existing files");
            
            // Clean up orphaned races after processing all files
            CleanupOrphanedRaces();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error processing existing files: {ex.Message}";
            WatcherLogger.Log(errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
    }

    private void OnRacesUpdated(object? sender, EventArgs e)
    {
        RacesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<Race> GetAllRaces()
    {
        return _evtFileManager.GetAllRaces();
    }

    private void CleanupOrphanedRaces()
    {
        try
        {
            // Get all active CSV files
            var activeFiles = Directory.GetFiles(_watchDirectory, _config.GcpvExportFilePattern);
            _evtFileManager.CleanupOrphanedRaces(activeFiles);
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error cleaning up orphaned races", ex);
        }
    }

    private async Task ProcessFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            ApplicationLogger.Log($"File no longer exists: {filePath}");
            return;
        }

        WatcherLogger.Log($"Processing file: \"{Path.GetFileName(filePath)}\"");

        try
        {
            // Create data provider for the file
            var dataProvider = new GcpvExportDataFileProvider(filePath);
            var parser = new GcpvExportParser(dataProvider, _config.KeyFields);

            // Parse the GCPV export data
            var gcpvRaces = await parser.ParseAsync();

            // Convert to Race objects
            var races = _raceDataConverter.ConvertGcpvRacesToRaces(gcpvRaces);

            // Update the EVT file
            var stats = _evtFileManager.UpdateRacesFromFile(filePath, races);

            // Log statistics to both loggers
            var fileName = Path.GetFileName(filePath);
            var userMessage = $"Processed \"{fileName}\": {stats.GetDetailedString()}";
            var detailedMessage = $"File: {fileName} - Added: {stats.RacesAdded}, Updated: {stats.RacesUpdated}, Unchanged: {stats.RacesUnchanged}, Removed: {stats.RacesRemoved}";
            
            WatcherLogger.Log(userMessage);
            ApplicationLogger.Log(detailedMessage);
        }
        catch (IOException ex)
        {
            // File is locked by another process
            var fileName = Path.GetFileName(filePath);
            var errorMessage = $"File is locked by another process, skipping: \"{fileName}\"";
            WatcherLogger.Log(errorMessage);
            ApplicationLogger.Log($"{errorMessage} - {ex.Message}");
            throw; // Re-throw to be handled by caller
        }
        catch (UnauthorizedAccessException ex)
        {
            // File access denied
            var fileName = Path.GetFileName(filePath);
            var errorMessage = $"Access denied to file, skipping: \"{fileName}\"";
            WatcherLogger.Log(errorMessage);
            ApplicationLogger.Log($"{errorMessage} - {ex.Message}");
            throw; // Re-throw to be handled by caller
        }
    }

    private void RemoveRacesFromFile(string filePath)
    {
        ApplicationLogger.Log($"Removing races from deleted file: {filePath}");
        _evtFileManager.RemoveRacesFromFile(filePath);
    }

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed)
                return;
            _disposed = true;
            _isWatching = false;
        }

        StopWatching();
        _pollingTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _evtFileManager?.Dispose();
        _soundNotificationService?.Dispose();
        _pollingSemaphore?.Dispose();
    }

    public async Task DisposeAsync()
    {
        lock (_lockObject)
        {
            if (_disposed)
                return;
            _disposed = true;
            _isWatching = false;
        }

        StopWatching();
        
        // Wait for any ongoing polling cycle to complete
        await _pollingSemaphore.WaitAsync();
        try
        {
            // Give background tasks time to complete
            await Task.Delay(100);
        }
        finally
        {
            _pollingSemaphore.Release();
        }
        
        _pollingTimer?.Dispose();
        _cleanupTimer?.Dispose();
        _evtFileManager?.Dispose();
        _soundNotificationService?.Dispose();
        _pollingSemaphore?.Dispose();
    }

    private class FileChange
    {
        public string FilePath { get; set; } = string.Empty;
        public FileChangeType ChangeType { get; set; }
    }

    private enum FileChangeType
    {
        Added,
        Changed,
        Removed
    }
}

