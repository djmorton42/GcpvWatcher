using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class CalculatorServiceTests
{
    private readonly CalculatorService _calculatorService;

    public CalculatorServiceTests()
    {
        _calculatorService = new CalculatorService();
    }

    [Fact]
    public void Add_WithTwoPositiveNumbers_ReturnsCorrectSum()
    {
        // Arrange
        int a = 4;
        int b = 2;

        // Act
        int result = _calculatorService.Add(a, b);

        // Assert
        Assert.Equal(6, result);
    }

    [Fact]
    public void Add_WithZeroAndPositiveNumber_ReturnsCorrectSum()
    {
        // Arrange
        int a = 0;
        int b = 5;

        // Act
        int result = _calculatorService.Add(a, b);

        // Assert
        Assert.Equal(5, result);
    }

    [Fact]
    public void Add_WithNegativeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        int a = -3;
        int b = -2;

        // Act
        int result = _calculatorService.Add(a, b);

        // Assert
        Assert.Equal(-5, result);
    }

    [Fact]
    public void Add_WithPositiveAndNegativeNumbers_ReturnsCorrectSum()
    {
        // Arrange
        int a = 10;
        int b = -3;

        // Act
        int result = _calculatorService.Add(a, b);

        // Assert
        Assert.Equal(7, result);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 1, 2)]
    [InlineData(100, 200, 300)]
    [InlineData(-10, 10, 0)]
    [InlineData(int.MaxValue, 0, int.MaxValue)]
    public void Add_WithVariousInputs_ReturnsCorrectSum(int a, int b, int expected)
    {
        // Act
        int result = _calculatorService.Add(a, b);

        // Assert
        Assert.Equal(expected, result);
    }
}

