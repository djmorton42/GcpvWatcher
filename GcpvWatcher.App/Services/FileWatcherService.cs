using System.IO;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;

namespace GcpvWatcher.App.Services;

public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _fileWatcher;
    private readonly AppConfig _config;
    private readonly string _watchDirectory;
    private readonly string _finishLynxDirectory;
    private readonly EvtFileManager _evtFileManager;
    private readonly RaceDataConverter _raceDataConverter;
    private readonly Dictionary<string, DateTime> _processedFiles;
    private readonly object _lockObject = new object();
    private bool _disposed = false;
    private Timer? _cleanupTimer;
    private Dictionary<int, Racer> _racers = new Dictionary<int, Racer>();
    private readonly HashSet<string> _filesProcessedDuringStartup = new HashSet<string>();

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

    public FileWatcherService(AppConfig config, string watchDirectory, string finishLynxDirectory)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _watchDirectory = watchDirectory ?? throw new ArgumentNullException(nameof(watchDirectory));
        _finishLynxDirectory = finishLynxDirectory ?? throw new ArgumentNullException(nameof(finishLynxDirectory));
        _evtFileManager = new EvtFileManager(_finishLynxDirectory);
        _evtFileManager.RacesUpdated += OnRacesUpdated;
        _raceDataConverter = new RaceDataConverter();
        _processedFiles = new Dictionary<string, DateTime>();
    }

    public async Task StartWatchingAsync()
    {
        if (_fileWatcher != null)
            return;

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

        _fileWatcher = new FileSystemWatcher(_watchDirectory)
        {
            Filter = _config.GcpvExportFilePattern,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _fileWatcher.Created += OnFileChanged;
        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.Deleted += OnFileDeleted;
        _fileWatcher.Error += OnError;

        WatcherLogger.Log($"Started watching: {_watchDirectory} for pattern: {_config.GcpvExportFilePattern}");
        
        try
        {
            // Load existing races from EVT file first
            await LoadExistingRacesFromEvtFileAsync();
            
            // Load existing racers from PPL file if it exists
            await LoadExistingRacersFromPplFileAsync();
            
            // Process existing files immediately
            ProcessExistingFiles();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("timed out"))
        {
            // Stop the file watcher if parsing times out
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Created -= OnFileChanged;
            _fileWatcher.Changed -= OnFileChanged;
            _fileWatcher.Deleted -= OnFileDeleted;
            _fileWatcher.Error -= OnError;
            _fileWatcher.Dispose();
            _fileWatcher = null;
            
            WatcherLogger.Log("File watcher stopped due to parsing timeout");
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
        if (_fileWatcher == null)
            return;

        _fileWatcher.EnableRaisingEvents = false;
        _fileWatcher.Created -= OnFileChanged;
        _fileWatcher.Changed -= OnFileChanged;
        _fileWatcher.Deleted -= OnFileDeleted;
        _fileWatcher.Error -= OnError;
        _fileWatcher.Dispose();
        _fileWatcher = null;

        WatcherLogger.Log("Stopped watching directory");
    }

    private async void ProcessExistingFiles()
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

            // Process each file
            foreach (var filePath in files)
            {
                try
                {
                    // Add to processed files history so they're not considered "new" later
                    lock (_lockObject)
                    {
                        _processedFiles[filePath] = DateTime.Now;
                        _filesProcessedDuringStartup.Add(filePath);
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

            ApplicationLogger.Log("Finished processing existing files");
            
            // Clean up orphaned races after processing all files
            await CleanupOrphanedRacesAsync();
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error processing existing files: {ex.Message}";
            WatcherLogger.Log(errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (e.ChangeType != WatcherChangeTypes.Created && e.ChangeType != WatcherChangeTypes.Changed)
            return;

        var fileName = Path.GetFileName(e.FullPath);
        var isNewFile = e.ChangeType == WatcherChangeTypes.Created;
        var isFileChange = e.ChangeType == WatcherChangeTypes.Changed;

        // Check if this is truly a new file or just a modification
        bool trulyNewFile = false;
        lock (_lockObject)
        {
            if (_processedFiles.TryGetValue(e.FullPath, out var lastProcessed))
            {
                if (DateTime.Now - lastProcessed < TimeSpan.FromSeconds(2))
                {
                    return; // Skip if processed within last 2 seconds
                }
                // File was processed before - consider it truly new if:
                // 1. It was NOT processed during startup (to avoid false positives) AND
                // 2. Either it's a Created event OR it's a Changed event but the file is very recent
                var fileAge = DateTime.Now - File.GetCreationTime(e.FullPath);
                var isRecentFile = fileAge < TimeSpan.FromMinutes(1); // File created within last minute
                trulyNewFile = !_filesProcessedDuringStartup.Contains(e.FullPath) && 
                              (isNewFile || (isFileChange && isRecentFile));
            }
            else
            {
                // File was never processed before, so it's truly new
                trulyNewFile = true;
            }
            _processedFiles[e.FullPath] = DateTime.Now;
        }

        // Log the nature of the file change (only once per file due to debouncing)
        if (trulyNewFile)
        {
            WatcherLogger.Log($"New file detected: \"{fileName}\"");
        }
        else if (isFileChange || isNewFile)
        {
            WatcherLogger.Log($"File changed: \"{fileName}\"");
        }

        // Wait a bit to ensure file is fully written
        await Task.Delay(500);

        try
        {
            await ProcessFileAsync(e.FullPath);
            FileProcessed?.Invoke(this, e.FullPath);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error processing file {e.FullPath}: {ex.Message}";
            WatcherLogger.Log(errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
    }

    private async void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        WatcherLogger.Log($"File deleted: \"{fileName}\"");
        
        try
        {
            await RemoveRacesFromFileAsync(e.FullPath);
            
            // Cancel any existing cleanup timer and start a new one
            // This batches multiple file deletions together
            _cleanupTimer?.Dispose();
            _cleanupTimer = new Timer(async _ =>
            {
                await CleanupOrphanedRacesAsync();
                _cleanupTimer?.Dispose();
                _cleanupTimer = null;
            }, null, 200, Timeout.Infinite);
            
            FileProcessed?.Invoke(this, e.FullPath);
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error removing races from deleted file {e.FullPath}: {ex.Message}";
            WatcherLogger.Log(errorMessage);
            ErrorOccurred?.Invoke(this, errorMessage);
        }
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        var errorMessage = $"File watcher error: {e.GetException().Message}";
        WatcherLogger.Log(errorMessage);
        ErrorOccurred?.Invoke(this, errorMessage);
    }

    private void OnRacesUpdated(object? sender, EventArgs e)
    {
        RacesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<Race> GetAllRaces()
    {
        return _evtFileManager.GetAllRaces();
    }

    private async Task CleanupOrphanedRacesAsync()
    {
        try
        {
            // Get all active CSV files
            var activeFiles = Directory.GetFiles(_watchDirectory, _config.GcpvExportFilePattern);
            await _evtFileManager.CleanupOrphanedRacesAsync(activeFiles);
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

        // Create data provider for the file
        var dataProvider = new GcpvExportDataFileProvider(filePath);
        var parser = new GcpvExportParser(dataProvider, _config.KeyFields);

        // Parse the GCPV export data
        var gcpvRaces = await parser.ParseAsync();

        // Convert to Race objects
        var races = _raceDataConverter.ConvertGcpvRacesToRaces(gcpvRaces);

        // Update the EVT file
        var stats = await _evtFileManager.UpdateRacesFromFileAsync(filePath, races);

        // Log statistics to both loggers
        var fileName = Path.GetFileName(filePath);
        var userMessage = $"Processed \"{fileName}\": {stats.GetDetailedString()}";
        var detailedMessage = $"File: {fileName} - Added: {stats.RacesAdded}, Updated: {stats.RacesUpdated}, Unchanged: {stats.RacesUnchanged}, Removed: {stats.RacesRemoved}";
        
        WatcherLogger.Log(userMessage);
        ApplicationLogger.Log(detailedMessage);
    }

    private async Task RemoveRacesFromFileAsync(string filePath)
    {
        ApplicationLogger.Log($"Removing races from deleted file: {filePath}");
        await _evtFileManager.RemoveRacesFromFileAsync(filePath);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopWatching();
            _cleanupTimer?.Dispose();
            _evtFileManager?.Dispose();
            _disposed = true;
        }
    }
}
