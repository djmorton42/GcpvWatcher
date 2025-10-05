using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using System.IO;

namespace GcpvWatcher.Tests.Services;

public class FileWatcherServiceTimeoutTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempEvtFile;
    private readonly string _tempPplFile;

    public FileWatcherServiceTimeoutTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        _tempEvtFile = Path.Combine(_tempDirectory, "Lynx.evt");
        _tempPplFile = Path.Combine(_tempDirectory, "Lynx.ppl");
    }

    [Fact]
    public async Task StartWatchingAsync_WithEvtFileTimeout_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new AppConfig
        {
            GcpvExportFilePattern = "*.csv"
        };
        
        // Create a large EVT file that will cause parsing to timeout
        var largeEvtContent = new List<string>();
        for (int i = 0; i < 10000; i++)
        {
            largeEvtContent.Add($"1A,,,\"Race {i}\",,,,,,,,,1.0");
            largeEvtContent.Add($",{i},1");
        }
        await File.WriteAllLinesAsync(_tempEvtFile, largeEvtContent);
        
        var service = new FileWatcherService(config, _tempDirectory, _tempDirectory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartWatchingAsync());
    }

    [Fact]
    public async Task StartWatchingAsync_WithPplFileTimeout_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new AppConfig
        {
            GcpvExportFilePattern = "*.csv"
        };
        
        // Create a large PPL file that will cause parsing to timeout
        var largePplContent = new List<string>();
        for (int i = 0; i < 100000; i++)
        {
            largePplContent.Add($"{i},LastName{i},FirstName{i},Affiliation{i}");
        }
        await File.WriteAllLinesAsync(_tempPplFile, largePplContent);
        
        var service = new FileWatcherService(config, _tempDirectory, _tempDirectory);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartWatchingAsync());
    }

    [Fact]
    public async Task StartWatchingAsync_WithValidFiles_DoesNotThrow()
    {
        // Arrange
        var config = new AppConfig
        {
            GcpvExportFilePattern = "*.csv"
        };
        
        // Create small valid files
        await File.WriteAllLinesAsync(_tempEvtFile, new[] { "1A,,,\"Test Race\",,,,,,,,,1.0", ",100,1" });
        await File.WriteAllLinesAsync(_tempPplFile, new[] { "100,Smith,John,Toronto" });
        
        var service = new FileWatcherService(config, _tempDirectory, _tempDirectory);

        // Act & Assert
        await service.StartWatchingAsync(); // Should not throw
        service.StopWatching();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
