# PortScanner

A .NET 8.0 Console application for scanning TCP ports on a target IP address. Shows real-time progress and prints a summary on completion.

## Requirements

- .NET 8.0 SDK

## Usage

```bash
dotnet run -- <ip> <startPort> <endPort> [timeout_ms]
```

| Argument | Description | Default |
|---|---|---|
| `ip` | Target IP address | `192.168.1.78` |
| `startPort` | First port to scan | `8000` |
| `endPort` | Last port to scan | `8010` |
| `timeout_ms` | Timeout per port (milliseconds) | `1000` |

### Examples

```bash
# Use defaults (192.168.1.78, ports 8000-8010)
dotnet run

# Scan specific range
dotnet run -- 192.168.1.78 8000 8010

# Scan with custom timeout (500ms)
dotnet run -- 10.0.0.1 80 443 500
```

## Sample Output

```
Port Scanner - .NET 8.0
============================================================
Target : 192.168.1.78
Ports  : 8000 - 8010 (11 ports)
Timeout: 1000 ms per port
Threads: up to 20 concurrent
============================================================

  [=====>                        ]    3/11 (27%)  Port 8002 [closed]

============================================================
  Scan Summary: 192.168.1.78  Ports 8000-8010
============================================================
  PORT     STATUS     LATENCY
--------------------------------
  8000     Open       12 ms
  8005     Open       8 ms

------------------------------------------------------------
  Total: 11  |  Open: 2  |  Closed: 8  |  Timeout: 1
  Scan duration: 1.05s
============================================================
```

## Notes

- Up to 20 ports are probed concurrently to speed up scanning.
- A port is reported as **Open** when a TCP connection succeeds.
- A port is reported as **Timeout** when no response is received within the timeout window.
- Only TCP scanning is supported; UDP is not covered.
