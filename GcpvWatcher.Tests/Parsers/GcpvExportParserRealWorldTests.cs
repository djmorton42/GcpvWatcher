using GcpvWatcher.App.Models;
using GcpvWatcher.App.Parsers;
using GcpvWatcher.App.Providers;
using GcpvWatcher.App.Services;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Parsers;

public class GcpvExportParserRealWorldTests
{
    private readonly string _sampleFilePath = Path.Combine("/Users/danielmorton/source/speedskating/GcpvWatcher", "etc", "RaceSample.csv");
    private readonly Dictionary<string, KeyFieldConfig> _keyFields;

    public GcpvExportParserRealWorldTests()
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
    public async Task ParseAsync_WithRealSampleData_ReturnsCorrectGroupedRaces()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Equal(3, raceList.Count);

        // Verify race 25A
        var race25A = raceList.First(r => r.RaceNumber == "25A");
        Assert.Equal("1500 111M", race25A.TrackParams);
        Assert.Equal("Open Men B  male", race25A.RaceGroup);
        Assert.Equal("Heat, 2 +2", race25A.Stage);
        Assert.Equal(7, race25A.Racers.Count);

        // Verify specific racers in race 25A
        var racer1 = race25A.Racers.First(r => r.Lane == "1");
        Assert.Equal("689 Dixon, Frankie", racer1.Racer);
        Assert.Equal("Hamilton", racer1.Affiliation);

        var racer2 = race25A.Racers.First(r => r.Lane == "2");
        Assert.Equal("963 White, Gale", racer2.Racer);
        Assert.Equal("Hamilton", racer2.Affiliation);

        var racer3 = race25A.Racers.First(r => r.Lane == "3");
        Assert.Equal("662 Adams, Nico", racer3.Racer);
        Assert.Equal("Oakville", racer3.Affiliation);

        // Verify race 25B
        var race25B = raceList.First(r => r.RaceNumber == "25B");
        Assert.Equal("1500 111M", race25B.TrackParams);
        Assert.Equal("Open Men B  male", race25B.RaceGroup);
        Assert.Equal("Heat, 2 +2", race25B.Stage);
        Assert.Equal(7, race25B.Racers.Count);

        // Verify specific racers in race 25B
        var racerB1 = race25B.Racers.First(r => r.Lane == "1");
        Assert.Equal("1167 Ellis, Dell", racerB1.Racer);
        Assert.Equal("Toronto", racerB1.Affiliation);

        var racerB2 = race25B.Racers.First(r => r.Lane == "2");
        Assert.Equal("505 Lee, Parker", racerB2.Racer);
        Assert.Equal("Milton", racerB2.Affiliation);

        // Verify race 25C
        var race25C = raceList.First(r => r.RaceNumber == "25C");
        Assert.Equal("1500 111M", race25C.TrackParams);
        Assert.Equal("Open Men B  male", race25C.RaceGroup);
        Assert.Equal("Heat, 2 +2", race25C.Stage);
        Assert.Equal(8, race25C.Racers.Count);

        // Verify specific racers in race 25C
        var racerC1 = race25C.Racers.First(r => r.Lane == "1");
        Assert.Equal("729 Lopez, Logan", racerC1.Racer);
        Assert.Equal("Newmarket", racerC1.Affiliation);

        var racerC8 = race25C.Racers.First(r => r.Lane == "8");
        Assert.Equal("1079 Stewart, Jamie", racerC8.Racer);
        Assert.Equal("Ottawa", racerC8.Affiliation);
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesAllRacersAreGroupedCorrectly()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        Assert.Equal(3, raceList.Count);

        // Count total racers across all races
        var totalRacers = raceList.Sum(r => r.Racers.Count);
        Assert.Equal(22, totalRacers); // 7 + 7 + 8 = 22 racers

        // Verify all racers have unique lane numbers within their race
        foreach (var race in raceList)
        {
            var laneNumbers = race.Racers.Select(r => r.Lane).ToList();
            Assert.Equal(laneNumbers.Count, laneNumbers.Distinct().Count());
        }

        // Verify all races have the same track params, race group, and stage
        foreach (var race in raceList)
        {
            Assert.Equal("1500 111M", race.TrackParams);
            Assert.Equal("Open Men B  male", race.RaceGroup);
            Assert.Equal("Heat, 2 +2", race.Stage);
        }
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesAffiliationDiversity()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        var allAffiliations = raceList.SelectMany(r => r.Racers.Select(racer => racer.Affiliation)).Distinct().ToList();
        
        // Verify we have multiple different affiliations
        Assert.True(allAffiliations.Count > 5);
        
        // Verify specific affiliations exist
        Assert.Contains("Hamilton", allAffiliations);
        Assert.Contains("Oakville", allAffiliations);
        Assert.Contains("Toronto", allAffiliations);
        Assert.Contains("Milton", allAffiliations);
        Assert.Contains("Thunder Bay", allAffiliations);
        Assert.Contains("St Lawrence", allAffiliations);
        Assert.Contains("Ottawa", allAffiliations);
        Assert.Contains("Newmarket", allAffiliations);
        Assert.Contains("London", allAffiliations);
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesRacerNameFormat()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        var allRacers = raceList.SelectMany(r => r.Racers).ToList();
        
        // Verify all racers have names in "Number LastName, FirstName" format
        foreach (var racer in allRacers)
        {
            Assert.True(racer.Racer.Contains(","), $"Racer name should contain comma: {racer.Racer}");
            Assert.True(racer.Racer.Contains(" "), $"Racer name should contain space: {racer.Racer}");
            
            // Verify it starts with a number
            var firstSpaceIndex = racer.Racer.IndexOf(' ');
            var numberPart = racer.Racer.Substring(0, firstSpaceIndex);
            Assert.True(int.TryParse(numberPart, out _), $"Racer name should start with number: {racer.Racer}");
        }
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesLaneNumbering()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        foreach (var race in raceList)
        {
            var laneNumbers = race.Racers.Select(r => int.Parse(r.Lane)).OrderBy(x => x).ToList();
            
            // Verify lane numbers are sequential starting from 1
            for (int i = 0; i < laneNumbers.Count; i++)
            {
                Assert.Equal(i + 1, laneNumbers[i]);
            }
        }
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesRacersAreSortedByLaneNumber()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        foreach (var race in raceList)
        {
            var laneNumbers = race.Racers.Select(r => int.Parse(r.Lane)).ToList();
            
            // Verify racers are sorted by lane number in the list order
            for (int i = 0; i < laneNumbers.Count - 1; i++)
            {
                Assert.True(laneNumbers[i] <= laneNumbers[i + 1], 
                    $"Racers in race {race.RaceNumber} are not sorted by lane number. " +
                    $"Lane {laneNumbers[i]} appears before lane {laneNumbers[i + 1]}");
            }
        }
    }

    [Fact]
    public async Task ParseAsync_WithRealSampleData_VerifiesRaceNumberFormat()
    {
        // Arrange
        var provider = new GcpvExportDataFileProvider(_sampleFilePath);
        var parser = new GcpvExportParser(provider, _keyFields);

        // Act
        var raceData = await parser.ParseAsync();
        var raceList = raceData.ToList();

        // Assert
        var raceNumbers = raceList.Select(r => r.RaceNumber).OrderBy(x => x).ToList();
        
        // Verify we have the expected race numbers
        Assert.Equal(new[] { "25A", "25B", "25C" }, raceNumbers);
        
        // Verify race number format (number followed by letter)
        foreach (var raceNumber in raceNumbers)
        {
            Assert.Matches(@"^\d+[A-Z]$", raceNumber);
        }
    }
}
