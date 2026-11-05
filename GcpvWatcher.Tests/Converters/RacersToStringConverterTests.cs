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
        var racers = new Dictionary<string, int>();
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>());

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("No racers", result);
    }

    [Fact]
    public void Convert_WithRacersButNoRacerData_ReturnsRacerIds()
    {
        // Arrange
        var racers = new Dictionary<string, int>
        {
            { "1", 1 },
            { "2", 2 }
        };
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>());

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    1{Environment.NewLine}Lane  2,    2", result);
    }

    [Fact]
    public void Convert_WithRacersAndRacerData_ReturnsRacerNames()
    {
        // Arrange
        var racers = new Dictionary<string, int>
        {
            { "1", 1 },
            { "2", 2 }
        };
        var racerData = new Dictionary<string, Racer>
        {
            { "1", new Racer("1", "Smith", "John", "Team A") },
            { "2", new Racer("2", "Johnson", "Jane", "Team B") }
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
        var racers = new Dictionary<string, int>
        {
            { "1", 1 },
            { "2", 2 },
            { "3", 3 }
        };
        var racerData = new Dictionary<string, Racer>
        {
            { "1", new Racer("1", "Smith", "John", "Team A") },
            { "3", new Racer("3", "Brown", "Bob", "Team C") }
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
        var racers = new Dictionary<string, int>
        {
            { "3", 1 },
            { "1", 3 },
            { "2", 2 }
        };
        var racerData = new Dictionary<string, Racer>
        {
            { "1", new Racer("1", "Smith", "John", "Team A") },
            { "2", new Racer("2", "Johnson", "Jane", "Team B") },
            { "3", new Racer("3", "Brown", "Bob", "Team C") }
        };
        RacerDataService.UpdateRacers(racerData);

        // Act
        var result = _converter.Convert(racers, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal($"Lane  1,    3 - Bob Brown (Team C){Environment.NewLine}Lane  2,    2 - Jane Johnson (Team B){Environment.NewLine}Lane  3,    1 - John Smith (Team A)", result);
    }

    [Fact]
    public void Convert_RelayFirstName_ShowsTeamNameAndMemberNamesInPipeOrder()
    {
        var race = RaceWithKeepName("100", 1, new KeepRacerName("Ontario", "|12|34|789|", "Club"));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "12", new Racer("12", "Smith", "Jane", "Hamilton") },
            { "34", new Racer("34", "Doe", "John", "Toronto") },
            { "789", new Racer("789", "Ng", "Amy", "Ottawa") },
            { "100", new Racer("100", "Brown", "Bob", "Someone Else") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,  100 - Ontario (Club)" + Environment.NewLine +
            "                Jane Smith" + Environment.NewLine +
            "                John Doe" + Environment.NewLine +
            "                Amy Ng",
            result);
        Assert.DoesNotContain("|", result?.ToString());
        Assert.DoesNotContain("Bob Brown", result?.ToString());
    }

    [Fact]
    public void Convert_RelayWithOneMember_ShowsThatMemberUnderTheTeam()
    {
        var race = RaceWithKeepName("100", 1, new KeepRacerName("Wolves", "|42|", ""));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "42", new Racer("42", "Ng", "Amy", "Ottawa") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,  100 - Wolves" + Environment.NewLine +
            "                Amy Ng",
            result);
    }

    [Fact]
    public void Convert_RelayMemberMissingFromPeopleFile_ShowsTheMemberId()
    {
        var race = RaceWithKeepName("100", 1, new KeepRacerName("Ontario", "|12|99|", ""));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "12", new Racer("12", "Smith", "Jane", "Hamilton") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,  100 - Ontario" + Environment.NewLine +
            "                Jane Smith" + Environment.NewLine +
            "                99",
            result);
    }

    [Fact]
    public void Convert_RelayMemberId_MatchesThePeopleFileIdExactly()
    {
        var race = RaceWithKeepName("100", 1, new KeepRacerName("Ontario", "|012|", ""));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "12", new Racer("12", "Smith", "Jane", "Hamilton") },
            { "012", new Racer("012", "Doe", "John", "Toronto") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,  100 - Ontario" + Environment.NewLine +
            "                John Doe",
            result);
    }

    [Fact]
    public void Convert_FirstNameThatIsNotARelayList_DoesNotAddSubtext()
    {
        var race = RaceWithKeepName("7", 1, new KeepRacerName("Ontario", "Jane", "Club"));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "7", new Racer("7", "Smith", "John", "Team A") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Lane  1,    7 - John Smith (Team A)", result);
    }

    [Fact]
    public void Convert_RelayMemberId_AllowsUpToFiveDigits()
    {
        var race = RaceWithKeepName("7", 1, new KeepRacerName("Ontario", "|12345|", ""));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "7", new Racer("7", "Smith", "John", "Team A") },
            { "12345", new Racer("12345", "Doe", "John", "Toronto") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,    7 - Ontario" + Environment.NewLine +
            "                John Doe",
            result);
    }

    [Fact]
    public void Convert_FirstNameWithANumberLongerThanFiveDigits_IsNotARelay()
    {
        var race = RaceWithKeepName("7", 1, new KeepRacerName("Ontario", "|123456|", ""));
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "7", new Racer("7", "Smith", "John", "Team A") },
            { "123456", new Racer("123456", "Doe", "John", "Toronto") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Lane  1,    7 - John Smith (Team A)", result);
    }

    [Fact]
    public void Convert_RelayAndIndividualInTheSameRace_AddsSubtextOnlyUnderTheTeam()
    {
        var race = new Race("21A", "Mixed", 4m, new Dictionary<string, int>
        {
            { "1", 1 },
            { "100", 2 }
        })
        {
            RacerNames = new Dictionary<string, KeepRacerName>
            {
                { "100", new KeepRacerName("Ontario", "|12|", "") }
            }
        };
        RacerDataService.UpdateRacers(new Dictionary<string, Racer>
        {
            { "1", new Racer("1", "Smith", "John", "Team A") },
            { "12", new Racer("12", "Doe", "Jane", "Hamilton") }
        });

        var result = _converter.Convert(race, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(
            "Lane  1,    1 - John Smith (Team A)" + Environment.NewLine +
            "Lane  2,  100 - Ontario" + Environment.NewLine +
            "                Jane Doe",
            result);
    }

    private static Race RaceWithKeepName(string racerId, int lane, KeepRacerName name)
    {
        return new Race("21A", "Relay", 4m, new Dictionary<string, int> { { racerId, lane } })
        {
            RacerNames = new Dictionary<string, KeepRacerName> { { racerId, name } }
        };
    }
}
