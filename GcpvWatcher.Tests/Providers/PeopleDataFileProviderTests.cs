using GcpvWatcher.App.Providers;
using System.IO;

namespace GcpvWatcher.Tests.Providers;

public class PeopleDataFileProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempPplFile;
    private readonly string _tempTxtFile;

    public PeopleDataFileProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
        
        _tempPplFile = Path.Combine(_tempDirectory, "test.ppl");
        _tempTxtFile = Path.Combine(_tempDirectory, "test.txt");
    }

    [Fact]
    public void Constructor_WithValidPplFile_DoesNotThrow()
    {
        // Arrange
        File.WriteAllText(_tempPplFile, "100,Smith,John,Toronto");

        // Act & Assert
        var exception = Record.Exception(() => new PeopleDataFileProvider(_tempPplFile));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithExistingFile_DoesNotThrow()
    {
        // Arrange
        File.WriteAllText(_tempPplFile, "100,Smith,John,Toronto");

        // Act & Assert
        var exception = Record.Exception(() => new PeopleDataFileProvider(_tempPplFile));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeopleDataFileProvider(null!));
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeopleDataFileProvider(""));
    }

    [Fact]
    public void Constructor_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeopleDataFileProvider("   "));
    }

    [Fact]
    public void Constructor_WithNonPplExtension_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PeopleDataFileProvider("test.txt"));
    }

    [Fact]
    public async Task GetDataRowsAsync_WithValidFile_ReturnsAllLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "101,Johnson,Jane,Montreal",
            "102,Brown,Bob,Kingston"
        };
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "",
            "101,Johnson,Jane,Montreal",
            "   ",
            "102,Brown,Bob,Kingston"
        };
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public void Constructor_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.ppl");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => new PeopleDataFileProvider(nonExistentFile));
    }

    [Fact]
    public void Constructor_WithNonExistentFileWithWrongExtension_ThrowsArgumentException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        // Act & Assert
        // Should throw ArgumentException for wrong extension, not FileNotFoundException
        // because extension is checked before file existence
        Assert.Throws<ArgumentException>(() => new PeopleDataFileProvider(nonExistentFile));
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyFile_ReturnsEmptyEnumerable()
    {
        // Arrange
        File.Create(_tempPplFile).Dispose();
        var provider = new PeopleDataFileProvider(_tempPplFile);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Empty(lines);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithWhitespaceInFile_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "100,Smith,John,Toronto",
            "   ",
            "",
            "\t",
            "101,Johnson,Jane,Montreal",
            "  \t  ",
            "102,Brown,Bob,Kingston"
        };
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        await File.WriteAllLinesAsync(_tempPplFile, new[] { "100,Smith,John,Toronto" });
        var provider = new PeopleDataFileProvider(_tempPplFile);
        
        // Delete the file after construction
        File.Delete(_tempPplFile);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetDataRowsAsync());
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileMovedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        var movedFile = Path.Combine(_tempDirectory, "moved.ppl");
        await File.WriteAllLinesAsync(_tempPplFile, new[] { "100,Smith,John,Toronto" });
        var provider = new PeopleDataFileProvider(_tempPplFile);
        
        // Move the file after construction
        File.Move(_tempPplFile, movedFile);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetDataRowsAsync());
        
        // Cleanup
        if (File.Exists(movedFile))
            File.Delete(movedFile);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileStillExists_ReturnsData()
    {
        // Arrange
        var testData = new[] { "100,Smith,John,Toronto", "101,Johnson,Jane,Montreal" };
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(2, lines.Count);
        Assert.Equal("100,Smith,John,Toronto", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal", lines[1]);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileDeletedAfterConstruction_ThrowsFileNotFoundExceptionWithCorrectMessage()
    {
        // Arrange
        await File.WriteAllLinesAsync(_tempPplFile, new[] { "100,Smith,John,Toronto" });
        var provider = new PeopleDataFileProvider(_tempPplFile);
        
        // Delete the file after construction
        File.Delete(_tempPplFile);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetDataRowsAsync());
        Assert.Contains(_tempPplFile, exception.Message);
        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLines_FiltersOutCommentLines()
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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithCommentLinesWithLeadingWhitespace_FiltersOutCommentLines()
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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithOnlyCommentLines_ReturnsEmptyEnumerable()
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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithSemicolonInData_DoesNotFilterOutDataLines()
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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

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
    public async Task GetDataRowsAsync_WithHashInData_DoesNotFilterOutDataLines()
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
        await File.WriteAllLinesAsync(_tempPplFile, testData);
        var provider = new PeopleDataFileProvider(_tempPplFile);

        // Act
        var result = await provider.GetDataRowsAsync();
        var lines = result.ToList();

        // Assert
        Assert.Equal(3, lines.Count);
        Assert.Equal("100,Smith,John,Toronto#123", lines[0]);
        Assert.Equal("101,Johnson,Jane,Montreal#456", lines[1]);
        Assert.Equal("102,Brown,Bob,Kingston#789", lines[2]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
