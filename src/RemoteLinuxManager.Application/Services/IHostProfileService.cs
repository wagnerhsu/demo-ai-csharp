using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Application.Services;

public interface IHostProfileService
{
    Task<IReadOnlyList<HostProfile>> GetAllAsync(CancellationToken cancellationToken);

    Task SaveAsync(HostProfile profile, CancellationToken cancellationToken);

    Task DeleteAsync(string profileName, CancellationToken cancellationToken);
}
