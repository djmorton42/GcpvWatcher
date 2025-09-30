using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GcpvWatcher.App.Services;

namespace GcpvWatcher.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly CalculatorService _calculatorService;
    private string _result = "0";

    public MainWindowViewModel()
    {
        _calculatorService = new CalculatorService();
        CalculateCommand = new RelayCommand(Calculate);
        CloseCommand = new RelayCommand(Close);
    }

    public string Result
    {
        get => _result;
        set
        {
            _result = value;
            OnPropertyChanged();
        }
    }

    public ICommand CalculateCommand { get; }
    public ICommand CloseCommand { get; }

    private void Calculate()
    {
        var result = _calculatorService.Add(4, 2);
        Result = result.ToString();
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
}
