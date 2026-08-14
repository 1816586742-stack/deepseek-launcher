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

        // Context menu: Settings / About / Exit
        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings", null, (_, _) => SettingsManager.ShowDialog());
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

            // Open external links in system browser
            web.CoreWebView2.NewWindowRequested += (_, e) =>
            {
                if (!e.Uri.Contains("127.0.0.1"))
                {
                    e.Handled = true;
                    Process.Start(new ProcessStartInfo(e.Uri) { UseShellExecute = true });
                }
            };

            web.CoreWebView2.Navigate(DefaultUrl);
        };

        Application.Run(form);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? cls, string? title);

    private static bool PortOpen(int port)
    {
        try { using var c = new TcpClient(); c.Connect("127.0.0.1", port); return true; }
        catch { return false; }
    }
}
