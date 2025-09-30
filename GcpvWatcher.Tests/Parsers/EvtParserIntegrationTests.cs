using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Parsers;

public class EvtParserIntegrationTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "integration_test.evt");

    [Fact]
    public async Task EvtParser_WithRealFile_ReturnsCorrectRaces()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"ALL SKATERS, (500A No Pts 111M) Final\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2",
            ",681,3",
            ",1231,4",
            ",746,5",
            "21B,,,\"ALL SKATERS, (500A No Pts 111M) Final\",,,,,,,,,4.5",
            ",563,1",
            ",617,2",
            ",531,3",
            ",2014,4",
            ",675,5"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Equal(2, raceList.Count);
            
            // Check first race
            var race1 = raceList[0];
            Assert.Equal("21A", race1.RaceNumber);
            Assert.Equal("ALL SKATERS, (500A No Pts 111M) Final", race1.RaceTitle);
            Assert.Equal(4.5m, race1.NumberOfLaps);
            Assert.Equal(5, race1.Racers.Count);
            Assert.Equal(1, race1.Racers[1051]);
            Assert.Equal(2, race1.Racers[2010]);
            Assert.Equal(3, race1.Racers[681]);
            Assert.Equal(4, race1.Racers[1231]);
            Assert.Equal(5, race1.Racers[746]);
            
            // Check second race
            var race2 = raceList[1];
            Assert.Equal("21B", race2.RaceNumber);
            Assert.Equal("ALL SKATERS, (500A No Pts 111M) Final", race2.RaceTitle);
            Assert.Equal(4.5m, race2.NumberOfLaps);
            Assert.Equal(5, race2.Racers.Count);
            Assert.Equal(1, race2.Racers[563]);
            Assert.Equal(2, race2.Racers[617]);
            Assert.Equal(3, race2.Racers[531]);
            Assert.Equal(4, race2.Racers[2014]);
            Assert.Equal(5, race2.Racers[675]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task EvtParser_WithFileContainingComments_FiltersOutComments()
    {
        // Arrange
        var testData = new[]
        {
            "; This is a comment",
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            "# Another comment",
            "21B,,,\"Race Title 2\",,,,,,,,,3.0",
            ",563,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Equal(2, raceList.Count);
            Assert.Equal("21A", raceList[0].RaceNumber);
            Assert.Equal("21B", raceList[1].RaceNumber);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public void EvtParser_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "nonexistent.evt";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new EventDataFileProvider(nonExistentFile));
    }

    [Fact]
    public async Task EvtParser_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        CreateTestFile();
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);
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
    public async Task EvtParser_WithComplexRaceNumbers_SortsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "100A,,,\"Race 100A\",,,,,,,,,1.0",
            ",100,1",
            "3B,,,\"Race 3B\",,,,,,,,,2.0",
            ",200,1",
            "3A,,,\"Race 3A\",,,,,,,,,3.0",
            ",300,1",
            "22A,,,\"Race 22A\",,,,,,,,,4.0",
            ",400,1",
            "10A,,,\"Race 10A\",,,,,,,,,5.0",
            ",500,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Equal(5, raceList.Count);
            Assert.Equal("3A", raceList[0].RaceNumber);
            Assert.Equal("3B", raceList[1].RaceNumber);
            Assert.Equal("10A", raceList[2].RaceNumber);
            Assert.Equal("22A", raceList[3].RaceNumber);
            Assert.Equal("100A", raceList[4].RaceNumber);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task EvtParser_WithEmptyFile_ReturnsEmptyResult()
    {
        // Arrange
        CreateTestFile(Array.Empty<string>());
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Empty(raceList);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task EvtParser_WithOnlyComments_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[]
        {
            "; This is a comment",
            "# Another comment",
            "   ; Comment with leading space"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Empty(raceList);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task EvtParser_WithInvalidRaceInfoLine_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "InvalidRaceInfo,,,\"Race Title\",,,,,,,,,4.5"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

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
    public async Task EvtParser_WithRaceTitleContainingSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with, \"\"quotes\"\", #hashes, and; semicolons\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2",
            "21B,,,\"Another race with special chars\",,,,,,,,,3.0",
            ",563,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Equal(2, raceList.Count);
            
            var race1 = raceList[0];
            Assert.Equal("21A", race1.RaceNumber);
            Assert.Equal("Race with, \"quotes\", #hashes, and; semicolons", race1.RaceTitle);
            Assert.Equal(4.5m, race1.NumberOfLaps);
            
            var race2 = raceList[1];
            Assert.Equal("21B", race2.RaceNumber);
            Assert.Equal("Another race with special chars", race2.RaceTitle);
            Assert.Equal(3.0m, race2.NumberOfLaps);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task EvtParser_WithRaceTitleContainingOnlySpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\",;\"\"#\",,,,,,,,,4.5",
            ",1051,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);
        var parser = new EvtParser(provider);

        try
        {
            // Act
            var races = await parser.ParseAsync();
            var raceList = races.ToList();

            // Assert
            Assert.Single(raceList);
            var race = raceList[0];
            Assert.Equal("21A", race.RaceNumber);
            Assert.Equal(",;\"#", race.RaceTitle);
            Assert.Equal(4.5m, race.NumberOfLaps);
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
            "21A,,,\"Test Race\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
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
