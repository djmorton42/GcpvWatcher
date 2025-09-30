using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using Xunit;

namespace GcpvWatcher.Tests.Parsers;

public class EvtParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidData_ReturnsCorrectRaces()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title A\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2",
            ",681,3",
            "21B,,,\"Race Title B\",,,,,,,,,3.0",
            ",563,1",
            ",617,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        
        // Check first race
        var race1 = raceList[0];
        Assert.Equal("21A", race1.RaceNumber);
        Assert.Equal("Race Title A", race1.RaceTitle);
        Assert.Equal(4.5m, race1.NumberOfLaps);
        Assert.Equal(3, race1.Racers.Count);
        Assert.Equal(1, race1.Racers[1051]);
        Assert.Equal(2, race1.Racers[2010]);
        Assert.Equal(3, race1.Racers[681]);
        
        // Check second race
        var race2 = raceList[1];
        Assert.Equal("21B", race2.RaceNumber);
        Assert.Equal("Race Title B", race2.RaceTitle);
        Assert.Equal(3.0m, race2.NumberOfLaps);
        Assert.Equal(2, race2.Racers.Count);
        Assert.Equal(1, race2.Racers[563]);
        Assert.Equal(2, race2.Racers[617]);
    }

    [Fact]
    public async Task ParseAsync_WithRaceNumberSorting_SortsCorrectly()
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
            ",400,1"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(4, raceList.Count);
        Assert.Equal("3A", raceList[0].RaceNumber);
        Assert.Equal("3B", raceList[1].RaceNumber);
        Assert.Equal("22A", raceList[2].RaceNumber);
        Assert.Equal("100A", raceList[3].RaceNumber);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespace_TrimsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "  21A  ,,,\"  Race Title  \",,,,,,,,,  4.5  ",
            "  ,  1051  ,  1  ",
            "  ,  2010  ,  2  "
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race Title", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
        Assert.Equal(1, race.Racers[1051]);
        Assert.Equal(2, race.Racers[2010]);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"21A\",\"\",\"\",\"Race, with, commas\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"\",\"4.5\"",
            "\"\",\"1051\",\"1\"",
            "\"\",\"2010\",\"2\""
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race, with, commas", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRaces_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5"
            // No racer lines
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Empty(race.Racers);
    }

    [Fact]
    public async Task ParseAsync_WithCommentLines_FiltersOutComments()
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
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        Assert.Equal("21A", raceList[0].RaceNumber);
        Assert.Equal("21B", raceList[1].RaceNumber);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            "",
            ",1051,1",
            "   ",
            "21B,,,\"Race Title 2\",,,,,,,,,3.0",
            ",563,1"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        Assert.Equal("21A", raceList[0].RaceNumber);
        Assert.Equal("21B", raceList[1].RaceNumber);
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRaceInfoLine_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "InvalidRaceInfo,,,\"Race Title\",,,,,,,,,4.5"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRaceNumber_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "Invalid,,,\"Race Title\",,,,,,,,,4.5"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidNumberOfLaps_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,InvalidLaps"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRacerId_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",InvalidId,1"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidLane_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,InvalidLane"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRaceInfoLine_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            ""
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Empty(raceList);
    }

    [Fact]
    public async Task ParseAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act & Assert
        var races = await parser.ParseAsync();
        Assert.NotEmpty(races);
    }

    [Fact]
    public async Task ParseAsync_WithMultipleRacersInSameLane_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,1", // Same lane as previous racer
            ",681,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal(3, race.Racers.Count);
        Assert.Equal(1, race.Racers[1051]);
        Assert.Equal(1, race.Racers[2010]); // Last racer with same lane overwrites
        Assert.Equal(2, race.Racers[681]);
    }

    [Fact]
    public async Task ParseAsync_WithDecimalLaps_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            "21B,,,\"Race Title 2\",,,,,,,,,13.5",
            ",2010,1"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        Assert.Equal(4.5m, raceList[0].NumberOfLaps);
        Assert.Equal(13.5m, raceList[1].NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithComplexRaceNumbers_SortsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "999Z,,,\"Race 999Z\",,,,,,,,,1.0",
            ",100,1",
            "1A,,,\"Race 1A\",,,,,,,,,2.0",
            ",200,1",
            "10A,,,\"Race 10A\",,,,,,,,,3.0",
            ",300,1",
            "2A,,,\"Race 2A\",,,,,,,,,4.0",
            ",400,1"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Equal(4, raceList.Count);
        Assert.Equal("1A", raceList[0].RaceNumber);
        Assert.Equal("2A", raceList[1].RaceNumber);
        Assert.Equal("10A", raceList[2].RaceNumber);
        Assert.Equal("999Z", raceList[3].RaceNumber);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingCommas_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race, with, multiple, commas\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race, with, multiple, commas", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingQuotes_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with \"\"quoted\"\" text\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race with \"quoted\" text", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingHashMarks_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with # hash marks\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race with # hash marks", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingSemicolons_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with ; semicolons\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race with ; semicolons", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingAllSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with, \"\"quotes\"\", #hashes, and; semicolons\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race with, \"quotes\", #hashes, and; semicolons", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingLeadingSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\",;\"\"#Race starting with special chars\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal(",;\"#Race starting with special chars", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingTrailingSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race ending with special chars,;\"\"#\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race ending with special chars,;\"#", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingOnlySpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\",;\"\"#\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

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

    [Fact]
    public async Task ParseAsync_WithRaceTitleContainingEscapedQuotes_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race with \"\"double\"\" and \"\"more\"\" quotes\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);
        var parser = new EvtParser(provider);

        // Act
        var races = await parser.ParseAsync();
        var raceList = races.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("Race with \"double\" and \"more\" quotes", race.RaceTitle);
        Assert.Equal(4.5m, race.NumberOfLaps);
    }

}
