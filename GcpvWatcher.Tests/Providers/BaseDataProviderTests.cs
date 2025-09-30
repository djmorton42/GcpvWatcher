using GcpvWatcher.App.Providers;

namespace GcpvWatcher.Tests.Providers;

public class BaseDataProviderTests
{
    private class TestDataProvider : BaseDataProvider
    {
        private readonly IEnumerable<string> _data;

        public TestDataProvider(IEnumerable<string> data)
        {
            _data = data;
        }

        public override Task<IEnumerable<string>> GetDataRowsAsync()
        {
            return Task.FromResult(FilterCommentLines(_data));
        }
    }

    [Fact]
    public async Task FilterCommentLines_WithCommentLines_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            ";This is a comment line",
            "101,Johnson,Jane,Montreal",
            "#Another comment line",
            "102,Brown,Bob,Kingston",
            "; Another comment with spaces",
            "103,Davis,Alice,Hamilton",
            "# Another comment with spaces",
            "104,Wilson,David,Ottawa"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(5, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston", lines[2]);
        Assert.Equal("103,Davis,Alice,Hamilton", lines[3]);
        Assert.Equal("104,Wilson,David,Ottawa", lines[4]);
    }

    [Fact]
    public async Task FilterCommentLines_WithCommentLinesWithLeadingWhitespace_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            " ;Comment with leading space",
            "101,Johnson,Jane,Montreal",
            "  #Comment with leading spaces",
            "102,Brown,Bob,Kingston",
            "\t;Comment with leading tab",
            "103,Davis,Alice,Hamilton",
            "\t\t#Comment with leading tabs",
            "104,Wilson,David,Ottawa"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(5, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston", lines[2]);
        Assert.Equal("103,Davis,Alice,Hamilton", lines[3]);
        Assert.Equal("104,Wilson,David,Ottawa", lines[4]);
    }

    [Fact]
    public async Task FilterCommentLines_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "",
            "101,Johnson,Jane,Montreal",
            "   ",
            "102,Brown,Bob,Kingston",
            "\t",
            "103,Davis,Alice,Hamilton"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(4, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston", lines[2]);
        Assert.Equal("103,Davis,Alice,Hamilton", lines[3]);
    }

    [Fact]
    public async Task FilterCommentLines_WithOnlyCommentLines_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new[]
        {
            ";This is a comment line",
            "#Another comment line",
            "; Another comment with spaces",
            "# Another comment with spaces",
            "  ;Comment with leading spaces",
            "\t#Comment with leading tab"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task FilterCommentLines_WithSemicolonInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto;ON",
            "101,Johnson,Jane,Montreal;QC",
            ";This is a comment line",
            "102,Brown,Bob,Kingston;ON",
            "#Another comment line"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("100,Smith,John,Toronto;ON", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal;QC", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston;ON", lines[2]);
    }

    [Fact]
    public async Task FilterCommentLines_WithHashInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto#123",
            "101,Johnson,Jane,Montreal#456",
            ";This is a comment line",
            "102,Brown,Bob,Kingston#789",
            "#Another comment line"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("100,Smith,John,Toronto#123", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal#456", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston#789", lines[2]);
    }

    [Fact]
    public async Task FilterCommentLines_WithMixedCommentAndEmptyLines_FiltersOutAll()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "",
            ";This is a comment line",
            "   ",
            "101,Johnson,Jane,Montreal",
            "\t",
            "#Another comment line",
            "102,Brown,Bob,Kingston",
            "  ;Comment with leading spaces",
            "  #Comment with leading spaces",
            "103,Davis,Alice,Hamilton"
        };
        var provider = new TestDataProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(4, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston", lines[2]);
        Assert.Equal("103,Davis,Alice,Hamilton", lines[3]);
    }
}
