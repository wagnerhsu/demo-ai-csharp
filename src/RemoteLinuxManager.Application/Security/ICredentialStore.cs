namespace RemoteLinuxManager.Application.Security;

public interface ICredentialStore
{
    Task SaveAsync(string secretId, string value, CancellationToken cancellationToken);

    Task<string?> GetAsync(string secretId, CancellationToken cancellationToken);

    Task DeleteAsync(string secretId, CancellationToken cancellationToken);
}
