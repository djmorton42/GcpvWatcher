using System;

namespace GcpvWatcher.App.Services;

/// <summary>
/// User-facing logger for GcpvWatcher application status and user-relevant information
/// </summary>
public static class WatcherLogger
{
    /// <summary>
    /// Event raised when a log message is written
    /// </summary>
    public static event EventHandler<string>? LogMessage;

    /// <summary>
    /// Logs a user-facing message (status updates, file processing results, etc.)
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var formattedMessage = $"[{timestamp}] {message}";
        
        Console.WriteLine(formattedMessage);
        LogMessage?.Invoke(null, formattedMessage);
    }
}
