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

    public event EventHandler<string>? FileProcessed;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler? RacesUpdated;

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

    public void StartWatching()
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
        
        // Process existing files immediately
        ProcessExistingFiles();
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

        // Debounce file changes - ignore if we've processed this file recently
        lock (_lockObject)
        {
            if (_processedFiles.TryGetValue(e.FullPath, out var lastProcessed))
            {
                if (DateTime.Now - lastProcessed < TimeSpan.FromSeconds(2))
                {
                    return; // Skip if processed within last 2 seconds
                }
            }
            _processedFiles[e.FullPath] = DateTime.Now;
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
        try
        {
            await RemoveRacesFromFileAsync(e.FullPath);
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
        var userMessage = $"Processed {fileName}: {stats}";
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
            _evtFileManager?.Dispose();
            _disposed = true;
        }
    }
}
