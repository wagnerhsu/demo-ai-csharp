using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Yaps;

public enum ScanOrder
{
    PortsFirst,
    IpsFirst
}

public enum ProbeResult
{
    Connect,
    Refuse,
    Timeout,
    Error
}

public sealed record ScanOptions(
    IPAddress StartAddress,
    IPAddress StopAddress,
    int StartPort,
    int StopPort,
    int TimeoutMs,
    int Simultaneous,
    ScanOrder Order,
    bool Continuous,
    bool ProbePorts);

public sealed record ScanEvent(
    IPAddress Address,
    int Port,
    ProbeResult Result,
    string? Banner,
    string? ErrorMessage);

public sealed class PortScanner
{
    private readonly ScanOptions _options;

    public PortScanner(ScanOptions options) => _options = options;

    public long TotalTargets =>
        (long)(ToUInt(_options.StopAddress) - ToUInt(_options.StartAddress) + 1)
        * (_options.StopPort - _options.StartPort + 1);

    public async Task RunAsync(Action<ScanEvent> onResult, Action<IPAddress, int> onTarget, Action onCycleCompleted, CancellationToken token)
    {
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.Simultaneous,
            CancellationToken = token
        };

        do
        {
            await Parallel.ForEachAsync(EnumerateTargets(), parallelOptions, async (target, ct) =>
            {
                onTarget(target.Address, target.Port);
                var result = await ProbeAsync(target.Address, target.Port, ct).ConfigureAwait(false);
                onResult(result);
            }).ConfigureAwait(false);

            onCycleCompleted();
        }
        while (_options.Continuous && !token.IsCancellationRequested);
    }

    private IEnumerable<(IPAddress Address, int Port)> EnumerateTargets()
    {
        uint first = ToUInt(_options.StartAddress);
        uint last = ToUInt(_options.StopAddress);

        if (_options.Order == ScanOrder.PortsFirst)
        {
            for (uint ip = first; ip <= last; ip++)
            {
                for (int port = _options.StartPort; port <= _options.StopPort; port++)
                    yield return (ToAddress(ip), port);
                if (ip == uint.MaxValue) yield break;
            }
        }
        else
        {
            for (int port = _options.StartPort; port <= _options.StopPort; port++)
            {
                for (uint ip = first; ip <= last; ip++)
                {
                    yield return (ToAddress(ip), port);
                    if (ip == uint.MaxValue) break;
                }
            }
        }
    }

    private async Task<ScanEvent> ProbeAsync(IPAddress address, int port, CancellationToken token)
    {
        using var client = new TcpClient(AddressFamily.InterNetwork);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_options.TimeoutMs);

        try
        {
            await client.ConnectAsync(address, port, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new ScanEvent(address, port, ProbeResult.Timeout, null, null);
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
        {
            return new ScanEvent(address, port, ProbeResult.Refuse, null, null);
        }
        catch (SocketException ex)
        {
            return new ScanEvent(address, port, ProbeResult.Error, null, ex.SocketErrorCode.ToString());
        }

        string? banner = null;
        if (_options.ProbePorts)
            banner = await ReadBannerAsync(client, token).ConfigureAwait(false);

        return new ScanEvent(address, port, ProbeResult.Connect, banner, null);
    }

    private static readonly byte[] ActiveProbe = "HEAD / HTTP/1.0\r\n\r\n"u8.ToArray();

    private static async Task<string?> ReadBannerAsync(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();

        // Services like SSH/FTP/SMTP greet the client first, so listen briefly before sending anything.
        var banner = await ReadWithTimeoutAsync(stream, 500, token).ConfigureAwait(false);
        if (banner is not null) return banner;

        // Silent services (HTTP and friends) only answer once spoken to.
        try
        {
            await stream.WriteAsync(ActiveProbe, token).ConfigureAwait(false);
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            return null;
        }

        return await ReadWithTimeoutAsync(stream, 750, token).ConfigureAwait(false);
    }

    private static async Task<string?> ReadWithTimeoutAsync(NetworkStream stream, int timeoutMs, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(timeoutMs);
        var buffer = new byte[256];

        try
        {
            int read = await stream.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
            if (read <= 0) return null;

            var text = new StringBuilder(read);
            for (int i = 0; i < read; i++)
                text.Append(buffer[i] is >= 32 and < 127 ? (char)buffer[i] : '.');
            var result = text.ToString().Trim('.', ' ');
            return result.Length > 0 ? result : null;
        }
        catch (Exception) when (!token.IsCancellationRequested)
        {
            return null;
        }
    }

    public static uint ToUInt(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    public static IPAddress ToAddress(uint value) =>
        new(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
}
