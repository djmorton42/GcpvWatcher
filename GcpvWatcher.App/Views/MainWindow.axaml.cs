using Avalonia.Controls;
using GcpvWatcher.App.ViewModels;

namespace GcpvWatcher.App.Views;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel();
        DataContext = _viewModel;
        _viewModel.SetWindow(this);
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (_viewModel != null)
        {
            await _viewModel.DisposeAsync();
        }
        base.OnClosed(e);
    }
}

