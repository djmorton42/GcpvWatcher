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

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Only reload if viewModel is initialized and the RawEvtTab is selected
        // This check prevents null reference exceptions during initialization
        if (_viewModel != null && MainTabControl != null && RawEvtTab != null && MainTabControl.SelectedItem == RawEvtTab)
        {
            // Reload the raw EVT content when the tab is selected
            _viewModel.ReloadRawEvtContent();
        }
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

