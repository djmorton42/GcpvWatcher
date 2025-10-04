using System;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Static logger class for GcpvWatcher application
/// </summary>
public static class WatcherLogger
{
    /// <summary>
    /// Logs a message to the console
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] {message}");
    }
}
