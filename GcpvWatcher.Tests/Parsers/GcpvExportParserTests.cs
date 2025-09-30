using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using Xunit;

namespace GcpvWatcher.Tests.Parsers;

public class GcpvExportParserTests
{
    private readonly Dictionary<string, KeyFieldConfig> _keyFields;

    public GcpvExportParserTests()
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
    public async Task ParseAsync_WithValidData_ReturnsCorrectRaceData()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("25A", race.RaceNumber);
        Assert.Equal("1500 111M", race.TrackParams);
        Assert.Equal("Open Men B  male", race.RaceGroup);
        Assert.Equal("Heat, 2 +2", race.Stage);
        Assert.Single(race.Racers);
        
        var racer = race.Racers[0];
        Assert.Equal("1", racer.Lane);
        Assert.Equal("689 PORTER, REGGIE", racer.Racer);
        Assert.Equal("Hamilton", racer.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithMultipleRows_ReturnsGroupedRaceData()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"",
            "\"Event :\",\"1000M\",\"Open Men A\",\"Stage :\",\"Heat 1\",,,\"Race\",\"22B\",,,\"Lane\",\"Skaters\",\"Club\",2,\"456 JOHNSON, BOB\",\"Montreal\",\"28-Sep-25   9:45:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        
        var race1 = raceList[0];
        Assert.Equal("21A", race1.RaceNumber);
        Assert.Equal("500M", race1.TrackParams);
        Assert.Equal("Open Women A", race1.RaceGroup);
        Assert.Equal("Final", race1.Stage);
        Assert.Single(race1.Racers);
        Assert.Equal("1", race1.Racers[0].Lane);
        Assert.Equal("123 SMITH, JANE", race1.Racers[0].Racer);
        Assert.Equal("Toronto", race1.Racers[0].Affiliation);
        
        var race2 = raceList[1];
        Assert.Equal("22B", race2.RaceNumber);
        Assert.Equal("1000M", race2.TrackParams);
        Assert.Equal("Open Men A", race2.RaceGroup);
        Assert.Equal("Heat 1", race2.Stage);
        Assert.Single(race2.Racers);
        Assert.Equal("2", race2.Racers[0].Lane);
        Assert.Equal("456 JOHNSON, BOB", race2.Racers[0].Racer);
        Assert.Equal("Montreal", race2.Racers[0].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithMultipleRacersInSameRace_GroupsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"456 JOHNSON, MARY\",\"Montreal\",\"28-Sep-25   9:30:00 AM\"",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",3,\"789 BROWN, SUE\",\"Vancouver\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("21A", race.RaceNumber);
        Assert.Equal("500M", race.TrackParams);
        Assert.Equal("Open Women A", race.RaceGroup);
        Assert.Equal("Final", race.Stage);
        Assert.Equal(3, race.Racers.Count);
        
        Assert.Equal("1", race.Racers[0].Lane);
        Assert.Equal("123 SMITH, JANE", race.Racers[0].Racer);
        Assert.Equal("Toronto", race.Racers[0].Affiliation);
        
        Assert.Equal("2", race.Racers[1].Lane);
        Assert.Equal("456 JOHNSON, MARY", race.Racers[1].Racer);
        Assert.Equal("Montreal", race.Racers[1].Affiliation);
        
        Assert.Equal("3", race.Racers[2].Lane);
        Assert.Equal("789 BROWN, SUE", race.Racers[2].Racer);
        Assert.Equal("Vancouver", race.Racers[2].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespace_TrimsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"  Event :  \",\"  1500 111M  \",\"  Open Men B  male  \",\"  Stage :  \",\"  Heat, 2 +2  \",,,\"  Race  \",\"  25A  \",,,\"  Lane  \",\"  Skaters  \",\"  Club  \",1,\"  689 PORTER, REGGIE  \",\"  Hamilton  \",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("25A", race.RaceNumber);
        Assert.Equal("1500 111M", race.TrackParams);
        Assert.Equal("Open Men B  male", race.RaceGroup);
        Assert.Equal("Heat, 2 +2", race.Stage);
        Assert.Single(race.Racers);
        
        var racer = race.Racers[0];
        Assert.Equal("1", racer.Lane);
        Assert.Equal("689 PORTER, REGGIE", racer.Racer);
        Assert.Equal("Hamilton", racer.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"\"\"500M with, commas\"\"\",\"\"\"Open Women, with quotes\"\"\",\"Stage :\",\"\"\"Final, with quotes\"\"\",,,\"Race\",\"\"\"25A, with quotes\"\"\",,,\"Lane\",\"Skaters\",\"Club\",1,\"\"\"123 SMITH, JANE\"\"\",\"\"\"Toronto, ON\"\"\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("\"25A, with quotes\"", race.RaceNumber);
        Assert.Equal("\"500M with, commas\"", race.TrackParams);
        Assert.Equal("\"Open Women, with quotes\"", race.RaceGroup);
        Assert.Equal("\"Final, with quotes\"", race.Stage);
        Assert.Single(race.Racers);
        
        var racer = race.Racers[0];
        Assert.Equal("1", racer.Lane);
        Assert.Equal("\"123 SMITH, JANE\"", racer.Racer);
        Assert.Equal("\"Toronto, ON\"", racer.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRows_FiltersOutEmptyRows()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "   ",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Equal(2, raceList.Count);
        Assert.Equal("25A", raceList[0].RaceNumber);
        Assert.Equal("21A", raceList[1].RaceNumber);
    }

    [Fact]
    public async Task ParseAsync_WithMissingKey_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "\"WrongKey\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithOutOfBoundsOffset_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRowFormat_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "InvalidRowFormat"
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRow_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[]
        {
            ""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Empty(raceList);
    }

    [Fact]
    public void ParseAsync_WithNullDataProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GcpvExportParser(null!, _keyFields));
    }

    [Fact]
    public void ParseAsync_WithNullKeyFields_ThrowsArgumentNullException()
    {
        // Arrange
        var testData = new[] { "\"Event :\",\"1500 111M\"" };
        var provider = new GcpvExportDataStaticProvider(testData);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GcpvExportParser(provider, null!));
    }

    [Fact]
    public async Task ParseAsync_WithMissingKeyFieldConfig_ThrowsException()
    {
        // Arrange
        var incompleteKeyFields = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) }
            // Missing other required fields
        };
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, incompleteKeyFields);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithCaseInsensitiveKeyMatching_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"event :\",\"1500 111M\",\"Open Men B  male\",\"stage :\",\"Heat, 2 +2\",,,\"race\",\"25A\",,,\"lane\",\"skaters\",\"club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("25A", race.RaceNumber);
        Assert.Equal("1500 111M", race.TrackParams);
        Assert.Equal("Open Men B  male", race.RaceGroup);
        Assert.Equal("Heat, 2 +2", race.Stage);
        Assert.Single(race.Racers);
        
        var racer = race.Racers[0];
        Assert.Equal("1", racer.Lane);
        Assert.Equal("689 PORTER, REGGIE", racer.Racer);
        Assert.Equal("Hamilton", racer.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithSpecialCharactersInValues_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M with # and ; special chars\",\"Open Men B with, commas and \"quotes\"\",\"Stage :\",\"Heat, 2 +2 with # and ;\",,,\"Race\",\"25A with # and ;\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE with # and ;\",\"Hamilton with, commas\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

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

    [Fact]
    public async Task ParseAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act & Assert
        var raceData = await parser.ParseAsync();
        Assert.NotEmpty(raceData);
    }

    [Fact]
    public async Task ParseAsync_WithUnsortedRacers_SortsByLaneNumber()
    {
        // Arrange - racers in unsorted order by lane
        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",3,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"456 JOHNSON, MARY\",\"Montreal\",\"28-Sep-25   9:30:00 AM\"",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"789 BROWN, SUE\",\"Vancouver\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal(3, race.Racers.Count);
        
        // Verify racers are sorted by lane number
        Assert.Equal("1", race.Racers[0].Lane);
        Assert.Equal("456 JOHNSON, MARY", race.Racers[0].Racer);
        Assert.Equal("Montreal", race.Racers[0].Affiliation);
        
        Assert.Equal("2", race.Racers[1].Lane);
        Assert.Equal("789 BROWN, SUE", race.Racers[1].Racer);
        Assert.Equal("Vancouver", race.Racers[1].Affiliation);
        
        Assert.Equal("3", race.Racers[2].Lane);
        Assert.Equal("123 SMITH, JANE", race.Racers[2].Racer);
        Assert.Equal("Toronto", race.Racers[2].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_RemovesStopWordsFromRaceGroup()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B", race.RaceGroup); // "male" should be removed
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_RemovesLongestMatchOnly()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  Genders Mixed\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B", race.RaceGroup); // "Genders Mixed" should be removed, not "male"
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_CaseSensitiveMatching()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "MALE", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B  male", race.RaceGroup); // "male" should NOT be removed due to case difference (MALE vs male)
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_ExactCaseMatch_RemovesStopWord()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B", race.RaceGroup); // "male" should be removed with exact case match
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_NoMatch_ReturnsOriginalValue()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  youth\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B  youth", race.RaceGroup); // No stop word match, original value preserved
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_EmptyStopWords_ReturnsOriginalValue()
    {
        // Arrange
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new string[0]) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B  male", race.RaceGroup); // No stop words configured, original value preserved
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_DoesNotRemovePartialMatches()
    {
        // Arrange - Test that "male" is not removed from "female"
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open Women A female\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Women A", race.RaceGroup); // "female" should be removed, not "male" from "female"
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_HandlesStopWordInMiddle()
    {
        // Arrange - Test that stop words in the middle are not removed
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open male Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open male Women A", race.RaceGroup); // "male" in middle should not be removed
    }

    [Fact]
    public async Task ParseAsync_WithSuffixStopWords_HandlesMultipleStopWordsAtEnd()
    {
        // Arrange - Test that only the longest matching stop word is removed
        var keyFieldsWithStopWords = new Dictionary<string, KeyFieldConfig>
        {
            { "track_params", new KeyFieldConfig("Event :", 1) },
            { "race_group", new KeyFieldConfig("Event :", 2, new[] { "male", "female", "Genders Mixed" }) },
            { "stage", new KeyFieldConfig("Stage :", 1) },
            { "race_number", new KeyFieldConfig("Race", 1) },
            { "lane", new KeyFieldConfig("Lane", 3) },
            { "racer", new KeyFieldConfig("Skaters", 3) },
            { "affiliation", new KeyFieldConfig("Club", 3) }
        };

        var testData = new[]
        {
            "\"Event :\",\"500M\",\"Open Men B  male female\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);
        var parser = new GcpvExportParser(provider, keyFieldsWithStopWords);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Single(raceList);
        var race = raceList[0];
        Assert.Equal("Open Men B  male", race.RaceGroup); // Only "female" should be removed (longest match)
    }
}