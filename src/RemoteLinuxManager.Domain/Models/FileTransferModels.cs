namespace RemoteLinuxManager.Domain.Models;

public enum TransferDirection
{
    Upload,
    Download
}

public sealed record TransferRequest
{
    public required string LocalPath { get; init; }

    public required string RemotePath { get; init; }

    public TransferDirection Direction { get; init; }
}

public sealed record TransferProgress(long BytesTransferred, long TotalBytes)
{
    public double Percentage => TotalBytes <= 0 ? 0 : (double)BytesTransferred / TotalBytes * 100d;
}

public sealed record TransferResult(bool Succeeded, string? ErrorMessage = null);
