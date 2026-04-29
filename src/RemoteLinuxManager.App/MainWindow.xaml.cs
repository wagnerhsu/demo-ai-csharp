using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RemoteLinuxManager.App.ViewModels;

namespace RemoteLinuxManager.App;

public partial class MainWindow : Window
{
    private readonly List<int> _searchMatches = [];
    private int _currentMatchIndex = -1;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    // ── 连接配置密码框 ─────────────────────────────────────────

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

    // ── 文件树选择 ────────────────────────────────────────────

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

    // ── 日志右键菜单 ──────────────────────────────────────────

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ClearLogsCommand.Execute(null);
    }

    private void ScrollToEnd_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.ScrollToEnd();
    }

    private void ShowSearch_Click(object sender, RoutedEventArgs e)
    {
        OpenSearch();
    }

    // ── 日志文本框键盘事件 ────────────────────────────────────

    private void LogTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenSearch();
            e.Handled = true;
        }
    }

    private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(LogTextBox.Text))
            ResetSearch();
        else if (_searchMatches.Count > 0)
            RefreshSearchMatches();
    }

    // ── 搜索栏操作 ────────────────────────────────────────────

    private void SearchInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshSearchMatches();
    }

    private void SearchInputBox_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    NavigateMatch(forward: false);
                else
                    NavigateMatch(forward: true);
                e.Handled = true;
                break;
            case Key.F3:
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    NavigateMatch(forward: false);
                else
                    NavigateMatch(forward: true);
                e.Handled = true;
                break;
            case Key.Escape:
                CloseSearch();
                e.Handled = true;
                break;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => NavigateMatch(forward: true);

    private void FindPrevious_Click(object sender, RoutedEventArgs e) => NavigateMatch(forward: false);

    private void CloseSearch_Click(object sender, RoutedEventArgs e) => CloseSearch();

    // ── 搜索核心逻辑 ─────────────────────────────────────────

    private void OpenSearch()
    {
        SearchBar.Visibility = Visibility.Visible;
        SearchInputBox.Focus();
        SearchInputBox.SelectAll();
    }

    private void CloseSearch()
    {
        SearchBar.Visibility = Visibility.Collapsed;
        ResetSearch();
        LogTextBox.Focus();
    }

    private void ResetSearch()
    {
        _searchMatches.Clear();
        _currentMatchIndex = -1;
        SearchResultLabel.Text = string.Empty;
    }

    private void RefreshSearchMatches()
    {
        _searchMatches.Clear();
        _currentMatchIndex = -1;

        var keyword = SearchInputBox.Text;
        if (string.IsNullOrEmpty(keyword))
        {
            SearchResultLabel.Text = string.Empty;
            return;
        }

        var text = LogTextBox.Text;
        var index = 0;
        while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            _searchMatches.Add(index);
            index += keyword.Length;
        }

        SearchResultLabel.Text = _searchMatches.Count == 0 ? "未找到" : $"共 {_searchMatches.Count} 处";
    }

    private void NavigateMatch(bool forward)
    {
        if (_searchMatches.Count == 0)
        {
            RefreshSearchMatches();
            if (_searchMatches.Count == 0) return;
        }

        if (forward)
            _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
        else
            _currentMatchIndex = (_currentMatchIndex - 1 + _searchMatches.Count) % _searchMatches.Count;

        var pos = _searchMatches[_currentMatchIndex];
        var keyword = SearchInputBox.Text;

        LogTextBox.Select(pos, keyword.Length);
        LogTextBox.ScrollToLine(LogTextBox.GetLineIndexFromCharacterIndex(pos));
        LogTextBox.Focus();

        SearchResultLabel.Text = $"{_currentMatchIndex + 1}/{_searchMatches.Count}";
    }
}


