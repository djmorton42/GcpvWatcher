using GcpvWatcher.App.Providers;
using Xunit;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Providers;

public class EventDataStaticProviderTests
{
    [Fact]
    public async Task GetDataRowsAsync_WithValidData_ReturnsAllRows()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
        Assert.Equal(",1051,1", lines[1]);
        Assert.Equal(",2010,2", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyStrings_FiltersOutEmptyStrings()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            "",
            ",1051,1",
            "   ",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
        Assert.Equal(",1051,1", lines[1]);
        Assert.Equal(",2010,2", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLines_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            "; This is a comment",
            ",1051,1",
            "# Another comment",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
        Assert.Equal(",1051,1", lines[1]);
        Assert.Equal(",2010,2", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithOnlyCommentsAndEmptyLines_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new[]
        {
            ";Comment 1",
            "#Comment 2",
            "",
            "   ",
            "\t;Comment 3"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public void GetDataRowsAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventDataStaticProvider(null!));
    }

    [Fact]
    public async Task GetDataRowsAsync_WithMixedWhitespaceAndComments_FiltersCorrectly()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            " ; Comment with leading space",
            "   # Comment with leading spaces",
            "",
            ",1051,1",
            "\t; Comment with leading tab",
            "   ",
            ",2010,2"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
        Assert.Equal(",1051,1", lines[1]);
        Assert.Equal(",2010,2", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithSemicolonOrHashInData_DoesNotFilter()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race; with; semicolons\",,,,,,,,,4.5",
            ",1051,1",
            ";This is a comment",
            "21B,,,\"Race# with# hashes\",,,,,,,,,3.0"
        };
        var provider = new EventDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("21A,,,\"Race; with; semicolons\",,,,,,,,,4.5", lines[0]);
        Assert.Equal(",1051,1", lines[1]);
        Assert.Equal("21B,,,\"Race# with# hashes\",,,,,,,,,3.0", lines[2]);
    }
}
