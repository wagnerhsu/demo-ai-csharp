using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Yaps;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly ConcurrentQueue<string> _pending = new();
    private readonly ConcurrentDictionary<IPAddress, string> _dnsCache = new();

    private CancellationTokenSource? _cts;
    private long _connect, _refuse, _timeouts, _errors, _completed, _total;
    private volatile string _currentTarget = "";
    private volatile bool _resolveNames;
    private volatile bool _hideErrors;
    private volatile bool _hex;

    public MainWindow()
    {
        InitializeComponent();
        _uiTimer.Tick += (_, _) => FlushUi();
        Closing += (_, _) =>
        {
            _cts?.Cancel();
            _uiTimer.Stop();
        };
        UpdateStatus();
    }

    private async void OnStartClick(object sender, RoutedEventArgs e) => await StartScanAsync();

    private void OnStopClick(object sender, RoutedEventArgs e) => _cts?.Cancel();

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnClearClick(object sender, RoutedEventArgs e) => OutputBox.Clear();

    private void OnAboutClick(object sender, RoutedEventArgs e) => MessageBox.Show(
        this,
        "YAPS - Yet Another Port Scanner\r\nA simple TCP connect scanner built on .NET 8 and WPF.\r\n\r\nOnly scan hosts you are authorized to test.",
        "About",
        MessageBoxButton.OK,
        MessageBoxImage.Information);

    // The scanner reads these from worker threads, so mirror them into fields instead of touching the controls.
    private void OnLiveOptionChanged(object sender, RoutedEventArgs e)
    {
        _resolveNames = ResolveNamesCheck.IsChecked == true;
        _hideErrors = HideErrorsCheck.IsChecked == true;
        _hex = HexCheck.IsChecked == true;
    }

    private async Task StartScanAsync()
    {
        if (!TryReadOptions(out var options)) return;

        var scanner = new PortScanner(options);
        _connect = _refuse = _timeouts = _errors = _completed = 0;
        _total = scanner.TotalTargets;
        _currentTarget = "";
        _dnsCache.Clear();

        var cts = new CancellationTokenSource();
        _cts = cts;
        SetRunning(true);
        _pending.Enqueue("Started scan");
        _uiTimer.Start();

        try
        {
            await Task.Run(() => scanner.RunAsync(
                OnResult,
                (address, port) => _currentTarget = $"{address}:{port}",
                () => Interlocked.Exchange(ref _completed, 0),
                cts.Token));
            _pending.Enqueue("Scan complete");
        }
        catch (OperationCanceledException)
        {
            _pending.Enqueue("Stopping scan");
        }
        catch (Exception ex)
        {
            _pending.Enqueue($"Error: {ex.Message}");
        }
        finally
        {
            _uiTimer.Stop();
            FlushUi();
            cts.Dispose();
            _cts = null;
            SetRunning(false);
        }
    }

    private void OnResult(ScanEvent e)
    {
        Interlocked.Increment(ref _completed);

        switch (e.Result)
        {
            case ProbeResult.Connect:
                Interlocked.Increment(ref _connect);
                var line = new StringBuilder($"{e.Address}:{e.Port} open");
                if (_resolveNames) line.Append($" [{ResolveName(e.Address)}]");
                if (e.BannerData is { Length: > 0 } data)
                    line.Append($"  \"{PortScanner.FormatBanner(data, _hex)}\"");
                _pending.Enqueue(line.ToString());
                break;
            case ProbeResult.Refuse:
                Interlocked.Increment(ref _refuse);
                break;
            case ProbeResult.Timeout:
                Interlocked.Increment(ref _timeouts);
                if (!_hideErrors) _pending.Enqueue($"{e.Address}:{e.Port} timeout");
                break;
            default:
                Interlocked.Increment(ref _errors);
                if (!_hideErrors) _pending.Enqueue($"{e.Address}:{e.Port} error ({e.ErrorMessage})");
                break;
        }
    }

    private string ResolveName(IPAddress address) =>
        _dnsCache.GetOrAdd(address, static a =>
        {
            try { return Dns.GetHostEntry(a).HostName; }
            catch (Exception) { return "?"; }
        });

    private void FlushUi()
    {
        if (!_pending.IsEmpty)
        {
            var text = new StringBuilder();
            while (_pending.TryDequeue(out var line))
                text.AppendLine(line);
            OutputBox.AppendText(text.ToString());
            OutputBox.ScrollToEnd();
        }

        UpdateStatus();
        ScanProgress.Value = _total > 0 ? Math.Min(1.0, (double)Interlocked.Read(ref _completed) / _total) : 0;
    }

    private void UpdateStatus() =>
        StatusText.Text = $"{_currentTarget}  connect {_connect}  refuse {_refuse}" +
                          (_timeouts + _errors > 0 ? $"  timeout {_timeouts}  error {_errors}" : "");

    private void SetRunning(bool running)
    {
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
        foreach (var control in new UIElement[]
                 {
                     StartPortBox, StopPortBox, TimeoutBox, SimultaneousBox, StartAddressBox, StopAddressBox,
                     PortsFirstRadio, IpsFirstRadio, ContinuousCheck, ProbePortsCheck
                 })
            control.IsEnabled = !running;

        if (!running) ScanProgress.Value = 0;
    }

    private bool TryReadOptions(out ScanOptions options)
    {
        options = null!;

        if (!TryParseInt(StartPortBox, "Start Port", 1, 65535, out int startPort) ||
            !TryParseInt(StopPortBox, "Stop Port", 1, 65535, out int stopPort) ||
            !TryParseInt(TimeoutBox, "Timeout", 1, 600_000, out int timeout) ||
            !TryParseInt(SimultaneousBox, "Simultaneous", 1, 10_000, out int simultaneous) ||
            !TryParseAddress(StartAddressBox, "Start Address", out var startAddress) ||
            !TryParseAddress(StopAddressBox, "Stop Address", out var stopAddress))
            return false;

        if (startPort > stopPort)
            return Fail(StartPortBox, "Start Port must not be greater than Stop Port.");
        if (PortScanner.ToUInt(startAddress) > PortScanner.ToUInt(stopAddress))
            return Fail(StartAddressBox, "Start Address must not be greater than Stop Address.");

        options = new ScanOptions(
            startAddress, stopAddress, startPort, stopPort, timeout, simultaneous,
            PortsFirstRadio.IsChecked == true ? ScanOrder.PortsFirst : ScanOrder.IpsFirst,
            ContinuousCheck.IsChecked == true, ProbePortsCheck.IsChecked == true);
        return true;
    }

    private bool TryParseInt(TextBox box, string name, int min, int max, out int value)
    {
        if (int.TryParse(box.Text.Trim(), out value) && value >= min && value <= max) return true;
        return Fail(box, $"{name} must be a number between {min} and {max}.");
    }

    private bool TryParseAddress(TextBox box, string name, out IPAddress address)
    {
        if (IPAddress.TryParse(box.Text.Trim(), out var parsed) &&
            parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            address = parsed;
            return true;
        }

        address = IPAddress.None;
        return Fail(box, $"{name} must be a valid IPv4 address.");
    }

    private bool Fail(Control control, string message)
    {
        MessageBox.Show(this, message, "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
        control.Focus();
        return false;
    }
}
