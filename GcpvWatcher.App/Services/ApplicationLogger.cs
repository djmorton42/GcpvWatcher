using System;

namespace GcpvWatcher.App.Services;

/// <summary>
/// Internal application logger for debugging and internal logging
/// </summary>
public static class ApplicationLogger
{
    /// <summary>
    /// Logs a message to the console for internal application logging
    /// </summary>
    /// <param name="message">The message to log</param>
    public static void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [APP] {message}");
    }

    /// <summary>
    /// Logs an exception with additional context
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="exception">The exception to log</param>
    public static void LogException(string message, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(message) && exception == null)
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = string.IsNullOrWhiteSpace(message) 
            ? $"[{timestamp}] [APP] Exception: {exception?.Message}"
            : $"[{timestamp}] [APP] {message}: {exception?.Message}";
        
        Console.WriteLine(logMessage);
        
        if (exception != null)
        {
            Console.WriteLine($"[{timestamp}] [APP] Stack Trace: {exception.StackTrace}");
        }
    }
}
