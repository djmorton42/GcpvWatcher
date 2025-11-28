using System;
using System.IO;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Service for writing log messages to date-suffixed log files in the ./log directory
/// </summary>
public static class FileLogService
{
    private static readonly object _lockObject = new object();
    private static string? _logDirectory;
    private static string? _currentLogFile;
    private static DateTime _currentDate;
    private static bool _initialized = false;

    /// <summary>
    /// Initializes the file log service (called automatically on first use)
    /// </summary>
    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (_lockObject)
        {
            if (_initialized)
                return;

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _logDirectory = Path.Combine(baseDirectory, "log");
            _currentDate = DateTime.Now.Date;
            
            // Ensure log directory exists
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
            
            _currentLogFile = GetLogFilePath(_currentDate);
            _initialized = true;
        }
    }

    /// <summary>
    /// Writes a message to the log file
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void WriteLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EnsureInitialized();

        lock (_lockObject)
        {
            try
            {
                var today = DateTime.Now.Date;
                
                // Check if we need to switch to a new log file (new day)
                if (today != _currentDate)
                {
                    _currentDate = today;
                    _currentLogFile = GetLogFilePath(_currentDate);
                }

                // Ensure log directory still exists (in case it was deleted)
                if (_logDirectory != null && !Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }

                // Append message to log file
                File.AppendAllText(_currentLogFile!, message + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If file logging fails, at least try to log to console
                // We don't want to throw exceptions from logging
                try
                {
                    Console.WriteLine($"[FILE LOG ERROR] Failed to write to log file: {ex.Message}");
                }
                catch
                {
                    // If even console logging fails, silently fail
                }
            }
        }
    }

    private static string GetLogFilePath(DateTime date)
    {
        var dateSuffix = date.ToString("yyyyMMdd");
        var logFileName = $"GcpvWatcher_{dateSuffix}.log";
        return Path.Combine(_logDirectory!, logFileName);
    }
}

