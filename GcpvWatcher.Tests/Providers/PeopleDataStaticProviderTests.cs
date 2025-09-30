using GcpvWatcher.App.Providers;

namespace GcpvWatcher.Tests.Providers;

public class PeopleDataStaticProviderTests
{
    [Fact]
    public void Constructor_WithValidData_DoesNotThrow()
    {
        // Arrange
        var testData = new[] { "100,Smith,John,Toronto", "101,Johnson,Jane,Montreal" };

        // Act & Assert
        var exception = Record.Exception(() => new PeopleDataStaticProvider(testData));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PeopleDataStaticProvider(null!));
    }

    [Fact]
    public async Task GetDataRowsAsync_WithValidData_ReturnsAllRows()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "101,Johnson,Jane,Montreal",
            "102,Brown,Bob,Kingston"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyData_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = Array.Empty<string>();
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyStrings_FiltersOutEmptyStrings()
    {
        // Arrange
        var testData = new[] { "", "100,Smith,John,Toronto", "   " };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Single(lines);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
    }

    [Fact]
    public async Task GetDataRowsAsync_IsIdempotent_ReturnsSameDataMultipleTimes()
    {
        // Arrange
        var testData = new[] { "100,Smith,John,Toronto", "101,Johnson,Jane,Montreal" };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result1 = await provider.GetDataRowsAsync();
        var result2 = await provider.GetDataRowsAsync();
        var lines1 = result1.ToList();
        var lines2 = result2.ToList();

        // Assert
        Assert.Equal(lines1.Count, lines2.Count);
        for (int i = 0; i < lines1.Count; i++)
        {
            Assert.Equal(lines1[i], lines2[i]);
        }
    }
}
