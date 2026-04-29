# Changelog

## Unreleased

### Added
- Serilog structured logging: console, daily rolling file (`Logs/RemoteLinuxManager-<date>.log`), and Seq sink (`http://localhost:5341`)
- `serilog.json` configuration file; enriched with machine name, process ID, and thread ID
- `Microsoft.Extensions.Logging` wired to Serilog via `AddSerilog()` so injected `ILogger<T>` resolves through Serilog
- Remote file tree defaults to `/home/{username}` on connect if the directory exists, falling back to `/`
- `IRemoteFileBrowserService.DirectoryExistsAsync` added; implemented via `SftpClient.Exists`

---

## v0.6.0

### Added
- Light / dark theme switching via **View → Theme** menu in the title bar area
- Default theme changed to **Light**

### Changed
- Merged **脚本执行** and **日志输出** panels into a single `TabControl` with two tabs to reduce vertical space usage

---

## v0.5.0

### Added
- **WPF UI (Fluent)** integration (`WPF-UI` 4.2.1 by lepoco)
  - `FluentWindow` with Mica backdrop (`WindowBackdropType="Mica"`)
  - All `GroupBox` sections replaced with `ui:Card`
  - `ui:Button` with Fluent icons and `Appearance` (Primary / Secondary)
  - `ui:PasswordBox` with TwoWay binding — eliminates all PasswordBox code-behind event handlers
  - Connection status indicator: colored `Ellipse` + `TextBlock` (green = connected, red = disconnected)

---

## v0.4.0

### Added
- **SQLite persistence** for host profiles (`%LOCALAPPDATA%\RemoteLinuxManager\profiles.db`)
- **Windows DPAPI** credential encryption (`ProtectedData` with `DataProtectionScope.CurrentUser`)
- `SqliteHostProfileService` and `SqliteCredentialStore` replacing in-memory implementations
- Credentials auto-loaded when a saved profile is selected (password / passphrase synced back to UI)

---

## v0.3.0

### Added
- **Log panel right-click context menu**: Copy, Select All, Clear Logs, Scroll to End, Find (`Ctrl+F`)
- **Inline search bar** in the log panel: keyword highlighting, match counter, `Enter`/`F3` navigation, `Esc` to close

---

## v0.2.0

### Added
- **Dual-pane file transfer UI**: local file tree (left) and remote SFTP tree (right) replacing manual path TextBoxes
- Lazy-loading `FileTreeNode` — directories expand on first click, remote tree loaded via `IRemoteFileBrowserService`
- `SshNetRemoteFileBrowserService` using `SftpClient.ListDirectory`
- Upload path derived from selected local file + remote directory; download path from selected remote file + local directory
- Remote tree auto-initialized at `/` after successful connection; cleared on disconnect
- **Sudo password** field added to script execution; scripts using `sudo` automatically rewritten to `sudo -S` with password injected via stdin (avoids "terminal required" error when no PTY is allocated)

---

## v0.1.0

### Added
- Initial release: SSH connection (password and private key authentication)
- File upload / download via SFTP
- Inline and local-file script execution
- Password and passphrase fields masked with `PasswordBox`
