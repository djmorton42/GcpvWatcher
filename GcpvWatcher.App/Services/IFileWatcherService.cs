using GcpvWatcher.App.Models;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Interface for file watcher services that monitor a directory for changes
/// and process GCPV export files.
/// </summary>
public interface IFileWatcherService : IDisposable
{
    /// <summary>
    /// Event raised when a file has been processed
    /// </summary>
    event EventHandler<string>? FileProcessed;

    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<string>? ErrorOccurred;

    /// <summary>
    /// Event raised when races have been updated
    /// </summary>
    event EventHandler? RacesUpdated;

    /// <summary>
    /// Event raised when racers have been updated
    /// </summary>
    event EventHandler? RacersUpdated;

    /// <summary>
    /// Gets the current collection of racers
    /// </summary>
    IReadOnlyDictionary<string, Racer> Racers { get; }

    /// <summary>
    /// Starts watching the configured directory asynchronously
    /// </summary>
    Task StartWatchingAsync();

    /// <summary>
    /// Starts watching the configured directory (synchronous)
    /// </summary>
    void StartWatching();

    /// <summary>
    /// Stops watching the directory
    /// </summary>
    void StopWatching();

    /// <summary>
    /// Gets all races currently tracked
    /// </summary>
    IEnumerable<Race> GetAllRaces();

    /// <summary>
    /// Disposes the service asynchronously
    /// </summary>
    Task DisposeAsync();
}

