using System.Windows;
using RemoteLinuxManager.App.ViewModels;

namespace RemoteLinuxManager.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
