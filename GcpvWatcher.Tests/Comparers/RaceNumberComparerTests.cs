using GcpvWatcher.App.Comparers;
using GcpvWatcher.App.Models;
using Xunit;

namespace GcpvWatcher.Tests.Comparers;

public class RaceNumberComparerTests
{
    private readonly RaceNumberComparer _comparer = new();

    [Fact]
    public void Compare_WithSameRaceNumbers_ReturnsZero()
    {
        // Arrange
        var race1 = new Race("21A", "Race 1", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("21A", "Race 2", 3.0m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race1, race2);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_WithDifferentNumbers_SortsByNumber()
    {
        // Arrange
        var race1 = new Race("100A", "Race 1", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("3A", "Race 2", 3.0m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race1, race2);

        // Assert
        Assert.True(result > 0); // 100A should come after 3A
    }

    [Fact]
    public void Compare_WithSameNumberDifferentLetters_SortsByLetter()
    {
        // Arrange
        var race1 = new Race("21B", "Race 1", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("21A", "Race 2", 3.0m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race1, race2);

        // Assert
        Assert.True(result > 0); // 21B should come after 21A
    }

    [Fact]
    public void Compare_WithComplexNumbers_SortsCorrectly()
    {
        // Arrange
        var races = new[]
        {
            new Race("100A", "Race 100A", 1.0m, new Dictionary<int, int>()),
            new Race("3B", "Race 3B", 2.0m, new Dictionary<int, int>()),
            new Race("3A", "Race 3A", 3.0m, new Dictionary<int, int>()),
            new Race("22A", "Race 22A", 4.0m, new Dictionary<int, int>()),
            new Race("10A", "Race 10A", 5.0m, new Dictionary<int, int>())
        };

        // Act
        var sortedRaces = races.OrderBy(r => r, _comparer).ToList();

        // Assert
        Assert.Equal("3A", sortedRaces[0].RaceNumber);
        Assert.Equal("3B", sortedRaces[1].RaceNumber);
        Assert.Equal("10A", sortedRaces[2].RaceNumber);
        Assert.Equal("22A", sortedRaces[3].RaceNumber);
        Assert.Equal("100A", sortedRaces[4].RaceNumber);
    }

    [Fact]
    public void Compare_WithNullFirst_ReturnsNegative()
    {
        // Arrange
        var race = new Race("21A", "Race", 4.5m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(null, race);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void Compare_WithNullSecond_ReturnsPositive()
    {
        // Arrange
        var race = new Race("21A", "Race", 4.5m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race, null);

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void Compare_WithBothNull_ReturnsZero()
    {
        // Act
        var result = _comparer.Compare(null, null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Compare_WithInvalidRaceNumber_ThrowsArgumentException()
    {
        // Arrange
        var race1 = new Race("Invalid", "Race 1", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("21A", "Race 2", 3.0m, new Dictionary<int, int>());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _comparer.Compare(race1, race2));
    }

    [Fact]
    public void Compare_WithSingleDigitNumbers_SortsCorrectly()
    {
        // Arrange
        var race1 = new Race("9A", "Race 9A", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("10A", "Race 10A", 3.0m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race1, race2);

        // Assert
        Assert.True(result < 0); // 9A should come before 10A
    }

    [Fact]
    public void Compare_WithLargeNumbers_SortsCorrectly()
    {
        // Arrange
        var race1 = new Race("999Z", "Race 999Z", 4.5m, new Dictionary<int, int>());
        var race2 = new Race("1000A", "Race 1000A", 3.0m, new Dictionary<int, int>());

        // Act
        var result = _comparer.Compare(race1, race2);

        // Assert
        Assert.True(result < 0); // 999Z should come before 1000A
    }
}
