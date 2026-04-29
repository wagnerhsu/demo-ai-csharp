using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RemoteLinuxManager.App.ViewModels;

public partial class FileTreeNode : ObservableObject
{
    public static readonly FileTreeNode LoadingPlaceholder =
        new("加载中...", string.Empty, false, null);

    private readonly Func<FileTreeNode, Task>? _onExpand;

    public FileTreeNode(string name, string fullPath, bool isDirectory, Func<FileTreeNode, Task>? onExpand)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        _onExpand = onExpand;

        if (isDirectory && onExpand != null)
            Children.Add(LoadingPlaceholder);
    }

    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }

    [ObservableProperty]
    private bool isExpanded;

    public ObservableCollection<FileTreeNode> Children { get; } = [];

    partial void OnIsExpandedChanged(bool value)
    {
        if (value && Children.Count == 1 && Children[0] == LoadingPlaceholder)
            _ = _onExpand!.Invoke(this);
    }
}
