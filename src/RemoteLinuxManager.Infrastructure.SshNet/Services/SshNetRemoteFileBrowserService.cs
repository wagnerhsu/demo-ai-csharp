using RemoteLinuxManager.Application.Services;

namespace RemoteLinuxManager.Infrastructure.SshNet.Services;

public sealed class SshNetRemoteFileBrowserService : IRemoteFileBrowserService
{
    private readonly SshNetRemoteSessionService _sessionService;

    public SshNetRemoteFileBrowserService(SshNetRemoteSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public Task<IReadOnlyList<RemoteFileEntry>> ListDirectoryAsync(string remotePath, CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<RemoteFileEntry>>(() =>
        {
            var sftp = _sessionService.GetRequiredSftpClient();

            return sftp.ListDirectory(remotePath)
                .Where(f => f.Name != "." && f.Name != "..")
                .OrderByDescending(f => f.IsDirectory)
                .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => new RemoteFileEntry(f.Name, f.FullName, f.IsDirectory, f.Attributes.Size))
                .ToList();
        }, cancellationToken);
    }

    public Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var sftp = _sessionService.GetRequiredSftpClient();
            return sftp.Exists(remotePath);
        }, cancellationToken);
    }
}
