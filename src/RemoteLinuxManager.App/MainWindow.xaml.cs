using System.Windows;
using System.Windows.Controls;
using RemoteLinuxManager.App.ViewModels;

namespace RemoteLinuxManager.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.Password = ((PasswordBox)sender).Password;
    }

    private void PassphraseBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.PrivateKeyPassphrase = ((PasswordBox)sender).Password;
    }

    private void SudoPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.SudoPassword = ((PasswordBox)sender).Password;
    }

    private void LocalTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm)
            vm.SelectedLocalNode = e.NewValue as FileTreeNode;
    }

    private void RemoteTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel vm)
            vm.SelectedRemoteNode = e.NewValue as FileTreeNode;
    }
}

