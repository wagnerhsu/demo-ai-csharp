namespace RemoteLinuxManager.Application.Services;

public interface IRemoteFileBrowserService
{
    Task<IReadOnlyList<RemoteFileEntry>> ListDirectoryAsync(string remotePath, CancellationToken cancellationToken);
    Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken);
}

public sealed record RemoteFileEntry(string Name, string FullPath, bool IsDirectory, long Size);
