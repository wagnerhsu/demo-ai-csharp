using RemoteLinuxManager.Domain.Models;

namespace RemoteLinuxManager.Application.Services;

public interface IFileTransferService
{
    Task<TransferResult> UploadAsync(
        TransferRequest request,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken);

    Task<TransferResult> DownloadAsync(
        TransferRequest request,
        IProgress<TransferProgress>? progress,
        CancellationToken cancellationToken);
}
