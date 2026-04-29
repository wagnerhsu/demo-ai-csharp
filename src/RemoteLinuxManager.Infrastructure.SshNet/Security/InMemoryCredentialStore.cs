using System.Collections.Concurrent;
using RemoteLinuxManager.Application.Security;

namespace RemoteLinuxManager.Infrastructure.SshNet.Security;

public sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(string secretId, string value, CancellationToken cancellationToken)
    {
        _secrets[secretId] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string secretId, CancellationToken cancellationToken)
    {
        _secrets.TryGetValue(secretId, out var value);
        return Task.FromResult(value);
    }

    public Task DeleteAsync(string secretId, CancellationToken cancellationToken)
    {
        _secrets.TryRemove(secretId, out _);
        return Task.CompletedTask;
    }
}
