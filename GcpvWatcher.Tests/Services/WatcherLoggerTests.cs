using GcpvWatcher.App.Services;
using System;
using System.IO;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class WatcherLoggerTests
{
    [Fact]
    public void Log_WithValidMessage_WritesToConsole()
    {
        // Arrange
        var testMessage = "Test log message";
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log(testMessage);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(testMessage, output);
            Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), output); // Check timestamp format
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithNullMessage_DoesNotWriteToConsole()
    {
        // Arrange
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log(null!);

            // Assert
            var output = stringWriter.ToString();
            Assert.Empty(output);
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithEmptyMessage_DoesNotWriteToConsole()
    {
        // Arrange
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log("");

            // Assert
            var output = stringWriter.ToString();
            Assert.Empty(output);
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithWhitespaceMessage_DoesNotWriteToConsole()
    {
        // Arrange
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log("   ");

            // Assert
            var output = stringWriter.ToString();
            Assert.Empty(output);
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithValidMessage_IncludesTimestamp()
    {
        // Arrange
        var testMessage = "Test log message with timestamp";
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log(testMessage);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("[", output);
            Assert.Contains("]", output);
            Assert.Contains(testMessage, output);
            
            // Verify the format is [timestamp] message
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Single(lines);
            var line = lines[0];
            Assert.StartsWith("[", line);
            Assert.EndsWith(testMessage, line);
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Log_WithMultipleMessages_WritesEachMessage()
    {
        // Arrange
        var message1 = "First message";
        var message2 = "Second message";
        var originalOut = Console.Out;
        
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // Act
            WatcherLogger.Log(message1);
            WatcherLogger.Log(message2);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains(message1, output);
            Assert.Contains(message2, output);
            
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
        }
        finally
        {
            // Restore original console output
            Console.SetOut(originalOut);
        }
    }
}
