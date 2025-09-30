using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using Avalonia.Controls;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private string _watchDirectory = "";
    private string _finishLynxDirectory = "";
    private string _status = "Not Watching";
    private bool _isWatching = false;
    private Window? _window;

    public MainWindowViewModel()
    {
        BrowseWatchDirectoryCommand = new RelayCommand(BrowseWatchDirectory);
        BrowseFinishLynxDirectoryCommand = new RelayCommand(BrowseFinishLynxDirectory);
        StartWatchingCommand = new RelayCommand(StartWatching, () => CanStartWatching);
        StopWatchingCommand = new RelayCommand(StopWatching, () => CanStopWatching);
        CloseCommand = new RelayCommand(Close);
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
            _watchDirectory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStartWatching));
            OnPropertyChanged(nameof(CanStopWatching));
            ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
        }
    }

    public string FinishLynxDirectory
    {
        get => _finishLynxDirectory;
        set
        {
            _finishLynxDirectory = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStartWatching));
            OnPropertyChanged(nameof(CanStopWatching));
            ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
            ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
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
            FinishLynxDirectory = result[0].Path.LocalPath;
        }
    }

    private void StartWatching()
    {
        _isWatching = true;
        Status = "Watching";
        OnPropertyChanged(nameof(CanStartWatching));
        OnPropertyChanged(nameof(CanStopWatching));
        ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
        ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
    }

    private void StopWatching()
    {
        _isWatching = false;
        Status = "Not Watching";
        OnPropertyChanged(nameof(CanStartWatching));
        OnPropertyChanged(nameof(CanStopWatching));
        ((RelayCommand)StartWatchingCommand).RaiseCanExecuteChanged();
        ((RelayCommand)StopWatchingCommand).RaiseCanExecuteChanged();
    }

    private void Close()
    {
        System.Environment.Exit(0);
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
