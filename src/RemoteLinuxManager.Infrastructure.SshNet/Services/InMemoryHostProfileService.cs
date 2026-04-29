using System.Collections.Concurrent;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Infrastructure.SshNet.Services;

public sealed class InMemoryHostProfileService : IHostProfileService
{
    private readonly ConcurrentDictionary<string, HostProfile> _profiles = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<HostProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<HostProfile> profiles = _profiles.Values
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(profiles);
    }

    public Task SaveAsync(HostProfile profile, CancellationToken cancellationToken)
    {
        _profiles[profile.Name] = profile;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string profileName, CancellationToken cancellationToken)
    {
        _profiles.TryRemove(profileName, out _);
        return Task.CompletedTask;
    }
}
