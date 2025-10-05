using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;

namespace GcpvWatcher.Tests.Parsers;

public class PplParserIntegrationTests
{
    [Fact]
    public async Task ParseAsync_WithActualLynxFile_ReturnsCorrectRacers()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "etc", "Lynx.ppl");
        var provider = new PeopleDataFileProvider(filePath);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        Assert.True(racers.Count > 0, "Should have parsed at least one racer from the file");
        
        // Check some specific racers from the file
        Assert.True(racers.TryGetValue(116, out var lopez));
        Assert.Equal("Lopez", lopez.LastName);
        Assert.Equal("Nancy", lopez.FirstName);
        Assert.Equal("St. Lawrence", lopez.Affiliation);

        Assert.True(racers.TryGetValue(667, out var belanger));
        Assert.Equal("Bélanger", belanger.LastName);
        Assert.Equal("Daniel", belanger.FirstName);
        Assert.Equal("CPV Gatineau", belanger.Affiliation);

        Assert.True(racers.TryGetValue(693, out var baileyMartin));
        Assert.Equal("Bailey Martin", baileyMartin.LastName);
        Assert.Equal("Helen", baileyMartin.FirstName);
        Assert.Equal("Newmarket", baileyMartin.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithActualLynxFile_HandlesAccentedCharacters()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "etc", "Lynx.ppl");
        var provider = new PeopleDataFileProvider(filePath);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        var belanger = racers.Values.FirstOrDefault(r => r.LastName == "Bélanger");
        Assert.NotNull(belanger);
        Assert.Equal("Bélanger", belanger.LastName);
    }

    [Fact]
    public async Task ParseAsync_WithActualLynxFile_HandlesMultiWordCities()
    {
        // Arrange
        var filePath = Path.Combine("..", "..", "..", "..", "etc", "Lynx.ppl");
        var provider = new PeopleDataFileProvider(filePath);
        var parser = new PplParser(provider);

        // Act
        var racers = await parser.ParseAsync();

        // Assert
        var stLawrenceRacers = racers.Values.Where(r => r.Affiliation == "St. Lawrence").ToList();
        Assert.True(stLawrenceRacers.Count > 0, "Should have racers from St. Lawrence");

        var kitchenerWaterlooRacers = racers.Values.Where(r => r.Affiliation == "Kitchener-Waterloo").ToList();
        Assert.True(kitchenerWaterlooRacers.Count > 0, "Should have racers from Kitchener-Waterloo");

        var montrealCentreSudRacers = racers.Values.Where(r => r.Affiliation == "CPV Montreal-Centre-Sud").ToList();
        Assert.True(montrealCentreSudRacers.Count > 0, "Should have racers from CPV Montreal-Centre-Sud");
    }

    [Fact]
    public void PplParser_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine("..", "..", "..", "..", "etc", "nonexistent.ppl");

        // Act & Assert
        // Should throw FileNotFoundException during provider construction
        Assert.Throws<FileNotFoundException>(() => new PeopleDataFileProvider(nonExistentFile));
    }

    [Fact]
    public async Task PplParser_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ppl");
        try
        {
            await File.WriteAllLinesAsync(tempFile, new[] { "100,Smith,John,Toronto" });
            var provider = new PeopleDataFileProvider(tempFile);
            var parser = new PplParser(provider);
            
            // Delete the file after construction
            File.Delete(tempFile);

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => parser.ParseAsync());
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task PplParser_WithFileContainingComments_FiltersOutComments()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ppl");
        try
        {
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
            await File.WriteAllLinesAsync(tempFile, testData);
            var provider = new PeopleDataFileProvider(tempFile);
            var parser = new PplParser(provider);

            // Act
            var racers = await parser.ParseAsync();

            // Assert
            Assert.Equal(4, racers.Count);
            
            Assert.True(racers.TryGetValue(100, out var racer100));
            Assert.Equal("Smith", racer100.LastName);
            Assert.Equal("John", racer100.FirstName);
            Assert.Equal("Toronto", racer100.Affiliation);

            Assert.True(racers.TryGetValue(101, out var racer101));
            Assert.Equal("Johnson", racer101.LastName);
            Assert.Equal("Jane", racer101.FirstName);
            Assert.Equal("Montreal", racer101.Affiliation);

            Assert.True(racers.TryGetValue(102, out var racer102));
            Assert.Equal("Brown", racer102.LastName);
            Assert.Equal("Bob", racer102.FirstName);
            Assert.Equal("Kingston", racer102.Affiliation);

            Assert.True(racers.TryGetValue(103, out var racer103));
            Assert.Equal("Davis", racer103.LastName);
            Assert.Equal("Alice", racer103.FirstName);
            Assert.Equal("Hamilton", racer103.Affiliation);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
