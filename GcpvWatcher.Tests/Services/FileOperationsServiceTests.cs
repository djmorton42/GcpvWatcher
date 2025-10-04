using GcpvWatcher.App.Services;
using System.IO;

namespace GcpvWatcher.Tests.Services;

public class FileOperationsServiceTests
{
    private readonly FileOperationsService _service;

    public FileOperationsServiceTests()
    {
        _service = new FileOperationsService();
    }

    [Fact]
    public void LynxEvtFileExists_WithNullDirectory_ReturnsFalse()
    {
        // Act
        var result = _service.LynxEvtFileExists(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LynxEvtFileExists_WithEmptyDirectory_ReturnsFalse()
    {
        // Act
        var result = _service.LynxEvtFileExists("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LynxEvtFileExists_WithNonExistentDirectory_ReturnsFalse()
    {
        // Act
        var result = _service.LynxEvtFileExists("C:\\NonExistentDirectory");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LynxEvtFileExists_WithDirectoryWithoutLynxEvt_ReturnsFalse()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _service.LynxEvtFileExists(tempDir);

            // Assert
            Assert.False(result);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LynxEvtFileExists_WithDirectoryContainingLynxEvt_ReturnsTrue()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var lynxEvtPath = Path.Combine(tempDir, "Lynx.evt");
        File.WriteAllText(lynxEvtPath, "test content");

        try
        {
            // Act
            var result = _service.LynxEvtFileExists(tempDir);

            // Assert
            Assert.True(result);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateLynxEvtFile_WithNullDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateLynxEvtFile(null!));
    }

    [Fact]
    public void CreateLynxEvtFile_WithEmptyDirectory_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CreateLynxEvtFile(""));
    }

    [Fact]
    public void CreateLynxEvtFile_WithNonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => _service.CreateLynxEvtFile("C:\\NonExistentDirectory"));
    }

    [Fact]
    public void CreateLynxEvtFile_WithValidDirectory_CreatesFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _service.CreateLynxEvtFile(tempDir);

            // Assert
            Assert.NotNull(result);
            Assert.True(File.Exists(result));
            Assert.Equal("Lynx.evt", Path.GetFileName(result));
            
            // Verify file content is empty
            var content = File.ReadAllText(result);
            Assert.Empty(content);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void CreateLynxEvtFile_WithValidDirectory_ReturnsCorrectPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _service.CreateLynxEvtFile(tempDir);

            // Assert
            var expectedPath = Path.Combine(tempDir, "Lynx.evt");
            Assert.Equal(expectedPath, result);
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }
}
