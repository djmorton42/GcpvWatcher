using GcpvWatcher.App.Providers;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace GcpvWatcher.Tests.Providers;

public class EventDataFileProviderTests
{
    private readonly string _testFilePath = Path.Combine(Path.GetTempPath(), "test.evt");

    [Fact]
    public void Constructor_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = "nonexistent.evt";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => new EventDataFileProvider(nonExistentFile));
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
            var provider = new EventDataFileProvider(_testFilePath);
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
        var exception = Assert.Throws<ArgumentException>(() => new EventDataFileProvider(wrongExtensionFile));
        Assert.Contains("must have a .evt extension", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventDataFileProvider(null!));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventDataFileProvider(""));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Constructor_WithWhitespaceFilePath_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new EventDataFileProvider("   "));
        Assert.Contains("cannot be null or empty", exception.Message);
    }

    [Fact]
    public async Task GetDataRowsAsync_WithFileDeletedAfterConstruction_ThrowsFileNotFoundException()
    {
        // Arrange
        CreateTestFile();
        var provider = new EventDataFileProvider(_testFilePath);
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
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(3, lines.Count);
            Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
            Assert.Equal(",2010,2", lines[2]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLines_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            "; This is a comment",
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ",1051,1",
            "# Another comment",
            "21B,,,\"Race Title 2\",,,,,,,,,3.0",
            ",563,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(4, lines.Count);
            Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
            Assert.Equal("21B,,,\"Race Title 2\",,,,,,,,,3.0", lines[2]);
            Assert.Equal(",563,1", lines[3]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithCommentLinesWithLeadingWhitespace_FiltersOutCommentLines()
    {
        // Arrange
        var testData = new[]
        {
            " ; Comment with leading space",
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            "   # Comment with leading spaces",
            ",1051,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(2, lines.Count);
            Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithOnlyCommentLines_ReturnsEmptyEnumerable()
    {
        // Arrange
        var testData = new[]
        {
            ";Comment 1",
            "#Comment 2",
            "   ;Comment 3",
            "\t#Comment 4"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

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
    public async Task GetDataRowsAsync_WithMixedCommentAndEmptyLines_FiltersOutAll()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race Title\",,,,,,,,,4.5",
            ";Comment",
            "",
            "   ",
            "#Another comment",
            ",1051,1"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(2, lines.Count);
            Assert.Equal("21A,,,\"Race Title\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithSemicolonInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race; with; semicolons\",,,,,,,,,4.5",
            ",1051,1",
            ";This is a comment",
            "21B,,,\"Race Title 2\",,,,,,,,,3.0"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(3, lines.Count);
            Assert.Equal("21A,,,\"Race; with; semicolons\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
            Assert.Equal("21B,,,\"Race Title 2\",,,,,,,,,3.0", lines[2]);
        }
        finally
        {
            CleanupTestFile();
        }
    }

    [Fact]
    public async Task GetDataRowsAsync_WithHashInData_DoesNotFilterOutDataLines()
    {
        // Arrange
        var testData = new[]
        {
            "21A,,,\"Race# with# hashes\",,,,,,,,,4.5",
            ",1051,1",
            "#This is a comment",
            "21B,,,\"Race Title 2\",,,,,,,,,3.0"
        };
        CreateTestFile(testData);
        var provider = new EventDataFileProvider(_testFilePath);

        try
        {
            // Act
            var result = await provider.GetDataRowsAsync();
            var lines = result.ToList();

            // Assert
            Assert.Equal(3, lines.Count);
            Assert.Equal("21A,,,\"Race# with# hashes\",,,,,,,,,4.5", lines[0]);
            Assert.Equal(",1051,1", lines[1]);
            Assert.Equal("21B,,,\"Race Title 2\",,,,,,,,,3.0", lines[2]);
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
            "21A,,,\"Test Race\",,,,,,,,,4.5",
            ",1051,1",
            ",2010,2"
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
