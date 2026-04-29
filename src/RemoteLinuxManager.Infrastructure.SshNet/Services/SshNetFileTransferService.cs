using RemoteLinuxManager.Application.Services;
using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Infrastructure.SshNet.Services;

public sealed class SshNetFileTransferService : IFileTransferService
{
    private readonly SshNetRemoteSessionService _sessionService;

    public SshNetFileTransferService(SshNetRemoteSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<TransferResult> UploadAsync(
        TransferRequest request,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Direction != TransferDirection.Upload)
        {
            return new TransferResult(false, "Transfer direction must be Upload.");
        }

        if (!File.Exists(request.LocalPath))
        {
            return new TransferResult(false, $"Local file not found: {request.LocalPath}");
        }

        try
        {
            var sftpClient = _sessionService.GetRequiredSftpClient();

            await Task.Run(() =>
            {
                using var stream = File.OpenRead(request.LocalPath);
                var total = stream.Length;
                sftpClient.UploadFile(stream, request.RemotePath, uploaded =>
                {
                    progress?.Report(new TransferProgress((long)uploaded, total));
                });
            }, cancellationToken);

            return new TransferResult(true);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, ex.Message);
        }
    }

    public async Task<TransferResult> DownloadAsync(
        TransferRequest request,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (request.Direction != TransferDirection.Download)
        {
            return new TransferResult(false, "Transfer direction must be Download.");
        }

        try
        {
            var sftpClient = _sessionService.GetRequiredSftpClient();

            await Task.Run(() =>
            {
                var attributes = sftpClient.GetAttributes(request.RemotePath);
                var total = attributes.Size;

                var localDirectory = Path.GetDirectoryName(request.LocalPath);
                if (!string.IsNullOrWhiteSpace(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                using var output = File.Open(request.LocalPath, FileMode.Create, FileAccess.Write, FileShare.None);
                sftpClient.DownloadFile(request.RemotePath, output, downloaded =>
                {
                    progress?.Report(new TransferProgress((long)downloaded, total));
                });
            }, cancellationToken);

            return new TransferResult(true);
        }
        catch (Exception ex)
        {
            return new TransferResult(false, ex.Message);
        }
    }
}
