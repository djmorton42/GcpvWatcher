using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using System.IO;
using System.Linq;

namespace GcpvWatcher.Tests.Services;

public class EvtFileManagerBackupTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly AppConfig _config;

    public EvtFileManagerBackupTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        _config = new AppConfig
        {
            EvtBackupDirectory = "backups",
            OutputEncoding = "utf-16"
        };
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WhenNoEvtFileExists_DoesNotCreateBackup()
    {
        // Arrange
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        Assert.False(Directory.Exists(backupDirectory));
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WhenEvtFileExists_CreatesBackup()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        Assert.True(Directory.Exists(backupDirectory));
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Single(backupFiles);
        
        var backupFileName = Path.GetFileName(backupFiles[0]);
        Assert.StartsWith("Lynx.evt.", backupFileName);
        Assert.Matches(@"Lynx\.evt\.\d{8}_\d{6}", backupFileName);
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WhenBackupDirectoryDoesNotExist_CreatesDirectory()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        Assert.True(Directory.Exists(backupDirectory));
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_BackupContainsCorrectContent()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        var originalContent = "original evt content";
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, originalContent);
        
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Single(backupFiles);
        
        var backupContent = File.ReadAllText(backupFiles[0]);
        Assert.Equal(originalContent, backupContent);
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_MultipleUpdates_CreatesMultipleBackups()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);

        // Act
        evtFileManager.UpdateRacesFromFile("test1.csv", new List<Race>());
        Thread.Sleep(1000); // Ensure different timestamps
        evtFileManager.UpdateRacesFromFile("test2.csv", new List<Race>());

        // Assert
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Equal(2, backupFiles.Length);
        
        // Verify all files have correct naming pattern
        foreach (var backupFile in backupFiles)
        {
            var fileName = Path.GetFileName(backupFile);
            Assert.Matches(@"Lynx\.evt\.\d{8}_\d{6}", fileName);
        }
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WithCustomBackupDirectory_WorksCorrectly()
    {
        // Arrange
        var customConfig = new AppConfig
        {
            EvtBackupDirectory = "custom_backups",
            OutputEncoding = "utf-16"
        };
        
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, customConfig.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        var evtFileManager = new EvtFileManager(_tempDirectory, customConfig);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        Assert.True(Directory.Exists(backupDirectory));
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Single(backupFiles);
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WhenBackupFileExists_AddsUniqueSuffix()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        // Create a fake backup file with the expected name to simulate conflict
        Directory.CreateDirectory(backupDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var conflictingFile = Path.Combine(backupDirectory, $"Lynx.evt.{timestamp}");
        File.WriteAllText(conflictingFile, "conflicting content");
        
        var evtFileManager = new EvtFileManager(_tempDirectory, _config);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Equal(2, backupFiles.Length); // Original conflicting file + new backup with suffix
        
        // Find the new backup file (should have a suffix)
        var newBackupFile = backupFiles.FirstOrDefault(f => f != conflictingFile);
        Assert.NotNull(newBackupFile);
        
        var fileName = Path.GetFileName(newBackupFile);
        Assert.Matches(@"Lynx\.evt\.\d{8}_\d{6}\.1", fileName);
        
        // Verify the new backup contains the correct content
        var backupContent = File.ReadAllText(newBackupFile);
        Assert.Equal("test content", backupContent);
    }

    [Fact]
    public async Task CreateBackupIfEvtFileExists_WhenMultipleBackupFilesExist_AddsIncrementalSuffix()
    {
        // Arrange
        var evtFilePath = Path.Combine(_tempDirectory, "Lynx.evt");
        var backupDirectory = Path.Combine(_tempDirectory, _config.EvtBackupDirectory);
        
        // Create initial EVT file
        File.WriteAllText(evtFilePath, "test content");
        
        // Create multiple fake backup files to simulate multiple conflicts
        Directory.CreateDirectory(backupDirectory);
        
        // Create a fixed timestamp to ensure we can predict the behavior
        var fixedTimestamp = "20251017_120000"; // Fixed timestamp
        var conflictingFile1 = Path.Combine(backupDirectory, $"Lynx.evt.{fixedTimestamp}");
        var conflictingFile2 = Path.Combine(backupDirectory, $"Lynx.evt.{fixedTimestamp}.1");
        var conflictingFile3 = Path.Combine(backupDirectory, $"Lynx.evt.{fixedTimestamp}.2");
        
        File.WriteAllText(conflictingFile1, "conflicting content 1");
        File.WriteAllText(conflictingFile2, "conflicting content 2");
        File.WriteAllText(conflictingFile3, "conflicting content 3");
        
        // Create a custom EvtFileManager that uses a fixed timestamp for testing
        var evtFileManager = new TestableEvtFileManager(_tempDirectory, _config, fixedTimestamp);

        // Act
        evtFileManager.UpdateRacesFromFile("test.csv", new List<Race>());

        // Assert
        var backupFiles = Directory.GetFiles(backupDirectory, "Lynx.evt.*");
        Assert.Equal(4, backupFiles.Length); // 3 existing + 1 new backup
        
        // Find the new backup file by looking for files that don't match our known conflicting files
        var knownConflictingFiles = new[] { conflictingFile1, conflictingFile2, conflictingFile3 };
        var newBackupFile = backupFiles.FirstOrDefault(f => !knownConflictingFiles.Contains(f));
        Assert.NotNull(newBackupFile);
        
        var fileName = Path.GetFileName(newBackupFile);
        // The new file should have a suffix (could be .1, .2, .3, etc. depending on which files exist)
        Assert.Matches(@"Lynx\.evt\.20251017_120000\.\d+", fileName);
        
        // Verify the new backup contains the correct content
        var backupContent = File.ReadAllText(newBackupFile);
        Assert.Equal("test content", backupContent);
    }

    // Test helper class that allows us to control the timestamp
    private class TestableEvtFileManager : EvtFileManager
    {
        private readonly string _fixedTimestamp;

        public TestableEvtFileManager(string finishLynxDirectory, AppConfig config, string fixedTimestamp) 
            : base(finishLynxDirectory, config)
        {
            _fixedTimestamp = fixedTimestamp;
        }

        protected override string GetCurrentTimestamp()
        {
            return _fixedTimestamp;
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
