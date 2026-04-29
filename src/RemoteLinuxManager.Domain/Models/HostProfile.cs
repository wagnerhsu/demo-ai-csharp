namespace RemoteLinuxManager.Domain.Models;

public sealed record HostProfile
{
    public required string Name { get; init; }

    public required string Host { get; init; }

    public int Port { get; init; } = 22;

    public required string Username { get; init; }

    public AuthenticationType AuthenticationType { get; init; }

    public string? PasswordSecretId { get; init; }

    public string? PrivateKeyPath { get; init; }

    public string? PrivateKeyPassphraseSecretId { get; init; }
}
