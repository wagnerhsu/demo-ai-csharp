using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;
using Renci.SshNet;

namespace RemoteLinuxManager.Infrastructure.SshNet.Services;

public sealed class SshNetRemoteSessionService : IRemoteSessionService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SshClient? _sshClient;
    private SftpClient? _sftpClient;
    private int _isDisposed;

    public bool IsConnected => _sshClient?.IsConnected == true && _sftpClient?.IsConnected == true;

    public HostProfile? CurrentHost { get; private set; }

    public async Task ConnectAsync(HostProfile profile, ConnectionSecret secret, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await DisconnectCoreAsync();

            var connectionInfo = CreateConnectionInfo(profile, secret);
            var sshClient = new SshClient(connectionInfo);
            var sftpClient = new SftpClient(connectionInfo);

            try
            {
                await Task.Run(() =>
                {
                    sshClient.Connect();
                    sftpClient.Connect();
                }, cancellationToken);
            }
            catch
            {
                if (sshClient.IsConnected)
                {
                    sshClient.Disconnect();
                }

                if (sftpClient.IsConnected)
                {
                    sftpClient.Disconnect();
                }

                sshClient.Dispose();
                sftpClient.Dispose();
                throw;
            }

            _sshClient = sshClient;
            _sftpClient = sftpClient;
            CurrentHost = profile;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await DisconnectCoreAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    internal SshClient GetRequiredSshClient()
    {
        if (_sshClient is null || !_sshClient.IsConnected)
        {
            throw new InvalidOperationException("No active SSH connection.");
        }

        return _sshClient;
    }

    internal SftpClient GetRequiredSftpClient()
    {
        if (_sftpClient is null || !_sftpClient.IsConnected)
        {
            throw new InvalidOperationException("No active SFTP connection.");
        }

        return _sftpClient;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        _gate.Wait();
        try
        {
            DisconnectCoreAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private Task DisconnectCoreAsync()
    {
        if (_sftpClient is not null)
        {
            if (_sftpClient.IsConnected)
            {
                _sftpClient.Disconnect();
            }

            _sftpClient.Dispose();
            _sftpClient = null;
        }

        if (_sshClient is not null)
        {
            if (_sshClient.IsConnected)
            {
                _sshClient.Disconnect();
            }

            _sshClient.Dispose();
            _sshClient = null;
        }

        CurrentHost = null;
        return Task.CompletedTask;
    }

    private static ConnectionInfo CreateConnectionInfo(HostProfile profile, ConnectionSecret secret)
    {
        AuthenticationMethod authenticationMethod = profile.AuthenticationType switch
        {
            AuthenticationType.Password => CreatePasswordAuth(profile, secret),
            AuthenticationType.PrivateKey => CreatePrivateKeyAuth(profile, secret),
            _ => throw new NotSupportedException($"Unsupported auth type: {profile.AuthenticationType}.")
        };

        return new ConnectionInfo(profile.Host, profile.Port, profile.Username, authenticationMethod)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    private static AuthenticationMethod CreatePasswordAuth(HostProfile profile, ConnectionSecret secret)
    {
        if (string.IsNullOrWhiteSpace(secret.Password))
        {
            throw new InvalidOperationException($"Password is required for profile '{profile.Name}'.");
        }

        return new PasswordAuthenticationMethod(profile.Username, secret.Password);
    }

    private static AuthenticationMethod CreatePrivateKeyAuth(HostProfile profile, ConnectionSecret secret)
    {
        if (string.IsNullOrWhiteSpace(profile.PrivateKeyPath))
        {
            throw new InvalidOperationException($"Private key path is required for profile '{profile.Name}'.");
        }

        if (!File.Exists(profile.PrivateKeyPath))
        {
            throw new FileNotFoundException("Private key file not found.", profile.PrivateKeyPath);
        }

        PrivateKeyFile privateKey = string.IsNullOrEmpty(secret.PrivateKeyPassphrase)
            ? new PrivateKeyFile(profile.PrivateKeyPath)
            : new PrivateKeyFile(profile.PrivateKeyPath, secret.PrivateKeyPassphrase);

        return new PrivateKeyAuthenticationMethod(profile.Username, privateKey);
    }
}
