using Microsoft.Data.Sqlite;
using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Infrastructure.SshNet.Persistence;

public sealed class SqliteHostProfileService : IHostProfileService
{
    private readonly AppDatabase _db;

    public SqliteHostProfileService(AppDatabase db) => _db = db;

    public async Task<IReadOnlyList<HostProfile>> GetAllAsync(CancellationToken cancellationToken)
    {
        var results = new List<HostProfile>();

        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Name, Host, Port, Username, AuthenticationType,
                   PasswordSecretId, PrivateKeyPath, PrivateKeyPassphraseSecretId
            FROM HostProfiles
            ORDER BY Name COLLATE NOCASE
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new HostProfile
            {
                Name = reader.GetString(0),
                Host = reader.GetString(1),
                Port = reader.GetInt32(2),
                Username = reader.GetString(3),
                AuthenticationType = (AuthenticationType)reader.GetInt32(4),
                PasswordSecretId = reader.IsDBNull(5) ? null : reader.GetString(5),
                PrivateKeyPath = reader.IsDBNull(6) ? null : reader.GetString(6),
                PrivateKeyPassphraseSecretId = reader.IsDBNull(7) ? null : reader.GetString(7),
            });
        }

        return results;
    }

    public async Task SaveAsync(HostProfile profile, CancellationToken cancellationToken)
    {
        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO HostProfiles
                (Name, Host, Port, Username, AuthenticationType,
                 PasswordSecretId, PrivateKeyPath, PrivateKeyPassphraseSecretId)
            VALUES
                (@name, @host, @port, @username, @authType,
                 @pwdSecretId, @keyPath, @passphraseSecretId)
            ON CONFLICT(Name) DO UPDATE SET
                Host                         = excluded.Host,
                Port                         = excluded.Port,
                Username                     = excluded.Username,
                AuthenticationType           = excluded.AuthenticationType,
                PasswordSecretId             = excluded.PasswordSecretId,
                PrivateKeyPath               = excluded.PrivateKeyPath,
                PrivateKeyPassphraseSecretId = excluded.PrivateKeyPassphraseSecretId;
            """;

        cmd.Parameters.AddWithValue("@name", profile.Name);
        cmd.Parameters.AddWithValue("@host", profile.Host);
        cmd.Parameters.AddWithValue("@port", profile.Port);
        cmd.Parameters.AddWithValue("@username", profile.Username);
        cmd.Parameters.AddWithValue("@authType", (int)profile.AuthenticationType);
        cmd.Parameters.AddWithValue("@pwdSecretId", (object?)profile.PasswordSecretId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@keyPath", (object?)profile.PrivateKeyPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@passphraseSecretId", (object?)profile.PrivateKeyPassphraseSecretId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string profileName, CancellationToken cancellationToken)
    {
        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM HostProfiles WHERE Name = @name";
        cmd.Parameters.AddWithValue("@name", profileName);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
