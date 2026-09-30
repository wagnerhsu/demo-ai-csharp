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
    byte[]? BannerData,
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

        var banner = await ReadBannerAsync(client, _options.ProbePorts, token).ConfigureAwait(false);

        return new ScanEvent(address, port, ProbeResult.Connect, banner, null);
    }

    private const int GreetingWaitMs = 1000;
    private const int ProbeReplyWaitMs = 1000;
    private const int IdleGapMs = 200;
    private const int MaxBannerBytes = 512;

    private static readonly byte[] ActiveProbe = "HEAD / HTTP/1.0\r\n\r\n"u8.ToArray();

    private static async Task<byte[]?> ReadBannerAsync(TcpClient client, bool activeProbe, CancellationToken token)
    {
        var stream = client.GetStream();

        // Many services (SSH, FTP, SMTP, custom TCP servers) greet the client first.
        var data = await ReadAvailableAsync(stream, GreetingWaitMs, token).ConfigureAwait(false);
        if (data.Length == 0 && activeProbe)
        {
            // Silent services (HTTP and friends) only answer once spoken to.
            try
            {
                await stream.WriteAsync(ActiveProbe, token).ConfigureAwait(false);
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                return null;
            }

            data = await ReadAvailableAsync(stream, ProbeReplyWaitMs, token).ConfigureAwait(false);
        }

        return data.Length == 0 ? null : data;
    }

    // Waits up to firstWaitMs for data, then keeps collecting chunks until the sender pauses.
    private static async Task<byte[]> ReadAvailableAsync(NetworkStream stream, int firstWaitMs, CancellationToken token)
    {
        var buffer = new byte[MaxBannerBytes];
        int total = 0;
        int wait = firstWaitMs;

        while (total < buffer.Length)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(wait);

            try
            {
                int read = await stream.ReadAsync(buffer.AsMemory(total), cts.Token).ConfigureAwait(false);
                if (read <= 0) break;
                total += read;
                wait = IdleGapMs;
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                break;
            }
        }

        return buffer[..total];
    }

    public static string FormatBanner(byte[] data, bool hex)
    {
        if (hex) return string.Join(' ', data.Select(b => b.ToString("X2")));

        var decoded = Encoding.UTF8.GetString(data);
        var text = new StringBuilder(decoded.Length);

        foreach (char c in decoded)
        {
            if (c is '\r' or '\n' or '\t')
            {
                if (text.Length > 0 && text[^1] != ' ') text.Append(' ');
            }
            else
            {
                text.Append(char.IsControl(c) || c == '�' ? '.' : c);
            }
        }

        var result = text.ToString().Trim();
        return result.Length > 0 ? result : new string('.', Math.Min(data.Length, 32));
    }

    public static uint ToUInt(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
    }

    public static IPAddress ToAddress(uint value) =>
        new(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
}
