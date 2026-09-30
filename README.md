# YAPS - Yet Another Port Scanner

A small WPF TCP connect port scanner built on .NET 8, modeled on the classic YAPS utility (see `01.png` in the repo root).

> Only scan hosts and networks you own or are explicitly authorized to test.

## Requirements

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Build and run

```bash
dotnet run --project Yaps
```

To build only:

```bash
dotnet build Yaps -c Release
```

## Usage

| Field / option | Description |
| --- | --- |
| Start Port / Stop Port | Inclusive port range to scan (1-65535). |
| Start Address / Stop Address | Inclusive IPv4 range to scan. Use the same value for a single host. |
| Timeout (ms) | How long to wait for a connection before it counts as a timeout. |
| Simultaneous | Maximum number of concurrent connection attempts (1-10000). |
| scan ports first | For each address, scan all ports before moving to the next address. |
| scan IPs first | For each port, scan all addresses before moving to the next port. |
| Resolve names | Reverse DNS lookup for hosts with open ports. |
| Continuous | Repeat the scan until Stop is pressed. |
| Hide Errors | Hide timeouts and socket errors from the output. |
| Hex | Show the data received from the server as a hex string (`53 53 48 ...`) instead of ASCII text. Can be toggled while a scan is running. |
| Probe Ports | If the service sends nothing after connecting, send `HEAD / HTTP/1.0` and show its reply (waits up to 1 s). Without this option, only data the server volunteers is shown. |

Buttons: **Start** begins the scan, **Stop** cancels it, **Clear** empties the output, **Close** exits, **About** shows program info.

### Output

- The status line shows the current target with `connect` and `refuse` counts. `timeout` and `error` counts appear once there are any.
- Each open port is listed as `ip:port open`, followed by the resolved name (if enabled) and whatever the server sent after connecting.
- After connecting, the scanner always waits up to 1 s for a greeting (SSH, FTP, SMTP, custom TCP servers...) and keeps reading until the sender pauses, up to 512 bytes. - By default the data is shown as text (UTF-8): newlines become spaces and control characters become `.`. With Hex checked, it is shown as space-separated hex bytes instead.
- Refused ports are counted but not listed.
- With Hide Errors unchecked, timeouts and other socket errors are listed too.

## Configuration

Initial values for the fields and checkboxes are read from `appsettings.json`, which is copied next to `Yaps.exe` on build. Edit the copy in the output folder to change defaults without rebuilding, or edit `Yaps/appsettings.json` and rebuild.

```json
{
  "Defaults": {
    "StartPort": 3500,
    "StopPort": 3600,
    "TimeoutMs": 2500,
    "Simultaneous": 100,
    "StartAddress": "192.168.100.1",
    "StopAddress": "192.168.100.1",
    "ScanOrder": "PortsFirst",
    "ResolveNames": false,
    "Continuous": false,
    "HideErrors": false,
    "ProbePorts": false,
    "Hex": false
  }
}
```

`ScanOrder` is `PortsFirst` or `IpsFirst`. Any key you omit, or a missing file, falls back to the values shown above. A file that cannot be parsed also falls back to them, and a warning is shown at startup.

## Project layout

```
Yaps/
  Yaps.csproj            net8.0-windows WPF project
  App.xaml(.cs)          application entry point
  MainWindow.xaml        UI layout
  MainWindow.xaml.cs     input validation, scan control, result display
  PortScanner.cs         scan engine (target enumeration, TCP connect, banner probe)
  AppSettings.cs         loads default values from appsettings.json
  appsettings.json       default field and checkbox values
```
