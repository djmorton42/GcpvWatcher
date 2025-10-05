using GcpvWatcher.App.Providers;
using Xunit;

namespace GcpvWatcher.Tests.Providers;

public class PeopleDataStaticProviderTests
{
    [Fact]
    public void Constructor_WithValidData_DoesNotThrow()
    {
        // Arrange
        var testData = new[] { "116,Lopez,Nancy,St. Lawrence", "315,Taylor,Dorothy,CPV Gatineau" };

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
    public async Task GetDataRowsAsync_WithValidData_ReturnsAllLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "315,Taylor,Dorothy,CPV Gatineau",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyData_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new string[0];
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "",
            "315,Taylor,Dorothy,CPV Gatineau",
            "   ",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLines_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            ";This is a comment line",
            "315,Taylor,Dorothy,CPV Gatineau",
            "#Another comment line",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLinesWithLeadingWhitespace_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            " ;Comment with leading space",
            "315,Taylor,Dorothy,CPV Gatineau",
            "  #Comment with leading spaces",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithOnlyCommentLines_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new[]
        {
            ";This is a comment line",
            "#Another comment line",
            "  ;Comment with leading spaces",
            "\t#Comment with leading tab"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithMixedCommentAndEmptyLines_FiltersOutAll()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence",
            "",
            ";This is a comment line",
            "   ",
            "315,Taylor,Dorothy,CPV Gatineau",
            "\t",
            "#Another comment line",
            "322,Adams,Justin,Milton"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithSemicolonInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence;ON",
            "315,Taylor,Dorothy,CPV Gatineau;QC",
            ";This is a comment line",
            "322,Adams,Justin,Milton;ON"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence;ON", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau;QC", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton;ON", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithHashInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "116,Lopez,Nancy,St. Lawrence#123",
            "315,Taylor,Dorothy,CPV Gatineau#456",
            ";This is a comment line",
            "322,Adams,Justin,Milton#789"
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("116,Lopez,Nancy,St. Lawrence#123", lines[0]);
        Assert.Equal("315,Taylor,Dorothy,CPV Gatineau#456", lines[1]);
        Assert.Equal("322,Adams,Justin,Milton#789", lines[2]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithWhitespaceInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "  116  ,  Lopez  ,  Nancy  ,  St. Lawrence  ",
            "  315  ,  Taylor  ,  Dorothy  ,  CPV Gatineau  "
        };
        var provider = new PeopleDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("  116  ,  Lopez  ,  Nancy  ,  St. Lawrence  ", lines[0]);
        Assert.Equal("  315  ,  Taylor  ,  Dorothy  ,  CPV Gatineau  ", lines[1]);
    }
}