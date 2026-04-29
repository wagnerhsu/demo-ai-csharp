using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Application.Services;

public interface IRemoteSessionService
{
    bool IsConnected { get; }

    HostProfile? CurrentHost { get; }

    Task ConnectAsync(HostProfile profile, ConnectionSecret secret, CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);
}
