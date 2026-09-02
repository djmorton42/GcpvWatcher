using GcpvWatcher.App.Models;
using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class TrackParamsFormatterTests
{
    [Theory]
    [InlineData("1000 111M", "1000m")]
    [InlineData("1000 111m", "1000m")]
    [InlineData("1500 100M", "1500m")]
    [InlineData("500 100m", "500m")]
    [InlineData("  1000  111M  ", "1000m")]
    [InlineData("3000 111M", "3000m")]
    public void FormatForRaceTitle_WithDistanceAndTrack_ReturnsDistanceWithLowercaseM(string input, string expected)
    {
        Assert.Equal(expected, TrackParamsFormatter.FormatForRaceTitle(input));
    }

    [Theory]
    [InlineData("500M", "500M")]
    [InlineData("1000M", "1000M")]
    [InlineData("5000", "5000")]
    [InlineData("1500 111M with # and ; special chars", "1500 111M with # and ; special chars")]
    [InlineData("1000 200M", "1000 200M")] // unknown track size — leave alone
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    public void FormatForRaceTitle_WithUnrecognizedFormat_ReturnsUnchanged(string input, string expected)
    {
        Assert.Equal(expected, TrackParamsFormatter.FormatForRaceTitle(input));
    }
}

public class RaceDataConverterTests
{
    [Fact]
    public void ConvertGcpvRacesToRaces_WithDistanceAndTrack_UsesDisplayDistanceInTitle()
    {
        var gcpvRace = new GcpvRaceData(
            "1C",
            "1000 111M",
            "Women A",
            "Heat (2 + 4)",
            [new GcpvRacerData("1", "263 SKATER, ALEX", "Milton")]);

        var races = new RaceDataConverter().ConvertGcpvRacesToRaces([gcpvRace]).ToList();

        Assert.Single(races);
        Assert.Equal("Women A 1000m Heat (2 + 4)", races[0].RaceTitle);
        Assert.Equal(9.0m, races[0].NumberOfLaps); // 1000 / 111 ≈ 9.0
    }

    [Fact]
    public void ConvertGcpvRacesToRaces_WithPlainDistance_LeavesTitleParamsUnchanged()
    {
        var gcpvRace = new GcpvRaceData(
            "21A",
            "500M",
            "Open Women A",
            "Final",
            [new GcpvRacerData("1", "123 SKATER, JANE", "Toronto")]);

        var races = new RaceDataConverter().ConvertGcpvRacesToRaces([gcpvRace]).ToList();

        Assert.Single(races);
        Assert.Equal("Open Women A 500M Final", races[0].RaceTitle);
        Assert.Equal(5.0m, races[0].NumberOfLaps); // defaults to 100m track
    }
}
