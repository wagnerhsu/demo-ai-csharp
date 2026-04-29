# Remote Linux Manager

A WPF desktop application for managing remote Linux servers via SSH/SFTP. Built with clean architecture, Fluent Design UI, and persistent host profile storage.

## Features

- **SSH connection management** — password and private key (with passphrase) authentication
- **File transfer** — dual-pane tree view (local ↔ remote) for upload and download via SFTP
- **Script execution** — run inline scripts or local shell scripts on the remote host, with optional `sudo` support
- **Host profile persistence** — profiles stored in SQLite; passwords encrypted with Windows DPAPI
- **Log output** — timestamped log panel with right-click menu (clear, scroll to end, Ctrl+F search)
- **Fluent Design UI** — WPF UI (lepoco/wpf-ui) with Mica backdrop and light/dark theme switching
- **Structured logging** — Serilog writing to console, rolling file, and Seq

## Architecture

```
RemoteLinuxManager.sln
├── src/
│   ├── RemoteLinuxManager.Domain            # Models: HostProfile, AuthenticationType, transfer/script models
│   ├── RemoteLinuxManager.Application       # Service interfaces and ConnectionSecret
│   ├── RemoteLinuxManager.Infrastructure.SshNet   # SSH.NET implementations + SQLite persistence
│   └── RemoteLinuxManager.App               # WPF UI (FluentWindow, MainViewModel, MVVM)
└── tests/
    ├── RemoteLinuxManager.UnitTests         # In-memory service unit tests
    └── RemoteLinuxManager.IntegrationTests  # DI registration smoke tests
```

### Key interfaces (`Application` layer)

| Interface | Purpose |
|---|---|
| `IRemoteSessionService` | Connect / disconnect SSH |
| `IFileTransferService` | Upload / download via SFTP |
| `IScriptExecutionService` | Execute shell scripts remotely |
| `IRemoteFileBrowserService` | List remote directory contents |
| `IHostProfileService` | CRUD for host profiles |
| `ICredentialStore` | Secure credential storage |

### Infrastructure (`Infrastructure.SshNet`)

- **SSH.NET 2025.x** — session, SFTP, command execution
- **`sudo -S`** — password injected via stdin when sudo is required (no PTY needed)
- **SQLite** (`Microsoft.Data.Sqlite`) — profiles at `%LOCALAPPDATA%\RemoteLinuxManager\profiles.db`
- **Windows DPAPI** — `ProtectedData.Protect/Unprotect` with `DataProtectionScope.CurrentUser`

## Requirements

- Windows 10 / 11 (DPAPI and Mica backdrop require Windows)
- .NET 8.0 SDK
- (Optional) [Seq](https://datalust.co/seq) at `http://localhost:5341` for structured log viewing

## Build

```bash
dotnet build RemoteLinuxManager.sln
```

## Run

```bash
dotnet run --project src/RemoteLinuxManager.App
```

## Test

```bash
dotnet test RemoteLinuxManager.sln
```

## Logging

Logging is configured via [`src/RemoteLinuxManager.App/serilog.json`](src/RemoteLinuxManager.App/serilog.json).

| Sink | Detail |
|---|---|
| Console | All levels ≥ Debug |
| File | `Logs/RemoteLinuxManager-<date>.log`, daily rolling, 7-day retention |
| Seq | `http://localhost:5341`, levels ≥ Information |

Enriched with: machine name, process ID, thread ID.

## UI Controls

| Shortcut / Action | Effect |
|---|---|
| Right-click log panel | Clear logs / scroll to end / open search |
| `Ctrl+F` in log panel | Open inline search bar |
| `Enter` / `F3` | Next search match |
| `Shift+Enter` / `Shift+F3` | Previous match |
| `Esc` | Close search bar |
| View → Theme | Switch between Light and Dark |
