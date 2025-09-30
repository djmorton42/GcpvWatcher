using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Parsers;

public class GcpvExportParserIntegrationTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "integration_test.csv");
    private readonly Dictionary<string, KeyFieldConfig> _keyFields;

    public GcpvExportParserIntegrationTests()
    {
        _keyFields = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };
    }

    [Fact]
    public async Task GcpvExportParser_WithRealFile_ReturnsCorrectRaceData()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act
            var raceData = await parser.ParseAsync();
            var raceList = raceData.ToList();

            // Assert
            Assert.Equal(2, raceList.Count);
            
            var race1 = raceList[0];
            Assert.Equal("25A", race1.RaceNumber);
            Assert.Equal("1500 111M", race1.TrackParams);
            Assert.Equal("Open Men B  male", race1.RaceGroup);
            Assert.Equal("Heat, 2 +2", race1.Stage);
            Assert.Single(race1.Racers);
            Assert.Equal("1", race1.Racers[0].Lane);
            Assert.Equal("689 PORTER, REGGIE", race1.Racers[0].Racer);
            Assert.Equal("Hamilton", race1.Racers[0].Affiliation);
            
            var race2 = raceList[1];
            Assert.Equal("21A", race2.RaceNumber);
            Assert.Equal("500M", race2.TrackParams);
            Assert.Equal("Open Women A", race2.RaceGroup);
            Assert.Equal("Final", race2.Stage);
            Assert.Single(race2.Racers);
            Assert.Equal("2", race2.Racers[0].Lane);
            Assert.Equal("123 SMITH, JANE", race2.Racers[0].Racer);
            Assert.Equal("Toronto", race2.Racers[0].Affiliation);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithFileContainingEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "   ",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act
            var raceData = await parser.ParseAsync();
            var raceList = raceData.ToList();

            // Assert
            Assert.Equal(2, raceList.Count);
            Assert.Equal("25A", raceList[0].RaceNumber);
            Assert.Equal("21A", raceList[1].RaceNumber);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithFileContainingComments_DoesNotFilterComments()
    {
        // Arrange
        var testData = new[]
        {
            "; This is a comment",
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "# Another comment",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public void GcpvExportParser_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "nonexistent.csv";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new GcpvExportDataFileProvider(nonExistentFile));
    }

    [Fact]
    public async Task GcpvExportParser_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        CreateTestFile();
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);
        File.Delete(_testFilePath);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => parser.ParseAsync());
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithEmptyFile_ReturnsEmptyResult()
    {
        // Arrange
        CreateTestFile(Array.Empty<string>());
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act
            var raceData = await parser.ParseAsync();
            var raceList = raceData.ToList();

            // Assert
            Assert.Empty(raceList);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithOnlyEmptyLines_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "   ",
            "\t",
            "  \t  "
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act
            var raceData = await parser.ParseAsync();
            var raceList = raceData.ToList();

            // Assert
            Assert.Empty(raceList);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithInvalidRowFormat_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "InvalidRowFormat"
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GcpvExportParser_WithSpecialCharactersInValues_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M with # and ; special chars\",\"Open Men B with, commas and \"quotes\"\",\"Stage :\",\"Heat, 2 +2 with # and ;\",,,\"Race\",\"25A with # and ;\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE with # and ;\",\"Hamilton with, commas\",\"28-Sep-25   9:35:42 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        try
        {
            // Act
            var raceData = await parser.ParseAsync();
            var raceList = raceData.ToList();

            // Assert
            Assert.Single(raceList);
            var race = raceList[0];
            Assert.Equal("25A with # and ;", race.RaceNumber);
            Assert.Equal("1500 111M with # and ; special chars", race.TrackParams);
            Assert.Equal("Open Men B with, commas and quotes\"\"", race.RaceGroup);
            Assert.Equal("Heat, 2 +2 with # and ;", race.Stage);
            Assert.Single(race.Racers);
            
            var racer = race.Racers[0];
            Assert.Equal("1", racer.Lane);
            Assert.Equal("689 PORTER, REGGIE with # and ;", racer.Racer);
            Assert.Equal("Hamilton with, commas", racer.Affiliation);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    private void CreateTestFile(string[]? content = null)
    {
        var testContent = content ?? new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        
        File.WriteAllLines(_testFilePath, testContent);
    }

    private void CleanupTestFile()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}