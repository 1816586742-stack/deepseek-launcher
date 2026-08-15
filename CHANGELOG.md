# Changelog

## v0.3.6 (2026-08-15)

### Added
- Download handling: fixed to the system Downloads folder, Content-Disposition/RFC 5987 filename parsing, blob: downloads get an extension from MIME type, same-name auto-rename, reserved device names sanitized (CON/NUL/COM1...); safe extensions (pdf/zip/images/media) auto-open when done, executable surfaces (.html/.svg/.exe...) never auto-open
- Popup classification: external http(s) links → system browser; same-origin popups → lightweight in-shell window sharing the session; blob:/data:/about: keep default behavior
- Balance query: right-click → Balance shows the DeepSeek account balance (key from DEEPSEEK_API_KEY env or ~/.dsh/.credentials.yaml; endpoint overridable via DEEPSEEK_BALANCE_URL / DEEPSEEK_API_BASE)
- ShellLogic + BalanceService unit tests (61 new, 87 total)

### Changed
- Permission auto-grant policy moved into ShellLogic (shared by main window and popups)

## v0.3.5 (2026-08-15)

### Added
- Session-done notifications: watches `<DSH_HOME>/sessions` zstd logs incrementally (baseline + tail-only decode, 5s directory cache) and shows a tray balloon when a top-level agent turn ends; title from session/title, body carries cwd tail + short session id, multi-turn count; subagent logs ignored
- Tray icon: hosts notifications, double-click restores/focuses the window
- dsh service watchdog: polls the port every 5s, silently restarts the service via start-dsh.vbs on drop and reloads the page after recovery; throttled to 5 restarts per 10 minutes
- Renderer crash recovery: auto-reload on render process exit/unresponsive (10s throttle)
- Plugin permission auto-grant: notifications / clipboard / autoplay / multi-download / persistent storage (mic/camera stay denied)
- ZstdFrames + SessionWatcher unit/integration tests (16 new, 26 total)

### Changed
- dsh service port check now reuses WatchdogService.PortOpen

## v0.3.4 (2026-08-14)

### Added
- About dialog with version info, license, and links (Bili23-style)
- Professional update dialog with version comparison and changelog
- "Skip this version" functionality
- Security policy (SECURITY.md)
- Windows build script (build-windows.sh)

### Fixed
- macOS Swift compilation errors
- exe icon not displaying
- Version numbers aligned across all files
- SettingsManager JSON serialization (now uses System.Text.Json)
- Removed tracked .csproj.user file
- Expanded .gitignore

## v0.2.0 (2026-08-14)

### Added
- Auto-update: checks GitHub releases for new versions on startup
- Settings panel: right-click → Settings (port, auto-start dsh)
- DeepSeek whale icon: official logo as app icon
- Right-click menu: Settings / Exit
- GitHub Actions CI: auto-build on tag push

### Changed
- Improved error messages
- Better single-instance handling

## v0.1.0 (2026-08-14)

### Added
- Initial release
- Cross-platform scripts (Windows/macOS/Linux)
- Windows desktop version (C# + WebView2)
- macOS desktop version (Swift + WKWebView)
- Linux desktop version (Python + GTK4 + WebKit)
- Auto-start dsh via npx
- Port detection and readiness waiting
- External links open in system browser
