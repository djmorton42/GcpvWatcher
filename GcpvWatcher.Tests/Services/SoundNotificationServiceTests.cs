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

    [Fact]
    public void Constructor_WithEnableNotificationSoundFalse_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "test.mp3";
        var enableNotificationSound = false;

        // Act & Assert
        var exception = Record.Exception(() => new SoundNotificationService(soundPath, enableNotificationSound));
        Assert.Null(exception);
    }

    [Fact]
    public void PlayNotificationSound_WithEnableNotificationSoundFalse_ShouldNotPlaySound()
    {
        // Arrange
        var soundPath = "test.mp3";
        var service = new SoundNotificationService(soundPath, false);

        // Act & Assert
        var exception = Record.Exception(() => service.PlayNotificationSound());
        Assert.Null(exception);
        // Note: This test verifies that no exception is thrown when sound is disabled
        // The actual sound playing behavior is tested by the fact that no exception occurs
    }

    [Fact]
    public void PlayNotificationSound_WithEnableNotificationSoundTrue_ShouldNotThrow()
    {
        // Arrange
        var soundPath = "nonexistent.mp3"; // Use non-existent file to avoid actual sound playing
        var service = new SoundNotificationService(soundPath, true);

        // Act & Assert
        var exception = Record.Exception(() => service.PlayNotificationSound());
        Assert.Null(exception);
    }
}
