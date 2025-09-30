using Avalonia.Controls;
using GcpvWatcher.App.ViewModels;

namespace GcpvWatcher.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        DataContext = viewModel;
        viewModel.SetWindow(this);
    }
}

