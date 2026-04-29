using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using RemoteLinuxManager.Application.Security;

namespace RemoteLinuxManager.Infrastructure.SshNet.Persistence;

[SupportedOSPlatform("windows")]
public sealed class SqliteCredentialStore : ICredentialStore
{
    private readonly AppDatabase _db;

    public SqliteCredentialStore(AppDatabase db) => _db = db;

    public async Task SaveAsync(string secretId, string value, CancellationToken cancellationToken)
    {
        var encrypted = Protect(value);

        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Credentials (SecretId, Value)
            VALUES (@id, @val)
            ON CONFLICT(SecretId) DO UPDATE SET Value = excluded.Value;
            """;
        cmd.Parameters.AddWithValue("@id", secretId);
        cmd.Parameters.AddWithValue("@val", encrypted);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(string secretId, CancellationToken cancellationToken)
    {
        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Credentials WHERE SecretId = @id";
        cmd.Parameters.AddWithValue("@id", secretId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull) return null;

        return Unprotect((string)result);
    }

    public async Task DeleteAsync(string secretId, CancellationToken cancellationToken)
    {
        await using var conn = new SqliteConnection(_db.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Credentials WHERE SecretId = @id";
        cmd.Parameters.AddWithValue("@id", secretId);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    // Windows DPAPI: encrypted with the current user's key, unreadable by other users
    private static string Protect(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private static string Unprotect(string value)
    {
        var encrypted = Convert.FromBase64String(value);
        var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
