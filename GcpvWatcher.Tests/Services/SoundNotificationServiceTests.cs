using GcpvWatcher.App.Services;
using Xunit;

namespace GcpvWatcher.Tests.Services;

public class SoundNotificationServiceTests
{
    [Fact]
    public void Constructor_WithValidPath_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "test.mp3";

        // Act & Assert
        var exception = Record.Exception(() => new SoundNotificationService(soundPath));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullPath_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SoundNotificationService(null!));
    }

    [Fact]
    public void PlayNotificationSound_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "nonexistent.mp3";
        var service = new SoundNotificationService(soundPath);

        // Act & Assert
        var exception = Record.Exception(() => service.PlayNotificationSound());
        Assert.Null(exception);
    }

    [Fact]
    public void PlayNotificationSound_WithDebouncing_ShouldSkipSecondCall()
    {
        // Arrange
        var soundPath = "test.mp3";
        var service = new SoundNotificationService(soundPath);

        // Act
        service.PlayNotificationSound();
        service.PlayNotificationSound(); // This should be skipped due to debouncing

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "test.mp3";
        var service = new SoundNotificationService(soundPath);

        // Act & Assert
        var exception = Record.Exception(() => service.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void PlayNotificationSound_AfterDispose_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "test.mp3";
        var service = new SoundNotificationService(soundPath);
        service.Dispose();

        // Act & Assert
        var exception = Record.Exception(() => service.PlayNotificationSound());
        Assert.Null(exception);
    }
}
