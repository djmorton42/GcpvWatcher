using System.Diagnostics;
using NAudio.Wave;

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
                    catch (Exception ex)
                    {
                        // Log error but don't fail - sound notification is not critical
                        Console.WriteLine($"Sound notification failed: {ex.Message}");
                    }
                }, _cancellationTokenSource.Token);

                _lastPlayTime = now;
            }
            catch (Exception ex)
            {
                // Log error but don't fail - sound notification is not critical
                Console.WriteLine($"Sound notification failed: {ex.Message}");
            }
        }
    }

    private void PlayAudioFile(string filePath)
    {
        try
        {
            // Use platform-specific audio playback
            if (OperatingSystem.IsWindows())
            {
                PlayAudioFileWithNAudio(filePath);
            }
            else if (OperatingSystem.IsMacOS())
            {
                PlayAudioFileWithAfplay(filePath);
            }
            else if (OperatingSystem.IsLinux())
            {
                PlayAudioFileWithLinuxPlayer(filePath);
            }
            else
            {
                throw new PlatformNotSupportedException("Audio playback not supported on this platform");
            }
        }
        catch (Exception ex)
        {
            // Log error but don't fail - sound notification is not critical
            Console.WriteLine($"Sound notification failed: {ex.Message}");
            throw;
        }
    }

    private void PlayAudioFileWithNAudio(string filePath)
    {
        using var audioFile = new AudioFileReader(filePath);
        using var outputDevice = new WaveOutEvent();
        
        // Set up event handler for when playback stops
        var playbackComplete = new ManualResetEventSlim(false);
        outputDevice.PlaybackStopped += (sender, e) => playbackComplete.Set();
        
        outputDevice.Init(audioFile);
        outputDevice.Play();
        
        // Wait for playback to complete without polling
        playbackComplete.Wait();
    }

    private void PlayAudioFileWithAfplay(string filePath)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "afplay",
            Arguments = $"\"{filePath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        });
        
        if (process != null)
        {
            process.WaitForExit();
        }
        else
        {
            throw new InvalidOperationException("Failed to start afplay process");
        }
    }

    private void PlayAudioFileWithLinuxPlayer(string filePath)
    {
        // Try common audio players on Linux
        var audioPlayers = new[] { "paplay", "aplay", "mpg123", "mpg321", "play" };
        foreach (var player in audioPlayers)
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = player,
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                if (process != null)
                {
                    process.WaitForExit();
                    return; // Success, exit the loop
                }
            }
            catch
            {
                // Try next player
                continue;
            }
        }
        throw new InvalidOperationException("No suitable audio player found on Linux");
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
