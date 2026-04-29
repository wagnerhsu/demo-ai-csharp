using System.Collections.ObjectModel;
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

    public MainViewModel(
        IRemoteSessionService remoteSessionService,
        IFileTransferService fileTransferService,
        IScriptExecutionService scriptExecutionService,
        IHostProfileService hostProfileService,
        ICredentialStore credentialStore)
    {
        _remoteSessionService = remoteSessionService;
        _fileTransferService = fileTransferService;
        _scriptExecutionService = scriptExecutionService;
        _hostProfileService = hostProfileService;
        _credentialStore = credentialStore;

        AuthenticationTypes = Enum.GetValues<AuthenticationType>();
        ScriptInputTypes = Enum.GetValues<ScriptInputType>();

        _ = LoadProfilesAsync();
    }

    public ObservableCollection<HostProfile> Profiles { get; } = [];

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
    private string localUploadPath = string.Empty;

    [ObservableProperty]
    private string remoteUploadPath = string.Empty;

    [ObservableProperty]
    private string remoteDownloadPath = string.Empty;

    [ObservableProperty]
    private string localDownloadPath = string.Empty;

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

        if (string.IsNullOrWhiteSpace(LocalUploadPath) || string.IsNullOrWhiteSpace(RemoteUploadPath))
        {
            AppendLog("上传失败: 本地路径和远程路径不能为空。");
            return;
        }

        try
        {
            IsBusy = true;
            TransferProgress = 0;

            var request = new TransferRequest
            {
                Direction = TransferDirection.Upload,
                LocalPath = LocalUploadPath,
                RemotePath = RemoteUploadPath
            };

            var progress = new Progress<TransferProgress>(value => TransferProgress = value.Percentage);
            var result = await _fileTransferService.UploadAsync(request, progress, CancellationToken.None);

            AppendLog(result.Succeeded
                ? $"上传成功: {LocalUploadPath} -> {RemoteUploadPath}"
                : $"上传失败: {result.ErrorMessage}");
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

        if (string.IsNullOrWhiteSpace(RemoteDownloadPath) || string.IsNullOrWhiteSpace(LocalDownloadPath))
        {
            AppendLog("下载失败: 远程路径和本地路径不能为空。");
            return;
        }

        try
        {
            IsBusy = true;
            TransferProgress = 0;

            var request = new TransferRequest
            {
                Direction = TransferDirection.Download,
                LocalPath = LocalDownloadPath,
                RemotePath = RemoteDownloadPath
            };

            var progress = new Progress<TransferProgress>(value => TransferProgress = value.Percentage);
            var result = await _fileTransferService.DownloadAsync(request, progress, CancellationToken.None);

            AppendLog(result.Succeeded
                ? $"下载成功: {RemoteDownloadPath} -> {LocalDownloadPath}"
                : $"下载失败: {result.ErrorMessage}");
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
