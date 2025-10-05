using GcpvWatcher.App.Converters;
using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using System.Globalization;
using Xunit;

namespace GcpvWatcher.Tests.Converters;

public class RacersToStringConverterTests
{
    private readonly RacersToStringConverter _converter;

    public RacersToStringConverterTests()
    {
        _converter = new RacersToStringConverter();
    }

    [Fact]
    public void Convert_WithNoRacers_ReturnsNoRacers()
    {
        // Arrange
        var racers = new Dictionary<int, int>();
        RacerDataService.UpdateRacers(new Dictionary<int, Racer>());

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("No racers", result);
    }

    [Fact]
    public void Convert_WithRacersButNoRacerData_ReturnsRacerIds()
    {
        // Arrange
        var racers = new Dictionary<int, int>
        {
            { 1, 1 },
            { 2, 2 }
        };
        RacerDataService.UpdateRacers(new Dictionary<int, Racer>());

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    1{Environment.NewLine}Lane  2,    2", result);
    }

    [Fact]
    public void Convert_WithRacersAndRacerData_ReturnsRacerNames()
    {
        // Arrange
        var racers = new Dictionary<int, int>
        {
            { 1, 1 },
            { 2, 2 }
        };
        var racerData = new Dictionary<int, Racer>
        {
            { 1, new Racer(1, "Smith", "John", "Team A") },
            { 2, new Racer(2, "Johnson", "Jane", "Team B") }
        };
        RacerDataService.UpdateRacers(racerData);

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    1 - John Smith (Team A){Environment.NewLine}Lane  2,    2 - Jane Johnson (Team B)", result);
    }

    [Fact]
    public void Convert_WithMixedRacerData_ReturnsNamesForKnownRacers()
    {
        // Arrange
        var racers = new Dictionary<int, int>
        {
            { 1, 1 },
            { 2, 2 },
            { 3, 3 }
        };
        var racerData = new Dictionary<int, Racer>
        {
            { 1, new Racer(1, "Smith", "John", "Team A") },
            { 3, new Racer(3, "Brown", "Bob", "Team C") }
        };
        RacerDataService.UpdateRacers(racerData);

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    1 - John Smith (Team A){Environment.NewLine}Lane  2,    2{Environment.NewLine}Lane  3,    3 - Bob Brown (Team C)", result);
    }

    [Fact]
    public void Convert_OrdersByLane()
    {
        // Arrange
        var racers = new Dictionary<int, int>
        {
            { 3, 1 },
            { 1, 3 },
            { 2, 2 }
        };
        var racerData = new Dictionary<int, Racer>
        {
            { 1, new Racer(1, "Smith", "John", "Team A") },
            { 2, new Racer(2, "Johnson", "Jane", "Team B") },
            { 3, new Racer(3, "Brown", "Bob", "Team C") }
        };
        RacerDataService.UpdateRacers(racerData);

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    3 - Bob Brown (Team C){Environment.NewLine}Lane  2,    2 - Jane Johnson (Team B){Environment.NewLine}Lane  3,    1 - John Smith (Team A)", result);
    }
}
