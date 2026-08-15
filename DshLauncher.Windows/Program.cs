using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DshLauncher;

internal static class Program
{
    private static int Port => SettingsManager.Port;
    private static string DefaultUrl => $"http://127.0.0.1:{Port}";
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [STAThread]
    private static void Main()
    {
        // Single instance lock
        using var mutex = new Mutex(true, "DshLauncher.SingleInstance", out var first);
        if (!first)
        {
            var existing = FindWindow(null, "DeepSeek Harness");
            if (existing != IntPtr.Zero)
            {
                ShowWindow(existing, SW_RESTORE);
                SetForegroundWindow(existing);
            }
            return;
        }

        // Auto-start dsh if port not open
        if (!PortOpen(Port))
        {
            var vbs = Path.Combine(AppContext.BaseDirectory, "start-dsh.vbs");
            if (File.Exists(vbs) && SettingsManager.AutoStartDsh)
                Process.Start("wscript.exe", $"\"{vbs}\"");

            // Wait up to 90 seconds
            for (var i = 0; i < 90 && !PortOpen(Port); i++)
                Thread.Sleep(1000);
        }

        if (!PortOpen(Port))
        {
            MessageBox.Show("dsh service not available. Check: %USERPROFILE%\\.dsh",
                "DSH Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Check for updates in background (shows dialog if new version available)
        _ = Task.Run(async () => await UpdateChecker.CheckAndPromptAsync());

        // Load icon from file (DeepSeek whale logo)
        Icon? icon = null;
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
                icon = new Icon(iconPath);
        }
        catch { /* ignore icon load errors */ }

        var form = new Form
        {
            Text = "DeepSeek Harness",
            ClientSize = new Size(1280, 840),
            StartPosition = FormStartPosition.CenterScreen,
            MinimumSize = new Size(800, 600),
            Icon = icon ?? SystemIcons.Application,
        };

        var web = new WebView2 { Dock = DockStyle.Fill };
        form.Controls.Add(web);

        // Context menu: Settings / Balance / About / Exit
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => SettingsManager.ShowDialog());
        menu.Items.Add("Balance", null, async (_, _) =>
        {
            try
            {
                var dshHome = GetDshHome();
                var result = await BalanceService.QueryBalanceAsync(dshHome);
                MessageBox.Show(BalanceService.FormatForNotification(result), "DeepSeek Balance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { /* ignore */ }
        });
        menu.Items.Add("About", null, (_, _) => { var dlg = new AboutDialog(); dlg.ShowDialog(); });
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => { web.Dispose(); Application.Exit(); });
        form.ContextMenuStrip = menu;

        form.Load += async (_, _) =>
        {
            var userData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DshLauncher", "WebView2");
            Directory.CreateDirectory(userData);

            var env = await CoreWebView2Environment.CreateAsync(null, userData);
            await web.EnsureCoreWebView2Async(env);

            web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            web.CoreWebView2.Settings.AreDevToolsEnabled = true;

            // Auto-grant permissions dsh plugins rely on; mic/camera stay denied
            web.CoreWebView2.PermissionRequested += (_, e) =>
            {
                if (ShellLogic.IsAutoGrantedPermission(e.PermissionKind))
                    e.State = CoreWebView2PermissionState.Allow;
            };

            // Popup classification: external http(s) → system browser; same-origin
            // popup → lightweight in-shell window (session preserved); blob:/data: → default.
            web.CoreWebView2.NewWindowRequested += async (_, e) =>
            {
                switch (ShellLogic.ClassifyPopup(e.Uri))
                {
                    case ShellLogic.PopupTarget.External:
                        e.Handled = true;
                        try { Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true }); } catch { /* ignore */ }
                        return;
                    case ShellLogic.PopupTarget.Internal:
                    {
                        var deferral = e.GetDeferral();
                        try
                        {
                            var popup = CreatePopupForm();
                            await InitPopupWebViewAsync(popup.Web, env);
                            popup.Web.CoreWebView2.DocumentTitleChanged += (_, _) =>
                            {
                                var title = popup.Web.CoreWebView2.DocumentTitle;
                                if (!string.IsNullOrWhiteSpace(title)) popup.Form.Text = title;
                            };
                            e.NewWindow = popup.Web.CoreWebView2;
                            popup.Form.Show();
                        }
                        catch { /* fall through to default */ }
                        finally { deferral.Complete(); }
                        return;
                    }
                    default:
                        return;
                }
            };

            // Downloads: fixed to the system Downloads folder, same-name auto-rename,
            // blob: gets an extension from MIME; safe extensions auto-open when done.
            web.CoreWebView2.DownloadStarting += (_, e) =>
            {
                try
                {
                    var downloads = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    Directory.CreateDirectory(downloads);
                    var name = ShellLogic.SanitizeFileName(ShellLogic.SuggestDownloadName(
                        e.DownloadOperation.ContentDisposition, e.DownloadOperation.Uri, e.DownloadOperation.MimeType));
                    var path = Path.Combine(downloads, name);
                    for (var i = 1; File.Exists(path); i++)
                        path = Path.Combine(downloads,
                            $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}");
                    e.Handled = true; // suppress the default download dialog
                    e.ResultFilePath = path;
                    if (ShellLogic.IsSafeToAutoOpen(name))
                    {
                        e.DownloadOperation.StateChanged += (_, _) =>
                        {
                            if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo(e.DownloadOperation.ResultFilePath) { UseShellExecute = true });
                                }
                                catch { /* no default app; ignore */ }
                            }
                        };
                    }
                }
                catch { /* fall back to WebView2 default download behavior */ }
            };

            // Renderer crash / unresponsive → auto reload (throttled 10s)
            var lastReloadTick = 0L;
            web.CoreWebView2.ProcessFailed += (_, e) =>
            {
                if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited
                    or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
                {
                    var now = Environment.TickCount64;
                    if (now - lastReloadTick > 10_000)
                    {
                        lastReloadTick = now;
                        try { web.CoreWebView2.Reload(); } catch { /* ignore */ }
                    }
                }
            };

            web.CoreWebView2.Navigate(DefaultUrl);

            // Tray icon: host for session-done notifications, double-click restores window
            var tray = new NotifyIcon
            {
                Icon = icon ?? SystemIcons.Application,
                Text = "DeepSeek Harness",
                Visible = true,
            };
            tray.DoubleClick += (_, _) => ShowAndFocus(form);
            form.FormClosing += (_, _) =>
            {
                try { tray.Visible = false; tray.Dispose(); } catch { /* ignore */ }
            };

            // Session-done notifications: watch <DSH_HOME>/sessions for incremental
            // zstd session logs; balloon tip when a top-level turn ends.
            var sessionsDir = Path.Combine(GetDshHome(), "sessions");
            var watcher = new SessionWatcher(sessionsDir, ev =>
            {
                try
                {
                    tray.BalloonTipTitle = ev.Title;
                    tray.BalloonTipText = ev.Body;
                    tray.ShowBalloonTip(8000);
                }
                catch { /* tray disposed */ }
            });
            watcher.Start();
            form.FormClosing += (_, _) => watcher.Dispose();

            // dsh service watchdog: auto-restart + reload on drop (throttled)
            var vbs = Path.Combine(AppContext.BaseDirectory, "start-dsh.vbs");
            var watchdog = new WatchdogService(Port, vbs, () =>
            {
                try
                {
                    if (!web.IsDisposed && web.CoreWebView2 is not null)
                        web.CoreWebView2.Reload();
                }
                catch { /* window closed */ }
            });
            watchdog.Start();
            form.FormClosing += (_, _) => watchdog.Dispose();
        };

        Application.Run(form);
    }

    private static void ShowAndFocus(Form form)
    {
        try
        {
            if (form.WindowState == FormWindowState.Minimized)
                form.WindowState = FormWindowState.Normal;
            form.Show();
            form.Activate();
            form.TopMost = true;
            form.TopMost = false;
        }
        catch { /* ignore */ }
    }

    /// <summary>Resolve DSH_HOME (env var or ~/.dsh default).</summary>
    private static string GetDshHome()
    {
        var dshHome = Environment.GetEnvironmentVariable("DSH_HOME");
        return string.IsNullOrWhiteSpace(dshHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh")
            : dshHome;
    }

    /// <summary>Lightweight window for same-origin plugin popups, sharing the main
    /// window's WebView2 environment (keeps session/login state).</summary>
    private static (Form Form, WebView2 Web) CreatePopupForm()
    {
        var popupWeb = new WebView2 { Dock = DockStyle.Fill };
        var form = new Form
        {
            Text = "DeepSeek Harness",
            ClientSize = new Size(900, 640),
            StartPosition = FormStartPosition.CenterParent,
            Icon = SystemIcons.Application
        };
        form.Controls.Add(popupWeb);
        form.FormClosing += (_, _) =>
        {
            try { popupWeb.Dispose(); } catch { /* ignore */ }
        };
        return (form, popupWeb);
    }

    /// <summary>Initialize a popup WebView with the same settings as the main window.</summary>
    private static async Task InitPopupWebViewAsync(WebView2 popupWeb, CoreWebView2Environment env)
    {
        await popupWeb.EnsureCoreWebView2Async(env);
        popupWeb.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        popupWeb.CoreWebView2.Settings.AreDevToolsEnabled = true;
        popupWeb.CoreWebView2.PermissionRequested += (_, e) =>
        {
            if (ShellLogic.IsAutoGrantedPermission(e.PermissionKind))
                e.State = CoreWebView2PermissionState.Allow;
        };
        // popups inside popups: external links still go to the system browser
        popupWeb.CoreWebView2.NewWindowRequested += (_, e) =>
        {
            if (ShellLogic.ClassifyPopup(e.Uri) == ShellLogic.PopupTarget.External)
            {
                e.Handled = true;
                try { Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true }); } catch { /* ignore */ }
            }
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? cls, string? title);

    private static bool PortOpen(int port) => WatchdogService.PortOpen(port);
}
