using System.Diagnostics;
using System.Net.Sockets;

namespace DshLauncher;

/// <summary>
/// dsh service watchdog: polls the target port and silently restarts the dsh
/// service (same start-dsh.vbs path used at boot) when it drops, then reloads
/// the page once the service is back. Throttled to 5 restarts per 10 minutes;
/// skipped entirely when an external DSH_WEB_URL is configured.
/// </summary>
public sealed class WatchdogService : IDisposable
{
    private readonly int _port;
    private readonly string _vbsPath;
    private readonly Action _onRecovered;
    private readonly Action<string, string> _log;
    private readonly object _lock = new();
    private System.Threading.Timer? _timer;
    private bool _starting;
    private int _restartCount;
    private DateTimeOffset _windowStart;
    private DateTimeOffset _lastRestartAt;

    public const int MaxRestartsPerWindow = 5;
    public static readonly TimeSpan WindowMs = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan GraceMs = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan PollMs = TimeSpan.FromSeconds(5);

    public WatchdogService(int port, string vbsPath, Action onRecovered, Action<string, string>? log = null)
    {
        _port = port;
        _vbsPath = vbsPath;
        _onRecovered = onRecovered;
        _log = log ?? ((_, _) => { });
    }

    public void Start()
    {
        _timer = new System.Threading.Timer(_ => Poll(), null, PollMs, PollMs);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        GC.SuppressFinalize(this);
    }

    private void Poll()
    {
        if (PortOpen(_port)) return;
        bool shouldRestart;
        lock (_lock)
        {
            // give an in-flight restart its grace period before probing again
            if (_starting && DateTimeOffset.Now - _lastRestartAt < GraceMs) return;

            var now = DateTimeOffset.Now;
            if (_restartCount == 0) _windowStart = now;
            else if (now - _windowStart > WindowMs)
            {
                _windowStart = now;
                _restartCount = 0;
            }
            shouldRestart = _restartCount < MaxRestartsPerWindow;
            _starting = true;
        }

        if (!shouldRestart)
        {
            _log("watchdog", $"dsh 服务不可用且已达重启上限（{MaxRestartsPerWindow} 次/10 分钟），停止自动重启");
            return;
        }

        if (!File.Exists(_vbsPath))
        {
            _log("watchdog", $"未找到 {_vbsPath}，无法重启 dsh 服务");
            lock (_lock) _starting = false;
            return;
        }

        lock (_lock)
        {
            _restartCount++;
            _lastRestartAt = DateTimeOffset.Now;
        }
        _log("watchdog", $"dsh 服务断开（端口 {_port}），自动重启（第 {_restartCount}/{MaxRestartsPerWindow} 次）");

        try
        {
            Process.Start(new ProcessStartInfo("wscript.exe", "\"" + _vbsPath + "\"") { UseShellExecute = true });
        }
        catch (Exception err)
        {
            _log("watchdog", "启动 dsh 失败: " + err.Message);
            lock (_lock) _starting = false;
            return;
        }

        // Wait for the service to recover, then notify (page reload). If it
        // never comes back within the window, reset the flag and let the next
        // poll cycle try again.
        Task.Run(async () =>
        {
            var deadline = DateTimeOffset.Now + TimeSpan.FromSeconds(90);
            while (DateTimeOffset.Now < deadline)
            {
                if (PortOpen(_port))
                {
                    lock (_lock) _starting = false;
                    _log("watchdog", "dsh 服务已恢复，重载页面");
                    try { _onRecovered(); } catch (Exception err) { _log("watchdog", "恢复回调异常: " + err.Message); }
                    return;
                }
                await Task.Delay(1000);
            }
            lock (_lock) _starting = false;
            _log("watchdog", "等待 90 秒后服务仍未恢复");
        });
    }

    /// <summary>Probe whether the port accepts connections on 127.0.0.1.</summary>
    public static bool PortOpen(int port)
    {
        try
        {
            using var c = new TcpClient();
            c.Connect("127.0.0.1", port);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
