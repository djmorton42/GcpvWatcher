using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Parsers;

public class PplParserTests
{
    [Fact]
    public async Task ParseAsync_WithValidData_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "315,Taylor,Dorothy,CPV Gatineau",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(3, racers.Count);
        
        // Check first racer
        Assert.True(racers.TryGetValue("116", out var racer1));
        Assert.Equal("116", racer1.RacerId);
        Assert.Equal("Lopez", racer1.LastName);
        Assert.Equal("Nancy", racer1.FirstName);
        Assert.Equal("St. Lawrence", racer1.Affiliation);
        
        // Check second racer
        Assert.True(racers.TryGetValue("315", out var racer2));
        Assert.Equal("315", racer2.RacerId);
        Assert.Equal("Taylor", racer2.LastName);
        Assert.Equal("Dorothy", racer2.FirstName);
        Assert.Equal("CPV Gatineau", racer2.Affiliation);
        
        // Check third racer
        Assert.True(racers.TryGetValue("322", out var racer3));
        Assert.Equal("322", racer3.RacerId);
        Assert.Equal("Adams", racer3.LastName);
        Assert.Equal("Justin", racer3.FirstName);
        Assert.Equal("Milton", racer3.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespace_TrimsCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "  116  ,  Lopez  ,  Nancy  ,  St. Lawrence  ",
            "  315  ,  Taylor  ,  Dorothy  ,  CPV Gatineau  "
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(2, racers.Count);
        
        Assert.True(racers.TryGetValue("116", out var racer1));
        Assert.Equal("Lopez", racer1.LastName);
        Assert.Equal("Nancy", racer1.FirstName);
        Assert.Equal("St. Lawrence", racer1.Affiliation);
        
        Assert.True(racers.TryGetValue("315", out var racer2));
        Assert.Equal("Taylor", racer2.LastName);
        Assert.Equal("Dorothy", racer2.FirstName);
        Assert.Equal("CPV Gatineau", racer2.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithQuotedFields_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "\"116\",\"Lopez\",\"Nancy\",\"St. Lawrence\"",
            "\"315\",\"Taylor\",\"Dorothy\",\"CPV Gatineau\""
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(2, racers.Count);
        
        Assert.True(racers.TryGetValue("116", out var racer1));
        Assert.Equal("Lopez", racer1.LastName);
        Assert.Equal("Nancy", racer1.FirstName);
        Assert.Equal("St. Lawrence", racer1.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "667,Bélanger,Daniel,CPV Gatineau",
            "693,Bailey Martin,Helen,Newmarket",
            "669,Myers,Jose,CRCP Laval"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.True(racers.TryGetValue("667", out var racer1));
        Assert.Equal("Bélanger", racer1.LastName);
        Assert.Equal("Daniel", racer1.FirstName);
        Assert.Equal("CPV Gatineau", racer1.Affiliation);
        
        Assert.True(racers.TryGetValue("693", out var racer2));
        Assert.Equal("Bailey Martin", racer2.LastName);
        Assert.Equal("Helen", racer2.FirstName);
        Assert.Equal("Newmarket", racer2.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "",
            "315,Taylor,Dorothy,CPV Gatineau",
            "   ",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(3, racers.Count);
        Assert.True(racers.ContainsKey("116"));
        Assert.True(racers.ContainsKey("315"));
        Assert.True(racers.ContainsKey("322"));
    }

    [Fact]
    public async Task ParseAsync_WithCommentLines_FiltersOutComments()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            ";This is a comment line",
            "315,Taylor,Dorothy,CPV Gatineau",
            "#Another comment line",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(3, racers.Count);
        Assert.True(racers.ContainsKey("116"));
        Assert.True(racers.ContainsKey("315"));
        Assert.True(racers.ContainsKey("322"));
    }

    [Fact]
    public async Task ParseAsync_WithAlphanumericRacerId_AcceptsRacer()
    {
        // Arrange - RacerId can be alphanumeric per current parser behavior
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "InvalidId,Taylor,Dorothy,CPV Gatineau",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert - all three racers are accepted (alphanumeric IDs are valid)
        Assert.Equal(3, racers.Count);
        Assert.True(racers.ContainsKey("116"));
        Assert.True(racers.ContainsKey("InvalidId"));
        Assert.True(racers.ContainsKey("322"));
        Assert.Equal("Taylor", racers["InvalidId"].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithMissingOptionalFields_AcceptsWithEmptyValues()
    {
        // Arrange - FirstName is optional; parser uses empty string when missing
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "315,Taylor,,CPV Gatineau", // Missing first name
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert - all three racers are accepted; missing optional field is empty string
        Assert.Equal(3, racers.Count);
        Assert.True(racers.ContainsKey("116"));
        Assert.True(racers.ContainsKey("315"));
        Assert.True(racers.ContainsKey("322"));
        Assert.Equal("", racers["315"].FirstName);
        Assert.Equal("Taylor", racers["315"].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyData_ReturnsEmptyDictionary()
    {
        // Arrange
        var testData = new string[0];
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Empty(racers);
    }

    [Fact]
    public async Task ParseAsync_WithOnlyComments_ReturnsEmptyDictionary()
    {
        // Arrange
        var testData = new[]
        {
            ";This is a comment line",
            "#Another comment line",
            "  ;Comment with leading spaces"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Empty(racers);
    }

    [Fact]
    public async Task ParseAsync_WithDuplicateRacerIds_KeepsLastOne()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "116,Taylor,Dorothy,CPV Gatineau", // Duplicate ID
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(2, racers.Count);
        Assert.True(racers.TryGetValue("116", out var racer116));
        Assert.Equal("Taylor", racer116.LastName); // Should be the last one
        Assert.Equal("Dorothy", racer116.FirstName);
        Assert.Equal("CPV Gatineau", racer116.Affiliation);
        
        Assert.True(racers.TryGetValue("322", out var racer322));
        Assert.Equal("Adams", racer322.LastName);
    }

    [Fact]
    public async Task ParseAsync_WithExtraCsvFields_UsesFirstFourFields()
    {
        // Arrange - CsvHelper maps first 4 fields to Racer; extra fields are ignored
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "Invalid,CSV,Format,With,Too,Many,Fields",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert - all three rows produce a racer (extra columns are ignored)
        Assert.Equal(3, racers.Count);
        Assert.True(racers.ContainsKey("116"));
        Assert.True(racers.ContainsKey("Invalid"));
        Assert.True(racers.ContainsKey("322"));
        Assert.Equal("CSV", racers["Invalid"].LastName);
        Assert.Equal("Format", racers["Invalid"].FirstName);
    }

    [Fact]
    public void ParseAsync_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PplParser(null!));
    }

    [Fact]
    public async Task ParseAsync_WithAffiliationContainingCommas_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,\"St. Lawrence, ON\"",
            "315,Taylor,Dorothy,\"CPV Gatineau, QC\""
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(2, racers.Count);
        
        Assert.True(racers.TryGetValue("116", out var racer1));
        Assert.Equal("St. Lawrence, ON", racer1.Affiliation);
        
        Assert.True(racers.TryGetValue("315", out var racer2));
        Assert.Equal("CPV Gatineau, QC", racer2.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithNamesContainingQuotes_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "116,\"O'Connor\",Nancy,St. Lawrence",
            "315,\"D'Angelo\",Dorothy,CPV Gatineau"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.Equal(2, racers.Count);
        
        Assert.True(racers.TryGetValue("116", out var racer1));
        Assert.Equal("O'Connor", racer1.LastName);
        
        Assert.True(racers.TryGetValue("315", out var racer2));
        Assert.Equal("D'Angelo", racer2.LastName);
    }
}