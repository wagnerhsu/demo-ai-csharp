using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RemoteLinuxManager.Application.Security;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRemoteSessionService _remoteSessionService;
    private readonly IFileTransferService _fileTransferService;
    private readonly IScriptExecutionService _scriptExecutionService;
    private readonly IHostProfileService _hostProfileService;
    private readonly ICredentialStore _credentialStore;
    private readonly IRemoteFileBrowserService _remoteFileBrowser;

    public MainViewModel(
        IRemoteSessionService remoteSessionService,
        IFileTransferService fileTransferService,
        IScriptExecutionService scriptExecutionService,
        IHostProfileService hostProfileService,
        ICredentialStore credentialStore,
        IRemoteFileBrowserService remoteFileBrowser)
    {
        _remoteSessionService = remoteSessionService;
        _fileTransferService = fileTransferService;
        _scriptExecutionService = scriptExecutionService;
        _hostProfileService = hostProfileService;
        _credentialStore = credentialStore;
        _remoteFileBrowser = remoteFileBrowser;

        AuthenticationTypes = Enum.GetValues<AuthenticationType>();
        ScriptInputTypes = Enum.GetValues<ScriptInputType>();

        InitLocalTree();
        _ = LoadProfilesAsync();
    }

    public ObservableCollection<HostProfile> Profiles { get; } = [];
    public ObservableCollection<FileTreeNode> LocalRoots { get; } = [];
    public ObservableCollection<FileTreeNode> RemoteRoots { get; } = [];

    public IReadOnlyList<AuthenticationType> AuthenticationTypes { get; }
    public IReadOnlyList<ScriptInputType> ScriptInputTypes { get; }

    [ObservableProperty]
    private HostProfile? selectedProfile;

    [ObservableProperty]
    private string profileName = "default";

    [ObservableProperty]
    private string host = string.Empty;

    [ObservableProperty]
    private string port = "22";

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private AuthenticationType authenticationType = AuthenticationType.Password;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string privateKeyPath = string.Empty;

    [ObservableProperty]
    private string privateKeyPassphrase = string.Empty;

    [ObservableProperty]
    private FileTreeNode? selectedLocalNode;

    [ObservableProperty]
    private FileTreeNode? selectedRemoteNode;

    [ObservableProperty]
    private ScriptInputType scriptInputType = ScriptInputType.InlineText;

    [ObservableProperty]
    private string scriptFilePath = string.Empty;

    [ObservableProperty]
    private string inlineScript = "echo hello";

    [ObservableProperty]
    private string sudoPassword = string.Empty;

    [ObservableProperty]
    private string remoteWorkingDirectory = ".";

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private double transferProgress;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string logs = string.Empty;

    partial void OnSelectedProfileChanged(HostProfile? value)
    {
        if (value is null)
        {
            return;
        }

        ProfileName = value.Name;
        Host = value.Host;
        Port = value.Port.ToString();
        Username = value.Username;
        AuthenticationType = value.AuthenticationType;
        PrivateKeyPath = value.PrivateKeyPath ?? string.Empty;
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        var profiles = await _hostProfileService.GetAllAsync(CancellationToken.None);

        Profiles.Clear();
        foreach (var profile in profiles)
        {
            Profiles.Add(profile);
        }

        AppendLog($"已加载 {profiles.Count} 个主机配置。");
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (!TryBuildHostProfile(out var profile, out var error))
        {
            AppendLog(error);
            return;
        }

        await _hostProfileService.SaveAsync(profile, CancellationToken.None);

        if (profile.AuthenticationType == AuthenticationType.Password && !string.IsNullOrWhiteSpace(Password))
        {
            await _credentialStore.SaveAsync(BuildPasswordSecretId(profile.Name), Password, CancellationToken.None);
        }

        if (profile.AuthenticationType == AuthenticationType.PrivateKey && !string.IsNullOrWhiteSpace(PrivateKeyPassphrase))
        {
            await _credentialStore.SaveAsync(BuildPassphraseSecretId(profile.Name), PrivateKeyPassphrase, CancellationToken.None);
        }

        await LoadProfilesAsync();
        AppendLog($"配置已保存: {profile.Name}");
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (!TryBuildHostProfile(out var profile, out var error))
        {
            AppendLog(error);
            return;
        }

        try
        {
            IsBusy = true;
            var secret = await BuildConnectionSecretAsync(profile, CancellationToken.None);
            await _remoteSessionService.ConnectAsync(profile, secret, CancellationToken.None);
            IsConnected = _remoteSessionService.IsConnected;
            AppendLog($"连接成功: {profile.Username}@{profile.Host}:{profile.Port}");
            await InitRemoteTreeAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            IsConnected = false;
            AppendLog($"连接失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        try
        {
            IsBusy = true;
            await _remoteSessionService.DisconnectAsync(CancellationToken.None);
            IsConnected = false;
            RemoteRoots.Clear();
            SelectedRemoteNode = null;
            AppendLog("连接已断开。");
        }
        catch (Exception ex)
        {
            AppendLog($"断开失败: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UploadAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        if (SelectedLocalNode is null || SelectedLocalNode.IsDirectory)
        {
            AppendLog("上传失败: 请在左侧选择一个本地文件。");
            return;
        }

        if (SelectedRemoteNode is null)
        {
            AppendLog("上传失败: 请在右侧选择目标远程目录或文件。");
            return;
        }

        var remotePath = SelectedRemoteNode.IsDirectory
            ? SelectedRemoteNode.FullPath.TrimEnd('/') + "/" + Path.GetFileName(SelectedLocalNode.FullPath)
            : SelectedRemoteNode.FullPath;

        try
        {
            IsBusy = true;
            TransferProgress = 0;

            var request = new TransferRequest
            {
                Direction = TransferDirection.Upload,
                LocalPath = SelectedLocalNode.FullPath,
                RemotePath = remotePath
            };

            var progress = new Progress<TransferProgress>(value => TransferProgress = value.Percentage);
            var result = await _fileTransferService.UploadAsync(request, progress, CancellationToken.None);

            AppendLog(result.Succeeded
                ? $"上传成功: {SelectedLocalNode.FullPath} -> {remotePath}"
                : $"上传失败: {result.ErrorMessage}");

            if (result.Succeeded)
            {
                await RefreshRemoteNodeAsync(SelectedRemoteNode, CancellationToken.None);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        if (SelectedRemoteNode is null || SelectedRemoteNode.IsDirectory)
        {
            AppendLog("下载失败: 请在右侧选择一个远程文件。");
            return;
        }

        if (SelectedLocalNode is null)
        {
            AppendLog("下载失败: 请在左侧选择本地目标目录。");
            return;
        }

        var localDir = SelectedLocalNode.IsDirectory
            ? SelectedLocalNode.FullPath
            : Path.GetDirectoryName(SelectedLocalNode.FullPath) ?? string.Empty;

        var localPath = Path.Combine(localDir, SelectedRemoteNode.Name);

        try
        {
            IsBusy = true;
            TransferProgress = 0;

            var request = new TransferRequest
            {
                Direction = TransferDirection.Download,
                LocalPath = localPath,
                RemotePath = SelectedRemoteNode.FullPath
            };

            var progress = new Progress<TransferProgress>(value => TransferProgress = value.Percentage);
            var result = await _fileTransferService.DownloadAsync(request, progress, CancellationToken.None);

            AppendLog(result.Succeeded
                ? $"下载成功: {SelectedRemoteNode.FullPath} -> {localPath}"
                : $"下载失败: {result.ErrorMessage}");

            if (result.Succeeded)
            {
                var parentNode = SelectedLocalNode.IsDirectory ? SelectedLocalNode : null;
                if (parentNode != null)
                {
                    await RefreshLocalNodeAsync(parentNode);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExecuteScriptAsync()
    {
        if (!EnsureConnected())
        {
            return;
        }

        if (ScriptInputType == ScriptInputType.LocalFile && string.IsNullOrWhiteSpace(ScriptFilePath))
        {
            AppendLog("执行失败: 本地脚本路径不能为空。");
            return;
        }

        if (ScriptInputType == ScriptInputType.InlineText && string.IsNullOrWhiteSpace(InlineScript))
        {
            AppendLog("执行失败: 内联脚本内容不能为空。");
            return;
        }

        try
        {
            IsBusy = true;

            var request = new ScriptExecutionRequest
            {
                InputType = ScriptInputType,
                Content = ScriptInputType == ScriptInputType.LocalFile ? ScriptFilePath : InlineScript,
                RemoteWorkingDirectory = string.IsNullOrWhiteSpace(RemoteWorkingDirectory) ? "." : RemoteWorkingDirectory,
                Timeout = TimeSpan.FromMinutes(3),
                SudoPassword = string.IsNullOrWhiteSpace(SudoPassword) ? null : SudoPassword
            };

            var result = await _scriptExecutionService.ExecuteAsync(request, CancellationToken.None);

            AppendLog($"脚本执行完成: ExitCode={result.ExitCode}, 耗时={result.Elapsed.TotalMilliseconds:F0}ms");
            if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                AppendLog($"STDOUT:\n{result.StandardOutput.TrimEnd()}\n");
            }

            if (!string.IsNullOrWhiteSpace(result.StandardError))
            {
                AppendLog($"STDERR:\n{result.StandardError.TrimEnd()}\n");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void InitLocalTree()
    {
        LocalRoots.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            LocalRoots.Add(CreateLocalNode(drive.RootDirectory.FullName, drive.Name));
        }
    }

    private async Task InitRemoteTreeAsync(CancellationToken cancellationToken)
    {
        RemoteRoots.Clear();
        var rootNode = CreateRemoteNode("/", "/", cancellationToken);
        RemoteRoots.Add(rootNode);
        rootNode.IsExpanded = true;
        await Task.CompletedTask;
    }

    private FileTreeNode CreateLocalNode(string path, string? displayName = null)
    {
        var name = displayName ?? Path.GetFileName(path);
        if (string.IsNullOrEmpty(name)) name = path;

        return new FileTreeNode(name, path, isDirectory: true, onExpand: async node =>
        {
            try
            {
                var children = await Task.Run(() =>
                {
                    var dirs = Directory.GetDirectories(path)
                        .Select(d => CreateLocalNode(d))
                        .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase);

                    var files = Directory.GetFiles(path)
                        .Select(f => new FileTreeNode(Path.GetFileName(f), f, isDirectory: false, onExpand: null))
                        .OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase);

                    return dirs.Concat(files).ToList();
                });

                node.Children.Clear();
                foreach (var child in children)
                    node.Children.Add(child);
            }
            catch
            {
                node.Children.Clear();
            }
        });
    }

    private FileTreeNode CreateRemoteNode(string path, string? displayName, CancellationToken cancellationToken)
    {
        var name = displayName ?? (path == "/" ? "/" : Path.GetFileName(path.TrimEnd('/')));
        if (string.IsNullOrEmpty(name)) name = path;

        return new FileTreeNode(name, path, isDirectory: true, onExpand: async node =>
        {
            try
            {
                var entries = await _remoteFileBrowser.ListDirectoryAsync(path, cancellationToken);

                node.Children.Clear();
                foreach (var entry in entries)
                {
                    if (entry.IsDirectory)
                        node.Children.Add(CreateRemoteNode(entry.FullPath, entry.Name, cancellationToken));
                    else
                        node.Children.Add(new FileTreeNode(entry.Name, entry.FullPath, isDirectory: false, onExpand: null));
                }
            }
            catch (Exception ex)
            {
                node.Children.Clear();
                AppendLog($"远程目录加载失败 [{path}]: {ex.Message}");
            }
        });
    }

    private async Task RefreshRemoteNodeAsync(FileTreeNode node, CancellationToken cancellationToken)
    {
        var targetNode = node.IsDirectory ? node : null;
        if (targetNode is null) return;

        targetNode.Children.Clear();
        targetNode.Children.Add(FileTreeNode.LoadingPlaceholder);
        targetNode.IsExpanded = false;
        targetNode.IsExpanded = true;
        await Task.CompletedTask;
    }

    private async Task RefreshLocalNodeAsync(FileTreeNode node)
    {
        node.Children.Clear();
        node.Children.Add(FileTreeNode.LoadingPlaceholder);
        node.IsExpanded = false;
        node.IsExpanded = true;
        await Task.CompletedTask;
    }

    private async Task<ConnectionSecret> BuildConnectionSecretAsync(HostProfile profile, CancellationToken cancellationToken)
    {
        if (profile.AuthenticationType == AuthenticationType.Password)
        {
            var pwd = Password;
            if (string.IsNullOrWhiteSpace(pwd) && !string.IsNullOrWhiteSpace(profile.PasswordSecretId))
            {
                pwd = await _credentialStore.GetAsync(profile.PasswordSecretId, cancellationToken) ?? string.Empty;
            }

            return new ConnectionSecret
            {
                Password = pwd
            };
        }

        var passphrase = PrivateKeyPassphrase;
        if (string.IsNullOrWhiteSpace(passphrase) && !string.IsNullOrWhiteSpace(profile.PrivateKeyPassphraseSecretId))
        {
            passphrase = await _credentialStore.GetAsync(profile.PrivateKeyPassphraseSecretId, cancellationToken) ?? string.Empty;
        }

        return new ConnectionSecret
        {
            PrivateKeyPassphrase = passphrase
        };
    }

    private bool TryBuildHostProfile(out HostProfile profile, out string error)
    {
        profile = null!;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            error = "配置名称不能为空。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Host))
        {
            error = "主机不能为空。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Username))
        {
            error = "用户名不能为空。";
            return false;
        }

        if (!int.TryParse(Port, out var parsedPort) || parsedPort <= 0)
        {
            error = "端口格式不正确。";
            return false;
        }

        if (AuthenticationType == AuthenticationType.PrivateKey && string.IsNullOrWhiteSpace(PrivateKeyPath))
        {
            error = "私钥认证模式下必须填写私钥路径。";
            return false;
        }

        profile = new HostProfile
        {
            Name = ProfileName.Trim(),
            Host = Host.Trim(),
            Port = parsedPort,
            Username = Username.Trim(),
            AuthenticationType = AuthenticationType,
            PrivateKeyPath = string.IsNullOrWhiteSpace(PrivateKeyPath) ? null : PrivateKeyPath.Trim(),
            PasswordSecretId = AuthenticationType == AuthenticationType.Password ? BuildPasswordSecretId(ProfileName.Trim()) : null,
            PrivateKeyPassphraseSecretId = AuthenticationType == AuthenticationType.PrivateKey ? BuildPassphraseSecretId(ProfileName.Trim()) : null
        };

        return true;
    }

    private bool EnsureConnected()
    {
        IsConnected = _remoteSessionService.IsConnected;

        if (IsConnected)
        {
            return true;
        }

        AppendLog("当前未连接远程主机。");
        return false;
    }

    private void AppendLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        Logs = string.IsNullOrWhiteSpace(Logs)
            ? line
            : $"{Logs}{Environment.NewLine}{line}";
    }

    private static string BuildPasswordSecretId(string profileName)
    {
        return $"{profileName}:password";
    }

    private static string BuildPassphraseSecretId(string profileName)
    {
        return $"{profileName}:passphrase";
    }
}
