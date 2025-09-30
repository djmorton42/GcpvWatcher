using GcpvWatcher.App.Providers;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Providers;

public class GcpvExportDataFileProviderTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "test.csv");

    [Fact]
    public void Constructor_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "nonexistent.csv";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => new GcpvExportDataFileProvider(nonExistentFile));
        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public void Constructor_WithExistingFile_DoesNotThrow()
    {
        // Arrange
        CreateTestFile();

        try
        {
            // Act & Assert
            var provider = new GcpvExportDataFileProvider(_testFilePath);
            Assert.NotNull(provider);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public void Constructor_WithWrongExtension_ThrowsArgumentException()
    {
        // Arrange
        var wrongExtensionFile = "test.txt";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GcpvExportDataFileProvider(wrongExtensionFile));
        Assert.Contains("must have a .csv extension", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GcpvExportDataFileProvider(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GcpvExportDataFileProvider(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GcpvExportDataFileProvider("   "));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        CreateTestFile();
        var provider = new GcpvExportDataFileProvider(_testFilePath);
        File.Delete(_testFilePath);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetDataRowsAsync());
        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileStillExists_ReturnsData()
    {
        // Arrange
        var testData = new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Single(lines);
            Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[0]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithEmptyLines_FiltersOutEmptyLines()
    {
        // Arrange
        var testData = new[]
        {
            "",
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"",
            "   ",
            "\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\""
        };
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(2, lines.Count);
            Assert.Equal("\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\"", lines[0]);
            Assert.Equal("\"Event :\",\"500M\",\"Open Women A\",\"Stage :\",\"Final\",,,\"Race\",\"21A\",,,\"Lane\",\"Skaters\",\"Club\",2,\"123 SMITH, JANE\",\"Toronto\",\"28-Sep-25   9:30:00 AM\"", lines[1]);
        }
        finally
        {
            CleanupTestFile();
        }
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
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Empty(lines);
        }
        finally
        {
            CleanupTestFile();
        }
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
        CreateTestFile(testData);
        var provider = new GcpvExportDataFileProvider(_testFilePath);

        try
        {
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
        finally
        {
            CleanupTestFile();
        }
    }

    private void CreateTestFile(string[]? content = null)
    {
        var testContent = content ?? new[]
        {
            "\"Event :\",\"1500 111M\",\"Open Men B  male\",\"Stage :\",\"Heat, 2 +2\",,,\"Race\",\"25A\",,,\"Lane\",\"Skaters\",\"Club\",1,\"689 PORTER, REGGIE\",\"Hamilton\",\"28-Sep-25   9:35:42 AM\""
        };
        
        File.WriteAllLines(_testFilePath, testContent);
    }

    private void CleanupTestFile()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
