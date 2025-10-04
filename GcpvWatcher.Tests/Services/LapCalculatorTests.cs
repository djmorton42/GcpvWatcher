using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class LapCalculatorTests
{
    #region CalculateLaps(string) Tests

    [Fact]
    public void CalculateLaps_WithStandardTrack_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "5000";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(50.0, result);
    }

    [Fact]
    public void CalculateLaps_With111MTrack_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "5000 111m";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(45.0, result);
    }

    [Fact]
    public void CalculateLaps_With111MTrackCaseInsensitive_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "3000 111M";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(27.0, result);
    }

    [Theory]
    [InlineData("2000", 20.0)]
    [InlineData("1500", 15.0)]
    [InlineData("1000", 10.0)]
    [InlineData("800", 8.0)]
    [InlineData("500", 5.0)]
    [InlineData("400", 4.0)]
    [InlineData("300", 3.0)]
    [InlineData("200", 2.0)]
    [InlineData("100", 1.0)]
    [InlineData("50", 0.5)]
    public void CalculateLaps_WithStandardTrackVariousDistances_ReturnsCorrectLaps(string raceParams, double expected)
    {
        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("5000 111m", 45.0)]
    [InlineData("3000 111m", 27.0)]
    [InlineData("2000 111m", 18.0)]
    [InlineData("1500 111m", 13.5)]
    [InlineData("1000 111m", 9.0)]
    [InlineData("777 111m", 7.0)]
    [InlineData("500 111m", 4.5)]
    [InlineData("333 111m", 3.0)]
    public void CalculateLaps_With111MTrackVariousDistances_ReturnsCorrectLaps(string raceParams, double expected)
    {
        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateLaps_WithUnknownDistance_ReturnsZero()
    {
        // Arrange
        string raceParams = "9999";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateLaps_WithEmptyString_ReturnsZero()
    {
        // Arrange
        string raceParams = "";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateLaps_WithNullString_ReturnsZero()
    {
        // Arrange
        string? raceParams = null;

        // Act
        double result = LapCalculator.CalculateLaps(raceParams!);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateLaps_WithNoDistanceInString_ReturnsZero()
    {
        // Arrange
        string raceParams = "Some other text without distance";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateLaps_WithMultipleDistances_ReturnsFirstMatch()
    {
        // Arrange
        string raceParams = "5000 3000 1000";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(50.0, result); // Should match 5000 first
    }

    [Fact]
    public void CalculateLaps_WithDistanceInMiddleOfString_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "Race: 1500 Final";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(15.0, result);
    }

    [Fact]
    public void CalculateLaps_WithDistanceAtEndOfString_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "Women's 1000";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(10.0, result);
    }

    [Fact]
    public void CalculateLaps_WithDistanceAtStartOfString_ReturnsCorrectLaps()
    {
        // Arrange
        string raceParams = "2000 Men's Race";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(20.0, result);
    }

    #endregion

    #region CalculateLaps(int, int) Tests

    [Fact]
    public void CalculateLaps_WithStandardTrackLength_ReturnsCorrectLaps()
    {
        // Arrange
        int distance = 5000;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(50.0, result);
    }

    [Fact]
    public void CalculateLaps_With111MTrackLength_ReturnsCorrectLaps()
    {
        // Arrange
        int distance = 5000;
        int trackLength = 111;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(45.0, result);
    }

    [Theory]
    [InlineData(2000, 100, 20.0)]
    [InlineData(1500, 100, 15.0)]
    [InlineData(1000, 100, 10.0)]
    [InlineData(800, 100, 8.0)]
    [InlineData(500, 100, 5.0)]
    [InlineData(400, 100, 4.0)]
    [InlineData(300, 100, 3.0)]
    [InlineData(200, 100, 2.0)]
    [InlineData(100, 100, 1.0)]
    [InlineData(50, 100, 0.5)]
    public void CalculateLaps_WithStandardTrackLengthVariousDistances_ReturnsCorrectLaps(int distance, int trackLength, double expected)
    {
        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(5000, 111, 45.0)]
    [InlineData(3000, 111, 27.0)]
    [InlineData(2000, 111, 18.0)]
    [InlineData(1500, 111, 13.5)]
    [InlineData(1000, 111, 9.0)]
    [InlineData(777, 111, 7.0)]
    [InlineData(500, 111, 4.5)]
    [InlineData(333, 111, 3.0)]
    public void CalculateLaps_With111MTrackLengthVariousDistances_ReturnsCorrectLaps(int distance, int trackLength, double expected)
    {
        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1000, 200, 5.0)]
    [InlineData(1000, 250, 4.0)]
    [InlineData(1000, 333, 3.0)]
    [InlineData(1000, 500, 2.0)]
    [InlineData(1000, 1000, 1.0)]
    public void CalculateLaps_WithVariousTrackLengths_ReturnsCorrectLaps(int distance, int trackLength, double expected)
    {
        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateLaps_WithZeroDistance_ReturnsZero()
    {
        // Arrange
        int distance = 0;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateLaps_WithDistanceLessThanTrackLength_ReturnsFractionalLaps()
    {
        // Arrange
        int distance = 50;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(0.5, result);
    }

    [Fact]
    public void CalculateLaps_WithDistanceEqualToTrackLength_ReturnsOneLap()
    {
        // Arrange
        int distance = 100;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateLaps_WithDistanceGreaterThanTrackLength_ReturnsMultipleLaps()
    {
        // Arrange
        int distance = 250;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(2.5, result);
    }

    [Fact]
    public void CalculateLaps_WithVerySmallDistance_ReturnsVerySmallLaps()
    {
        // Arrange
        int distance = 1;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(0.0, result); // Rounded to 1 decimal place
    }

    [Fact]
    public void CalculateLaps_WithVeryLargeDistance_ReturnsManyLaps()
    {
        // Arrange
        int distance = 10000;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(100.0, result);
    }

    [Fact]
    public void CalculateLaps_WithRounding_ReturnsRoundedResult()
    {
        // Arrange
        int distance = 333;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(3.3, result); // 333/100 = 3.33, rounded to 3.3
    }

    [Fact]
    public void CalculateLaps_WithRoundingUp_ReturnsRoundedResult()
    {
        // Arrange
        int distance = 335;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(3.4, result); // 335/100 = 3.35, rounded to 3.4
    }

    [Fact]
    public void CalculateLaps_WithRoundingDown_ReturnsRoundedResult()
    {
        // Arrange
        int distance = 334;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(3.3, result); // 334/100 = 3.34, rounded to 3.3
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Fact]
    public void CalculateLaps_WithNegativeDistance_ThrowsArgumentException()
    {
        // Arrange
        int distance = -1000;
        int trackLength = 100;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => LapCalculator.CalculateLaps(distance, trackLength));
        Assert.Equal("Distance cannot be negative. (Parameter 'distance')", exception.Message);
    }

    [Fact]
    public void CalculateLaps_WithNegativeTrackLength_ThrowsArgumentException()
    {
        // Arrange
        int distance = 1000;
        int trackLength = -100;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => LapCalculator.CalculateLaps(distance, trackLength));
        Assert.Equal("Track length cannot be negative. (Parameter 'trackLength')", exception.Message);
    }

    [Fact]
    public void CalculateLaps_WithBothNegativeValues_ThrowsArgumentException()
    {
        // Arrange
        int distance = -1000;
        int trackLength = -100;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => LapCalculator.CalculateLaps(distance, trackLength));
        Assert.Equal("Distance cannot be negative. (Parameter 'distance')", exception.Message);
    }

    [Fact]
    public void CalculateLaps_WithZeroTrackLength_ThrowsArgumentException()
    {
        // Arrange
        int distance = 1000;
        int trackLength = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => LapCalculator.CalculateLaps(distance, trackLength));
        Assert.Equal("Track length cannot be zero. (Parameter 'trackLength')", exception.Message);
    }

    [Fact]
    public void CalculateLaps_WithVeryLargeNumbers_ReturnsCorrectLaps()
    {
        // Arrange
        int distance = int.MaxValue;
        int trackLength = 100;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.True(result > 0);
        Assert.Equal(int.MaxValue / 100.0, result, 1);
    }

    [Fact]
    public void CalculateLaps_WithVerySmallTrackLength_ReturnsLargeLaps()
    {
        // Arrange
        int distance = 1000;
        int trackLength = 1;

        // Act
        double result = LapCalculator.CalculateLaps(distance, trackLength);

        // Assert
        Assert.Equal(1000.0, result);
    }

    [Fact]
    public void CalculateLaps_WithWhitespaceString_ReturnsZero()
    {
        // Arrange
        string raceParams = "   ";

        // Act
        double result = LapCalculator.CalculateLaps(raceParams);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion
}
