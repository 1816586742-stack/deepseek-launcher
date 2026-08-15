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
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // --- Splash screen (shown while dsh warms up) ---
        var splash = new SplashScreen();
        var splashShown = false;
        var splashFinished = false;

        if (!PortOpen(Port))
        {
            splash.Show();
            splashShown = true;

            // Keep splash responsive while polling (max 90s)
            var deadline = Environment.TickCount + 90_000;
            while (!PortOpen(Port) && Environment.TickCount < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(500);
            }

            splashFinished = true;
        }

        if (!PortOpen(Port))
        {
            if (splashShown) splash.Dispose();
            MessageBox.Show("dsh service not available. Check: %USERPROFILE%\\.dsh",
                "DSH Launcher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // --- Main form ---
        Icon? icon = null;
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(iconPath)) icon = new Icon(iconPath);
        }
        catch { /* ignore */ }

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

        // Context menu: full feature set
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => SettingsManager.ShowDialog());
        menu.Items.Add("Balance", null, async (_, _) =>
        {
            try
            {
                var result = await BalanceService.QueryBalanceAsync(GetDshHome());
                MessageBox.Show(BalanceService.FormatForNotification(result), "DeepSeek Balance",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { /* ignore */ }
        });
        menu.Items.Add("About", null, (_, _) => new AboutDialog().ShowDialog());
        menu.Items.Add("-");
        var reloadItem = menu.Items.Add("Reload", null, (_, _) =>
        {
            try { if (web.CoreWebView2 is not null) web.CoreWebView2.Reload(); } catch { }
        }) as ToolStripMenuItem;
        if (reloadItem is not null) reloadItem.ShortcutKeys = Keys.Control | Keys.R;

        var devToolsItem = menu.Items.Add("DevTools", null, (_, _) =>
        {
            try { web.CoreWebView2?.OpenDevToolsWindow(); } catch { }
        }) as ToolStripMenuItem;
        if (devToolsItem is not null) devToolsItem.ShortcutKeys = Keys.F12;

        var fullScreenItem = menu.Items.Add("Fullscreen", null, (_, _) =>
        {
            form.WindowState = form.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }) as ToolStripMenuItem;
        if (fullScreenItem is not null) fullScreenItem.ShortcutKeys = Keys.F11;
        menu.Items.Add("Open in Browser", null, (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(DefaultUrl) { UseShellExecute = true }); } catch { }
        });
        menu.Items.Add("Open Log Dir", null, (_, _) =>
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            try { Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true }); } catch { }
        });
        menu.Items.Add("-");
        menu.Items.Add("Exit", null, (_, _) => { web.Dispose(); Application.Exit(); });
        form.ContextMenuStrip = menu;

        // Tray icon (declared before FormClosing so it can be referenced)
        var trayIcon = new NotifyIcon
        {
            Icon = icon ?? SystemIcons.Application,
            Visible = true,
            Text = "DeepSeek Launcher",
        };
        trayIcon.DoubleClick += (_, _) => { form.Show(); form.WindowState = FormWindowState.Normal; form.Activate(); };
        form.FormClosed += (_, _) =>
        {
            try { trayIcon.Visible = false; trayIcon.Dispose(); } catch { }
            if (icon is not null) { try { icon.Dispose(); } catch { } }
        };

        // Close → minimize to tray (only "Exit" from menu actually quits)
        form.FormClosing += (sender, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                form.Hide();
                trayIcon.Text = "DeepSeek Launcher — 双击恢复";
                trayIcon.Visible = true;
            }
        };

        // Close splash in form.Load handler, then let Application.Run show the form
        form.Load += async (_, _) =>
        {
            // Close splash screen now that WebView2 is about to initialize
            if (splashShown)
            {
                try { splash.Close(); } catch { try { splash.Dispose(); } catch { } }
                splashShown = false;
            }
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

            // Popup classification: external → browser, internal → in-shell window, default for blob/data
            web.CoreWebView2.NewWindowRequested += async (_, e) =>
            {
                switch (ShellLogic.ClassifyPopup(e.Uri))
                {
                    case ShellLogic.PopupTarget.External:
                        e.Handled = true;
                        try { Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true }); } catch { }
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
                        catch { }
                        finally { deferral.Complete(); }
                        return;
                    }
                    default: return;
                }
            };

            // Downloads: save to Downloads, avoid duplicate names, auto-open safe extensions
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
                        path = Path.Combine(downloads, $"{Path.GetFileNameWithoutExtension(name)} ({i}){Path.GetExtension(name)}");
                    e.Handled = true;
                    e.ResultFilePath = path;
                    if (ShellLogic.IsSafeToAutoOpen(name))
                    {
                        e.DownloadOperation.StateChanged += (_, _) =>
                        {
                            if (e.DownloadOperation.State == CoreWebView2DownloadState.Completed)
                                try { Process.Start(new ProcessStartInfo(e.DownloadOperation.ResultFilePath) { UseShellExecute = true }); } catch { }
                        };
                    }
                }
                catch { }
            };

            // Renderer crash / unresponsive → auto reload (throttled 10s)
            var lastReloadTick = 0L;
            web.CoreWebView2.ProcessFailed += (_, e) =>
            {
                if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited
                    or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
                {
                    var now = Environment.TickCount64;
                    if (now - lastReloadTick > 10_000) { lastReloadTick = now; try { web.CoreWebView2.Reload(); } catch { } }
                }
            };

            web.CoreWebView2.Navigate(DefaultUrl);

            // Session-done notifications
            var sessionsDir = Path.Combine(GetDshHome(), "sessions");
            var watcher = new SessionWatcher(sessionsDir, ev =>
            {
                try { trayIcon.BalloonTipTitle = ev.Title; trayIcon.BalloonTipText = ev.Body; trayIcon.ShowBalloonTip(8000); }
                catch { }
            });
            watcher.Start();
            form.FormClosing += (_, _) => watcher.Dispose();

            // dsh service watchdog
            var vbs = Path.Combine(AppContext.BaseDirectory, "start-dsh.vbs");
            var watchdog = new WatchdogService(Port, vbs, () =>
            {
                try { if (!web.IsDisposed && web.CoreWebView2 is not null) web.CoreWebView2.Reload(); } catch { }
            });
            watchdog.Start();
            form.FormClosing += (_, _) => watchdog.Dispose();

            // Check for updates in background
            _ = Task.Run(async () => await UpdateChecker.CheckAndPromptAsync());
        };

        Application.Run(form);
    }

    private static string GetDshHome()
    {
        var dshHome = Environment.GetEnvironmentVariable("DSH_HOME");
        return string.IsNullOrWhiteSpace(dshHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh")
            : dshHome;
    }

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
        form.FormClosing += (_, _) => { try { popupWeb.Dispose(); } catch { } };
        return (form, popupWeb);
    }

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
        popupWeb.CoreWebView2.NewWindowRequested += (_, e) =>
        {
            if (ShellLogic.ClassifyPopup(e.Uri) == ShellLogic.PopupTarget.External)
            {
                e.Handled = true;
                try { Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true }); } catch { }
            }
        };
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? cls, string? title);

    private static bool PortOpen(int port) => WatchdogService.PortOpen(port);
}
