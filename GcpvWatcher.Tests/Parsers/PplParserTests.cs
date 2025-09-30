using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;

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
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.Equal(116, racers[0].RacerId);
        Assert.Equal("Lopez", racers[0].LastName);
        Assert.Equal("Nancy", racers[0].FirstName);
        Assert.Equal("St. Lawrence", racers[0].Affiliation);

        Assert.Equal(315, racers[1].RacerId);
        Assert.Equal("Taylor", racers[1].LastName);
        Assert.Equal("Dorothy", racers[1].FirstName);
        Assert.Equal("CPV Gatineau", racers[1].Affiliation);

        Assert.Equal(322, racers[2].RacerId);
        Assert.Equal("Adams", racers[2].LastName);
        Assert.Equal("Justin", racers[2].FirstName);
        Assert.Equal("Milton", racers[2].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithTwoWordLastNames_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Bailey Martin,Helen,Newmarket",
            "101,Van Der Berg,John,Toronto",
            "102,De La Cruz,Maria,Montreal"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.Equal("Bailey Martin", racers[0].LastName);
        Assert.Equal("Van Der Berg", racers[1].LastName);
        Assert.Equal("De La Cruz", racers[2].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithTwoWordFirstNames_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,Mary Jane,Toronto",
            "101,Johnson,Jean Pierre,Montreal",
            "102,Brown,Anna Marie,Kingston"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.Equal("Mary Jane", racers[0].FirstName);
        Assert.Equal("Jean Pierre", racers[1].FirstName);
        Assert.Equal("Anna Marie", racers[2].FirstName);
    }

    [Fact]
    public async Task ParseAsync_WithAccentedCharacters_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Bélanger,Daniel,CPV Gatineau",
            "101,François,Marie,Québec",
            "102,Antonio,José,Mexico City",
            "103,Müller,Hans,Berlin",
            "104,Østergaard,Lars,Copenhagen"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(5, racers.Count);
        
        Assert.Equal("Bélanger", racers[0].LastName);
        Assert.Equal("François", racers[1].LastName);
        Assert.Equal("José", racers[2].FirstName);
        Assert.Equal("Müller", racers[3].LastName);
        Assert.Equal("Østergaard", racers[4].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithMultiWordCities_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Kitchener-Waterloo",
            "101,Johnson,Jane,St. Lawrence",
            "102,Brown,Bob,New York City",
            "103,Davis,Alice,San Francisco Bay Area"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(4, racers.Count);
        
        Assert.Equal("Kitchener-Waterloo", racers[0].Affiliation);
        Assert.Equal("St. Lawrence", racers[1].Affiliation);
        Assert.Equal("New York City", racers[2].Affiliation);
        Assert.Equal("San Francisco Bay Area", racers[3].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithHyphenatedNames_ReturnsCorrectRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith-Jones,John,Toronto",
            "101,Johnson-Williams,Jane,Montreal",
            "102,Brown-Davis,Bob,Kingston"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.Equal("Smith-Jones", racers[0].LastName);
        Assert.Equal("Johnson-Williams", racers[1].LastName);
        Assert.Equal("Brown-Davis", racers[2].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyLines_IgnoresEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "",
            "   ",
            "101,Johnson,Jane,Montreal",
            ""
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(2, racers.Count);
        Assert.Equal("Smith", racers[0].LastName);
        Assert.Equal("Johnson", racers[1].LastName);
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRowFormat_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "invalid,row",
            "101,Johnson,Jane,Montreal"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithInvalidRacerId_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "invalid,Johnson,Jane,Montreal",
            "102,Brown,Bob,Kingston"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithQuotedFields_HandlesQuotesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "100,\"Smith, Jr.\",John,\"Toronto, ON\"",
            "101,Johnson,\"Mary Jane\",Montreal"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(2, racers.Count);
        Assert.Equal("Smith, Jr.", racers[0].LastName);
        Assert.Equal("Toronto, ON", racers[0].Affiliation);
        Assert.Equal("Mary Jane", racers[1].FirstName);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceAroundFields_TrimsWhitespace()
    {
        // Arrange
        var testData = new[]
        {
            " 100 , Smith , John , Toronto ",
            "  101  ,  Johnson  ,  Jane  ,  Montreal  ",
            "\t102\t,\tBrown\t,\tBob\t,\tKingston\t",
            " 103 , Bailey Martin , Helen , Kitchener-Waterloo ",
            " 104 , Bélanger , Daniel , CPV Gatineau "
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(5, racers.Count);
        
        // Check that all whitespace is trimmed
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("Smith", racers[0].LastName);
        Assert.Equal("John", racers[0].FirstName);
        Assert.Equal("Toronto", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Johnson", racers[1].LastName);
        Assert.Equal("Jane", racers[1].FirstName);
        Assert.Equal("Montreal", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("Brown", racers[2].LastName);
        Assert.Equal("Bob", racers[2].FirstName);
        Assert.Equal("Kingston", racers[2].Affiliation);

        Assert.Equal(103, racers[3].RacerId);
        Assert.Equal("Bailey Martin", racers[3].LastName);
        Assert.Equal("Helen", racers[3].FirstName);
        Assert.Equal("Kitchener-Waterloo", racers[3].Affiliation);

        Assert.Equal(104, racers[4].RacerId);
        Assert.Equal("Bélanger", racers[4].LastName);
        Assert.Equal("Daniel", racers[4].FirstName);
        Assert.Equal("CPV Gatineau", racers[4].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithOnlyWhitespaceFields_TrimsToEmptyStrings()
    {
        // Arrange
        var testData = new[]
        {
            " 100 ,   , John , Toronto ",
            " 101 , Smith ,   , Montreal ",
            " 102 , Johnson , Jane ,   ",
            " 103 ,   ,   ,   "
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(4, racers.Count);
        
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("", racers[0].LastName);
        Assert.Equal("John", racers[0].FirstName);
        Assert.Equal("Toronto", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Smith", racers[1].LastName);
        Assert.Equal("", racers[1].FirstName);
        Assert.Equal("Montreal", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("Johnson", racers[2].LastName);
        Assert.Equal("Jane", racers[2].FirstName);
        Assert.Equal("", racers[2].Affiliation);

        Assert.Equal(103, racers[3].RacerId);
        Assert.Equal("", racers[3].LastName);
        Assert.Equal("", racers[3].FirstName);
        Assert.Equal("", racers[3].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithMixedWhitespaceTypes_TrimsAllWhitespace()
    {
        // Arrange
        var testData = new[]
        {
            " \t 100 \t , \t Smith \t , \t John \t , \t Toronto \t ",
            "   101   ,   Johnson   ,   Jane   ,   Montreal   ",
            "   102   ,   Brown   ,   Bob   ,   Kingston   ",
            " \t 103 \t , \t Bailey Martin \t , \t Helen \t , \t Kitchener-Waterloo \t "
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(4, racers.Count);
        
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("Smith", racers[0].LastName);
        Assert.Equal("John", racers[0].FirstName);
        Assert.Equal("Toronto", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Johnson", racers[1].LastName);
        Assert.Equal("Jane", racers[1].FirstName);
        Assert.Equal("Montreal", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("Brown", racers[2].LastName);
        Assert.Equal("Bob", racers[2].FirstName);
        Assert.Equal("Kingston", racers[2].Affiliation);

        Assert.Equal(103, racers[3].RacerId);
        Assert.Equal("Bailey Martin", racers[3].LastName);
        Assert.Equal("Helen", racers[3].FirstName);
        Assert.Equal("Kitchener-Waterloo", racers[3].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceInQuotedFields_TrimsWhitespace()
    {
        // Arrange
        var testData = new[]
        {
            " 100 , \" Smith, Jr. \" , John , \" Toronto, ON \" ",
            " 101 , Johnson , \" Mary Jane \" , Montreal "
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(2, racers.Count);
        
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("Smith, Jr.", racers[0].LastName);
        Assert.Equal("John", racers[0].FirstName);
        Assert.Equal("Toronto, ON", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Johnson", racers[1].LastName);
        Assert.Equal("Mary Jane", racers[1].FirstName);
        Assert.Equal("Montreal", racers[1].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyOptionalFields_AcceptsEmptyStrings()
    {
        // Arrange
        var testData = new[]
        {
            "100,,,",  // Only racer ID, all other fields empty
            "101,Smith,,",  // Missing first name and affiliation
            "102,,John,",  // Missing last name and affiliation
            "103,,,Toronto",  // Missing first and last name
            "104,Smith,,Toronto",  // Missing first name
            "105,,John,Toronto",  // Missing last name
            "106,Smith,John,",  // Missing affiliation
            "107,Smith,,Toronto",  // Missing first name
            "108,,John,",  // Missing last name and affiliation
            "109,,,",  // Only racer ID
            "110, , , "  // Only whitespace in optional fields
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(11, racers.Count);
        
        // Test various combinations of empty fields
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("", racers[0].LastName);
        Assert.Equal("", racers[0].FirstName);
        Assert.Equal("", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Smith", racers[1].LastName);
        Assert.Equal("", racers[1].FirstName);
        Assert.Equal("", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("", racers[2].LastName);
        Assert.Equal("John", racers[2].FirstName);
        Assert.Equal("", racers[2].Affiliation);

        Assert.Equal(103, racers[3].RacerId);
        Assert.Equal("", racers[3].LastName);
        Assert.Equal("", racers[3].FirstName);
        Assert.Equal("Toronto", racers[3].Affiliation);

        Assert.Equal(104, racers[4].RacerId);
        Assert.Equal("Smith", racers[4].LastName);
        Assert.Equal("", racers[4].FirstName);
        Assert.Equal("Toronto", racers[4].Affiliation);

        Assert.Equal(105, racers[5].RacerId);
        Assert.Equal("", racers[5].LastName);
        Assert.Equal("John", racers[5].FirstName);
        Assert.Equal("Toronto", racers[5].Affiliation);

        Assert.Equal(106, racers[6].RacerId);
        Assert.Equal("Smith", racers[6].LastName);
        Assert.Equal("John", racers[6].FirstName);
        Assert.Equal("", racers[6].Affiliation);

        Assert.Equal(107, racers[7].RacerId);
        Assert.Equal("Smith", racers[7].LastName);
        Assert.Equal("", racers[7].FirstName);
        Assert.Equal("Toronto", racers[7].Affiliation);

        Assert.Equal(108, racers[8].RacerId);
        Assert.Equal("", racers[8].LastName);
        Assert.Equal("John", racers[8].FirstName);
        Assert.Equal("", racers[8].Affiliation);

        Assert.Equal(109, racers[9].RacerId);
        Assert.Equal("", racers[9].LastName);
        Assert.Equal("", racers[9].FirstName);
        Assert.Equal("", racers[9].Affiliation);

        Assert.Equal(110, racers[10].RacerId);
        Assert.Equal("", racers[10].LastName);
        Assert.Equal("", racers[10].FirstName);
        Assert.Equal("", racers[10].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithMissingRacerId_ThrowsException()
    {
        // Arrange
        var testData = new[]
        {
            ",Smith,John,Toronto",  // Missing racer ID
            " ,Smith,John,Toronto",  // Whitespace-only racer ID
            "invalid,Smith,John,Toronto"  // Invalid racer ID format
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => parser.ParseAsync());
    }

    [Fact]
    public async Task ParseAsync_WithValidRacerIdAndEmptyFields_CreatesValidRacers()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",  // All fields present
            "101,,,",  // Only racer ID
            "102,Smith,,",  // Missing first name and affiliation
            "103,,John,",  // Missing last name and affiliation
            "104,,,Toronto"  // Missing first and last name
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(5, racers.Count);
        
        // Verify all racers have valid IDs
        foreach (var racer in racers)
        {
            Assert.True(racer.RacerId > 0, $"Racer {racer.RacerId} should have a valid ID");
        }
    }

    [Fact]
    public async Task ParseAsync_WithQuotedEmptyFields_HandlesCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "100,\"\",\"\",\"\"",  // Quoted empty fields
            "101,\"Smith\",\"\",\"Toronto\"",  // Mixed quoted empty and non-empty
            "102,\"\",\"John\",\"\""  // Mixed quoted empty and non-empty
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(3, racers.Count);
        
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("", racers[0].LastName);
        Assert.Equal("", racers[0].FirstName);
        Assert.Equal("", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Smith", racers[1].LastName);
        Assert.Equal("", racers[1].FirstName);
        Assert.Equal("Toronto", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("", racers[2].LastName);
        Assert.Equal("John", racers[2].FirstName);
        Assert.Equal("", racers[2].Affiliation);
    }

    [Fact]
    public void Racer_WithEmptyStrings_HandlesCorrectly()
    {
        // Arrange & Act
        var racer = new Racer(100, "", "", "");

        // Assert
        Assert.Equal(100, racer.RacerId);
        Assert.Equal("", racer.LastName);
        Assert.Equal("", racer.FirstName);
        Assert.Equal("", racer.Affiliation);
        Assert.Equal("100,,,", racer.ToString());
    }

    [Fact]
    public void Racer_WithMixedEmptyAndNonEmptyFields_HandlesCorrectly()
    {
        // Arrange & Act
        var racer1 = new Racer(101, "Smith", "", "Toronto");
        var racer2 = new Racer(102, "", "John", "");
        var racer3 = new Racer(103, "", "", "Montreal");

        // Assert
        Assert.Equal("101,Smith,,Toronto", racer1.ToString());
        Assert.Equal("102,,John,", racer2.ToString());
        Assert.Equal("103,,,Montreal", racer3.ToString());
    }

    [Fact]
    public void Racer_EqualityWithEmptyStrings_WorksCorrectly()
    {
        // Arrange
        var racer1 = new Racer(100, "", "", "");
        var racer2 = new Racer(100, "", "", "");
        var racer3 = new Racer(100, "Smith", "", "");

        // Act & Assert
        Assert.Equal(racer1, racer2);
        Assert.NotEqual(racer1, racer3);
        Assert.Equal(racer1.GetHashCode(), racer2.GetHashCode());
        Assert.NotEqual(racer1.GetHashCode(), racer3.GetHashCode());
    }

    [Fact]
    public async Task ParseAsync_WithCommentLines_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            ";This is a comment line",
            "101,Johnson,Jane,Montreal",
            "#Another comment line",
            "102,Brown,Bob,Kingston",
            "  ;Comment with leading spaces",
            "  #Comment with leading spaces",
            "103,Davis,Alice,Hamilton"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.Equal(4, racers.Count);
        
        Assert.Equal(100, racers[0].RacerId);
        Assert.Equal("Smith", racers[0].LastName);
        Assert.Equal("John", racers[0].FirstName);
        Assert.Equal("Toronto", racers[0].Affiliation);

        Assert.Equal(101, racers[1].RacerId);
        Assert.Equal("Johnson", racers[1].LastName);
        Assert.Equal("Jane", racers[1].FirstName);
        Assert.Equal("Montreal", racers[1].Affiliation);

        Assert.Equal(102, racers[2].RacerId);
        Assert.Equal("Brown", racers[2].LastName);
        Assert.Equal("Bob", racers[2].FirstName);
        Assert.Equal("Kingston", racers[2].Affiliation);

        Assert.Equal(103, racers[3].RacerId);
        Assert.Equal("Davis", racers[3].LastName);
        Assert.Equal("Alice", racers[3].FirstName);
        Assert.Equal("Hamilton", racers[3].Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithEmptyRow_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[] { "" };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert - Empty rows are filtered out by the provider, so no racers should be returned
        Assert.Empty(racers);
    }

    [Fact]
    public async Task ParseAsync_WithWhitespaceOnlyRow_ReturnsEmptyResult()
    {
        // Arrange
        var testData = new[] { "   " };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert - Whitespace-only rows are filtered out by the provider, so no racers should be returned
        Assert.Empty(racers);
    }

    [Fact]
    public async Task ParseAsync_WithValidData_DoesNotThrow()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "101,Johnson,Jane,Montreal"
        };
        var provider = new PeopleDataStaticProvider(testData);
        var parser = new PplParser(provider);

        // Act & Assert
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        Assert.Equal(2, racers.Count);
        Assert.Equal("Smith", racers[0].LastName);
        Assert.Equal("Johnson", racers[1].LastName);
    }

    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PplParser(null!));
    }
}
