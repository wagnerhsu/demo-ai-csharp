# Changelog

## [1.0.0] - 2026-04-30

### Added

- Initial release of PortScanner (.NET 8.0 Console)
- TCP port scanning with configurable IP, port range, and per-port timeout
- Concurrent scanning via `SemaphoreSlim` (up to 20 simultaneous probes)
- Real-time single-line progress bar with port status indicator
- Post-scan summary table listing open ports with latency, plus Open/Closed/Timeout counts and total duration
- Command-line argument support: `<ip> <startPort> <endPort> [timeout_ms]` with sensible defaults
