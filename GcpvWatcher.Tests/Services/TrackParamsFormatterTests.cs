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
    [InlineData("600 111M", "600m")]
    [InlineData("500A 111M", "500m")]
    [InlineData("500 No Pts 111M", "500m No Pts")]
    [InlineData("400 No Pts 100M", "400m No Pts")]
    [InlineData("500A No Pts 111M", "500m No Pts")]
    [InlineData("  500  No Pts  111m  ", "500m No Pts")]
    [InlineData("500a 111m", "500m")]
    [InlineData("1000B 100M", "1000m")]
    [InlineData("600C 111M", "600m")]
    [InlineData("  500A  111M  ", "500m")]
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
    [InlineData("500A", "500A")] // series letter without a track size — leave alone
    [InlineData("500AB 111M", "500AB 111M")] // only a single series letter is recognized
    [InlineData("500A 200M", "500A 200M")] // unknown track size — leave alone
    [InlineData("500 A 111M", "500m A")] // spaced letter is a qualifier, not a series
    [InlineData("500m 111M", "500m 111M")] // "m" is meters, not a series letter
    [InlineData("1000M 111M", "1000M 111M")]
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

    [Theory]
    [InlineData("38A", "500 No Pts 111M", "All Skaters", "Final", "All Skaters 500m No Pts Final", 4.5)]
    [InlineData("12A", "400 No Pts 100M", "Age 13 Mixed Gender", "Final", "Age 13 Mixed Gender 400m No Pts Final", 4.0)]
    [InlineData("21A", "500A No Pts 111M", "All Skaters", "Final", "All Skaters 500m No Pts Final A", 4.5)]
    [InlineData("2A", "50 No Pts 100M", "All Skaters", "Final", "All Skaters 50m No Pts Final", 0.5)]
    [InlineData("2A", "500A 111M", "All Skaters,", "Final", "All Skaters, 500m Final A", 4.5)]
    [InlineData("3B", "1000B 111M", "Open Men", "Final", "Open Men 1000m Final B", 9.0)]
    [InlineData("4C", "1500C 100M", "Women", "Semi-Final", "Women 1500m Semi-Final C", 15.0)]
    [InlineData("5A", "600A 111M", "All Skaters,", "Final", "All Skaters, 600m Final A", 6.0)]
    [InlineData("7A", "400 111M", "Age 13", "Final", "Age 13 400m Final", 4.0)]
    [InlineData("6A", "777a 111m", "Mixed", "Final", "Mixed 777m Final a", 7.0)]
    [InlineData("2A", "500B 111M", "All Skaters,", "Final", "All Skaters, 500m Final B", 4.5)]
    [InlineData("31C", "500 111M", "Women A", "Final", "Women A 500m Final", 4.5)]
    [InlineData("2A", "500AB 111M", "All Skaters,", "Final", "All Skaters, 500AB 111M Final", 4.5)]
    [InlineData("2A", "500A 200M", "All Skaters,", "Final", "All Skaters, 500A 200M Final", 5.0)]
    public void ConvertGcpvRacesToRaces_WithOptionalSeriesLetter_BuildsExpectedTitle(
        string raceNumber,
        string trackParams,
        string raceGroup,
        string stage,
        string expectedTitle,
        double expectedLaps)
    {
        var gcpvRace = new GcpvRaceData(
            raceNumber,
            trackParams,
            raceGroup,
            stage,
            [new GcpvRacerData("1", "746 WU, ALWYN", "Richmond Hill")]);

        var races = new RaceDataConverter().ConvertGcpvRacesToRaces([gcpvRace]).ToList();

        Assert.Single(races);
        Assert.Equal(expectedTitle, races[0].RaceTitle);
        Assert.Equal((decimal)expectedLaps, races[0].NumberOfLaps);
        Assert.Equal(raceNumber, races[0].RaceNumber);
    }
}
