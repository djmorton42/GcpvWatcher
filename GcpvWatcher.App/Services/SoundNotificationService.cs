using System.Diagnostics;

namespace GcpvWatcher.App.Services;

public class SoundNotificationService : IDisposable
{
    private readonly string _notificationSoundPath;
    private readonly object _lockObject = new object();
    private DateTime _lastPlayTime = DateTime.MinValue;
    private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(5);
    private bool _disposed = false;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public SoundNotificationService(string notificationSoundPath)
    {
        _notificationSoundPath = notificationSoundPath ?? throw new ArgumentNullException(nameof(notificationSoundPath));
    }

    public void PlayNotificationSound()
    {
        if (_disposed)
            return;

        lock (_lockObject)
        {
            var now = DateTime.Now;
            
            // Check if enough time has passed since the last sound
            if (now - _lastPlayTime < _debounceInterval)
            {
                return;
            }

            // Check if the sound file exists
            if (!File.Exists(_notificationSoundPath))
            {
                return;
            }

            try
            {
                // Play the sound asynchronously to avoid blocking
                Task.Run(() =>
                {
                    try
                    {
                        if (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            PlayAudioFile(_notificationSoundPath);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when service is being disposed
                    }
                    catch (Exception)
                    {
                        // Silently fail - sound notification is not critical
                    }
                }, _cancellationTokenSource.Token);

                _lastPlayTime = now;
            }
            catch (Exception)
            {
                // Silently fail - sound notification is not critical
            }
        }
    }

    private void PlayAudioFile(string filePath)
    {
        try
        {
            // Use platform-specific audio players
            if (OperatingSystem.IsWindows())
            {
                // On Windows, use the built-in Windows Media Player
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start /min \"\" \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                // On macOS, use the afplay command
                Process.Start(new ProcessStartInfo
                {
                    FileName = "afplay",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            else if (OperatingSystem.IsLinux())
            {
                // On Linux, try common audio players
                var audioPlayers = new[] { "paplay", "aplay", "mpg123", "mpg321", "play" };
                foreach (var player in audioPlayers)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = player,
                            Arguments = $"\"{filePath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                        return; // Success, exit the loop
                    }
                    catch
                    {
                        // Try next player
                        continue;
                    }
                }
                throw new InvalidOperationException("No suitable audio player found on Linux");
            }
            else
            {
                throw new PlatformNotSupportedException("Audio playback not supported on this platform");
            }
        }
        catch (Exception)
        {
            // Silently fail - sound notification is not critical
            throw;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}
