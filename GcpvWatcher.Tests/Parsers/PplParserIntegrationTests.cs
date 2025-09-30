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
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        Assert.True(racers.Count > 0, "Should have parsed at least one racer from the file");
        
        // Check some specific racers from the file
        var lopez = racers.FirstOrDefault(r => r.RacerId == 116);
        Assert.NotNull(lopez);
        Assert.Equal("Lopez", lopez.LastName);
        Assert.Equal("Nancy", lopez.FirstName);
        Assert.Equal("St. Lawrence", lopez.Affiliation);

        var belanger = racers.FirstOrDefault(r => r.RacerId == 667);
        Assert.NotNull(belanger);
        Assert.Equal("Bélanger", belanger.LastName);
        Assert.Equal("Daniel", belanger.FirstName);
        Assert.Equal("CPV Gatineau", belanger.Affiliation);

        var baileyMartin = racers.FirstOrDefault(r => r.RacerId == 693);
        Assert.NotNull(baileyMartin);
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
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        var belanger = racers.FirstOrDefault(r => r.LastName == "Bélanger");
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
        var result = await parser.ParseAsync();
        var racers = result.ToList();

        // Assert
        var stLawrenceRacers = racers.Where(r => r.Affiliation == "St. Lawrence").ToList();
        Assert.True(stLawrenceRacers.Count > 0, "Should have racers from St. Lawrence");

        var kitchenerWaterlooRacers = racers.Where(r => r.Affiliation == "Kitchener-Waterloo").ToList();
        Assert.True(kitchenerWaterlooRacers.Count > 0, "Should have racers from Kitchener-Waterloo");

        var montrealCentreSudRacers = racers.Where(r => r.Affiliation == "CPV Montreal-Centre-Sud").ToList();
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
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
