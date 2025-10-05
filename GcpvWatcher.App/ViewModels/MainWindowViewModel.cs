using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using GcpvWatcher.App.Services;
using GcpvWatcher.App.Models;
using System.IO;
using System.Collections.ObjectModel;

namespace GcpvWatcher.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private string _watchDirectory = "";
    private string _finishLynxDirectory = "";
    private string _status = "Not Watching";
    private bool _isWatching = false;
    private Window? _window;
    private readonly FileOperationsService _fileOperationsService;
    private readonly AppConfigService _appConfigService;
    private FileWatcherService? _fileWatcherService;
    private AppConfig? _appConfig;
    private UserPreferences _userPreferences = new();
    private readonly ObservableCollection<Race> _races = new();
    private string _logContent = "";

    public MainWindowViewModel()
    {
        _fileOperationsService = new FileOperationsService();
        _appConfigService = new AppConfigService();
        BrowseWatchDirectoryCommand = new RelayCommand(BrowseWatchDirectory, () => CanBrowseWatchDirectory);
        BrowseFinishLynxDirectoryCommand = new RelayCommand(BrowseFinishLynxDirectory, () => CanBrowseFinishLynxDirectory);
        StartWatchingCommand = new RelayCommand(StartWatching, () => CanStartWatching);
        StopWatchingCommand = new RelayCommand(StopWatching, () => CanStopWatching);
        CloseCommand = new RelayCommand(Close);
        
        // Subscribe to logger events
        WatcherLogger.LogMessage += OnLogMessage;
        
        // Load user preferences
        LoadUserPreferences();
        
        // Load configuration
        LoadConfiguration();
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }

    public string WatchDirectory
    {
        get => _watchDirectory;
        set
        {
            if (_watchDirectory != value)
            {
                _watchDirectory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartWatching));
                OnPropertyChanged(nameof(CanStopWatching));
                ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
                
                // Save user preferences
                SaveUserPreferences();
                
                // If we're currently watching, restart watching with the new directory
                if (_isWatching)
                {
                    RestartWatching();
                }
            }
        }
    }

    public string FinishLynxDirectory
    {
        get => _finishLynxDirectory;
        set
        {
            if (_finishLynxDirectory != value)
            {
                _finishLynxDirectory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartWatching));
                OnPropertyChanged(nameof(CanStopWatching));
                ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
                
                // Save user preferences
                SaveUserPreferences();
            }
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    public string StatusColor => _isWatching ? "Green" : "Red";

    public bool CanStartWatching => !_isWatching && !string.IsNullOrEmpty(_watchDirectory) && !string.IsNullOrEmpty(_finishLynxDirectory);

    public bool CanStopWatching => _isWatching;

    public bool CanBrowseWatchDirectory => !_isWatching;

    public bool CanBrowseFinishLynxDirectory => !_isWatching;

    public ObservableCollection<Race> Races => _races;

    public Dictionary<int, Racer> Racers => _fileWatcherService?.Racers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<int, Racer>();

    public string LogContent
    {
        get => _logContent;
        set
        {
            if (_logContent != value)
            {
                _logContent = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand BrowseWatchDirectoryCommand { get; }
    public ICommand BrowseFinishLynxDirectoryCommand { get; }
    public ICommand StartWatchingCommand { get; }
    public ICommand StopWatchingCommand { get; }
    public ICommand CloseCommand { get; }

    private async void BrowseWatchDirectory()
    {
        if (_window == null) return;

        var options = new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select Watch Directory"
        };

        var result = await _window.StorageProvider.OpenFolderPickerAsync(options);
        if (result.Count > 0)
        {
            WatchDirectory = result[0].Path.LocalPath;
        }
    }

    private async void BrowseFinishLynxDirectory()
    {
        if (_window == null) return;

        var options = new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select FinishLynx Event Directory"
        };

        var result = await _window.StorageProvider.OpenFolderPickerAsync(options);
        if (result.Count > 0)
        {
            var selectedDirectory = result[0].Path.LocalPath;
            
            // Check if Lynx.evt file exists, create it if it doesn't
            if (!_fileOperationsService.LynxEvtFileExists(selectedDirectory))
            {
                try
                {
                    var createdFilePath = _fileOperationsService.CreateLynxEvtFile(selectedDirectory);
                    WatcherLogger.Log("Lynx.evt file not found. Created.");
                }
                catch (Exception ex)
                {
                    ApplicationLogger.LogException($"Error creating Lynx.evt file", ex);
                    return; // Don't set the directory if file creation failed
                }
            }
            else
            {
                WatcherLogger.Log("Lynx.evt file found.");
            }
            
            FinishLynxDirectory = selectedDirectory;
        }
    }

    private async void StartWatching()
    {
        try
        {
            if (_appConfig == null)
            {
                ApplicationLogger.Log("Configuration not loaded. Cannot start watching.");
                return;
            }

            _fileWatcherService = new FileWatcherService(_appConfig, _watchDirectory, _finishLynxDirectory);
            _fileWatcherService.FileProcessed += OnFileProcessed;
            _fileWatcherService.ErrorOccurred += OnErrorOccurred;
            _fileWatcherService.RacesUpdated += OnRacesUpdated;
            _fileWatcherService.RacersUpdated += OnRacersUpdated;

            await _fileWatcherService.StartWatchingAsync();
            
            _isWatching = true;
            Status = "Watching";
            OnPropertyChanged(nameof(CanStartWatching));
            OnPropertyChanged(nameof(CanStopWatching));
            OnPropertyChanged(nameof(CanBrowseWatchDirectory));
            OnPropertyChanged(nameof(CanBrowseFinishLynxDirectory));
            ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)BrowseWatchDirectoryCommand).RaiseCanExecuteChanged();
            ((RelayCommand)BrowseFinishLynxDirectoryCommand).RaiseCanExecuteChanged();
            
            ApplicationLogger.Log("File watching started successfully");
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException($"Error starting file watcher", ex);
        }
    }

    private void StopWatching()
    {
        try
        {
            _fileWatcherService?.StopWatching();
            _fileWatcherService?.Dispose();
            _fileWatcherService = null;

            _isWatching = false;
            Status = "Not Watching";
            OnPropertyChanged(nameof(CanStartWatching));
            OnPropertyChanged(nameof(CanStopWatching));
            OnPropertyChanged(nameof(CanBrowseWatchDirectory));
            OnPropertyChanged(nameof(CanBrowseFinishLynxDirectory));
            ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)BrowseWatchDirectoryCommand).RaiseCanExecuteChanged();
            ((RelayCommand)BrowseFinishLynxDirectoryCommand).RaiseCanExecuteChanged();
            
            WatcherLogger.Log("File watching stopped");
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException($"Error stopping file watcher", ex);
        }
    }

    private void Close()
    {
        StopWatching();
        System.Environment.Exit(0);
    }

    private void LoadUserPreferences()
    {
        try
        {
            _userPreferences = UserPreferences.Load();
            
            // Set directories if they exist and are valid
            if (!string.IsNullOrEmpty(_userPreferences.WatchDirectory) && Directory.Exists(_userPreferences.WatchDirectory))
            {
                _watchDirectory = _userPreferences.WatchDirectory;
                OnPropertyChanged(nameof(WatchDirectory));
            }
            
            if (!string.IsNullOrEmpty(_userPreferences.FinishLynxDirectory) && Directory.Exists(_userPreferences.FinishLynxDirectory))
            {
                _finishLynxDirectory = _userPreferences.FinishLynxDirectory;
                OnPropertyChanged(nameof(FinishLynxDirectory));
            }
            
            ApplicationLogger.Log("User preferences loaded successfully");
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error loading user preferences", ex);
            _userPreferences = new UserPreferences();
        }
    }

    private void SaveUserPreferences()
    {
        try
        {
            if (_userPreferences != null)
            {
                _userPreferences.WatchDirectory = _watchDirectory;
                _userPreferences.FinishLynxDirectory = _finishLynxDirectory;
                _userPreferences.Save();
                ApplicationLogger.Log("User preferences saved successfully");
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException("Error saving user preferences", ex);
        }
    }

    private async void LoadConfiguration()
    {
        try
        {
            _appConfig = await _appConfigService.LoadConfigAsync();
            ApplicationLogger.Log("Configuration loaded successfully");
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException($"Error loading configuration", ex);
        }
    }

    private void OnFileProcessed(object? sender, string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        ApplicationLogger.Log($"Processed file: {fileName}");
    }

    private void OnErrorOccurred(object? sender, string errorMessage)
    {
        WatcherLogger.Log(errorMessage);
    }

    private void OnRacesUpdated(object? sender, EventArgs e)
    {
        if (_fileWatcherService != null)
        {
            var allRaces = _fileWatcherService.GetAllRaces().ToList();
            
            // Update racer data in static service
            var racers = _fileWatcherService.Racers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            RacerDataService.UpdateRacers(racers);
            
            // Update the races collection on the UI thread
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _races.Clear();
                foreach (var race in allRaces)
                {
                    _races.Add(race);
                }
            });
        }
    }

    private void OnRacersUpdated(object? sender, EventArgs e)
    {
        // Update the static racer data service
        if (_fileWatcherService != null)
        {
            var racers = _fileWatcherService.Racers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            RacerDataService.UpdateRacers(racers);
        }
        
        // Notify that the Racers property has changed
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(Racers));
        });
    }

    private void OnLogMessage(object? sender, string message)
    {
        // Update log content on the UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LogContent += message + Environment.NewLine;
        });
    }

    private async void RestartWatching()
    {
        try
        {
            ApplicationLogger.Log("Watch directory changed, restarting file watcher...");
            
            // Stop current watching
            _fileWatcherService?.StopWatching();
            _fileWatcherService?.Dispose();
            _fileWatcherService = null;

            // Start watching with new directory
            if (_appConfig != null)
            {
                _fileWatcherService = new FileWatcherService(_appConfig, _watchDirectory, _finishLynxDirectory);
                _fileWatcherService.FileProcessed += OnFileProcessed;
                _fileWatcherService.ErrorOccurred += OnErrorOccurred;
                _fileWatcherService.RacesUpdated += OnRacesUpdated;
                _fileWatcherService.RacersUpdated += OnRacersUpdated;
                await _fileWatcherService.StartWatchingAsync();
                
                ApplicationLogger.Log($"Now watching directory: {_watchDirectory}");
            }
        }
        catch (Exception ex)
        {
            ApplicationLogger.LogException($"Error restarting file watcher", ex);
        }
    }

    public void Dispose()
    {
        // Unsubscribe from logger events
        WatcherLogger.LogMessage -= OnLogMessage;
        
        // Stop watching and dispose file watcher
        StopWatching();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
