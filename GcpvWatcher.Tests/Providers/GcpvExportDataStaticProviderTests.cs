using GcpvWatcher.App.Providers;
using Xunit;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Providers;

public class GcpvExportDataStaticProviderTests
{
    [Fact]
    public async Task GetDataRowsAsync_WithValidData_ReturnsAllRows()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Single(lines);
        Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[0]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyStrings_FiltersOutEmptyStrings()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"",
            "   "
        };
        var provider = new GcpvExportDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[0]);
        Assert.Equal("\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"", lines[1]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLines_DoesNotFilterComments()
    {
        // Arrange
        var testData = new[]
        {
            "; This is a comment",
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "# Another comment",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        var provider = new GcpvExportDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(4, lines.Count);
        Assert.Equal("; This is a comment", lines[0]);
        Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[1]);
        Assert.Equal("# Another comment", lines[2]);
        Assert.Equal("\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"", lines[3]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithOnlyEmptyLines_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "   ",
            "\t",
            "  \t  "
        };
        var provider = new GcpvExportDataStaticProvider(testData);

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
        Assert.Throws<ArgumentNullException>(() => new GcpvExportDataStaticProvider(null!));
    }

    [Fact]
    public async Task GetDataRowsAsync_WithMixedWhitespaceAndComments_DoesNotFilterComments()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            " ; Comment with leading space",
            "   # Comment with leading spaces",
            "",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"",
            "\t; Comment with leading tab",
            "   "
        };
        var provider = new GcpvExportDataStaticProvider(testData);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(5, lines.Count);
        Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[0]);
        Assert.Equal(" ; Comment with leading space", lines[1]);
        Assert.Equal("   # Comment with leading spaces", lines[2]);
        Assert.Equal("\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"", lines[3]);
        Assert.Equal("\t; Comment with leading tab", lines[4]);
    }
}
