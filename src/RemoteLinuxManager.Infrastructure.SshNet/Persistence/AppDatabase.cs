using Microsoft.Data.Sqlite;

namespace RemoteLinuxManager.Infrastructure.SshNet.Persistence;

public sealed class AppDatabase
{
    public string ConnectionString { get; }

    public AppDatabase()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RemoteLinuxManager");

        Directory.CreateDirectory(dir);

        ConnectionString = $"Data Source={Path.Combine(dir, "profiles.db")}";

        InitializeSchema();
    }

    private void InitializeSchema()
    {
        using var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS HostProfiles (
                Name                        TEXT PRIMARY KEY COLLATE NOCASE,
                Host                        TEXT NOT NULL,
                Port                        INTEGER NOT NULL,
                Username                    TEXT NOT NULL,
                AuthenticationType          INTEGER NOT NULL,
                PasswordSecretId            TEXT,
                PrivateKeyPath              TEXT,
                PrivateKeyPassphraseSecretId TEXT
            );
            CREATE TABLE IF NOT EXISTS Credentials (
                SecretId TEXT PRIMARY KEY COLLATE NOCASE,
                Value    TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }
}
