using System.Diagnostics;
using System.Net.Sockets;

const string DefaultIp = "192.168.1.78";
const int DefaultStartPort = 8000;
const int DefaultEndPort = 8010;
const int DefaultTimeoutMs = 1000;
const int MaxConcurrency = 20;

string ip = args.Length > 0 ? args[0] : DefaultIp;
int startPort = args.Length > 1 ? int.Parse(args[1]) : DefaultStartPort;
int endPort = args.Length > 2 ? int.Parse(args[2]) : DefaultEndPort;
int timeoutMs = args.Length > 3 ? int.Parse(args[3]) : DefaultTimeoutMs;

int totalPorts = endPort - startPort + 1;

Console.WriteLine($"Port Scanner - .NET 8.0");
Console.WriteLine(new string('=', 60));
Console.WriteLine($"Target : {ip}");
Console.WriteLine($"Ports  : {startPort} - {endPort} ({totalPorts} ports)");
Console.WriteLine($"Timeout: {timeoutMs} ms per port");
Console.WriteLine($"Threads: up to {MaxConcurrency} concurrent");
Console.WriteLine(new string('=', 60));
Console.WriteLine();

var results = new PortResult[totalPorts];
int completed = 0;
var semaphore = new SemaphoreSlim(MaxConcurrency, MaxConcurrency);
var stopwatch = Stopwatch.StartNew();

var tasks = Enumerable.Range(startPort, totalPorts).Select(async port =>
{
    await semaphore.WaitAsync();
    try
    {
        var result = await ProbePortAsync(ip, port, timeoutMs);
        int idx = port - startPort;
        results[idx] = result;

        int done = Interlocked.Increment(ref completed);
        PrintProgress(done, totalPorts, port, result.Status);
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);
stopwatch.Stop();

Console.WriteLine();
Console.WriteLine();
PrintSummary(ip, startPort, endPort, results, stopwatch.Elapsed);

static async Task<PortResult> ProbePortAsync(string ip, int port, int timeoutMs)
{
    var sw = Stopwatch.StartNew();
    using var tcp = new TcpClient();
    try
    {
        var connectTask = tcp.ConnectAsync(ip, port);
        var timeoutTask = Task.Delay(timeoutMs);
        var winner = await Task.WhenAny(connectTask, timeoutTask);

        if (winner == timeoutTask)
            return new PortResult(port, PortStatus.Timeout, -1);

        await connectTask; // re-throw if faulted
        sw.Stop();
        return new PortResult(port, PortStatus.Open, (int)sw.ElapsedMilliseconds);
    }
    catch (SocketException)
    {
        return new PortResult(port, PortStatus.Closed, -1);
    }
    catch
    {
        return new PortResult(port, PortStatus.Timeout, -1);
    }
}

static void PrintProgress(int done, int total, int currentPort, PortStatus status)
{
    int barWidth = 30;
    int filled = (int)Math.Round((double)done / total * barWidth);
    string bar = new string('=', filled) + (filled < barWidth ? ">" : "") + new string(' ', Math.Max(0, barWidth - filled - 1));
    double pct = (double)done / total * 100;
    string statusIcon = status == PortStatus.Open ? "[OPEN]  " : status == PortStatus.Closed ? "[closed]" : "[timeout]";
    Console.Write($"\r  [{bar}] {done,4}/{total} ({pct:0}%)  Port {currentPort} {statusIcon}   ");
}

static void PrintSummary(string ip, int startPort, int endPort, PortResult[] results, TimeSpan elapsed)
{
    int openCount = results.Count(r => r.Status == PortStatus.Open);
    int closedCount = results.Count(r => r.Status == PortStatus.Closed);
    int timeoutCount = results.Count(r => r.Status == PortStatus.Timeout);

    Console.WriteLine(new string('=', 60));
    Console.WriteLine($"  Scan Summary: {ip}  Ports {startPort}-{endPort}");
    Console.WriteLine(new string('=', 60));

    if (openCount > 0)
    {
        Console.WriteLine($"  {"PORT",-8} {"STATUS",-10} {"LATENCY",-10}");
        Console.WriteLine(new string('-', 32));
        foreach (var r in results.Where(r => r.Status == PortStatus.Open).OrderBy(r => r.Port))
            Console.WriteLine($"  {r.Port,-8} {"Open",-10} {r.LatencyMs + " ms",-10}");
        Console.WriteLine();
    }
    else
    {
        Console.WriteLine("  No open ports found.");
        Console.WriteLine();
    }

    Console.WriteLine(new string('-', 60));
    Console.WriteLine($"  Total: {results.Length}  |  Open: {openCount}  |  Closed: {closedCount}  |  Timeout: {timeoutCount}");
    Console.WriteLine($"  Scan duration: {elapsed.TotalSeconds:0.00}s");
    Console.WriteLine(new string('=', 60));
}

enum PortStatus { Open, Closed, Timeout }

record PortResult(int Port, PortStatus Status, int LatencyMs);
