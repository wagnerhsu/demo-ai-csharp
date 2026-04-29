namespace RemoteLinuxManager.Application.Services;

public sealed record ConnectionSecret
{
    public string? Password { get; init; }

    public string? PrivateKeyPassphrase { get; init; }
}
