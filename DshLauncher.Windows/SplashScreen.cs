using System.Drawing.Drawing2D;

namespace DshLauncher;

/// <summary>
/// Startup splash screen: whale logo + "DeepSeek Harness 正在启动..." while
/// the dsh service warms up. The caller transitions to the main form when ready.
/// </summary>
internal sealed class SplashScreen : Form
{
    private readonly System.Windows.Forms.Timer _spinnerTimer;
    private int _angle;

    public SplashScreen()
    {
        Text = "DeepSeek Launcher";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(420, 480);
        BackColor = Color.FromArgb(32, 33, 36);  // dark gray like reference
        ShowInTaskbar = false;
        TopMost = false;

        _spinnerTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _spinnerTimer.Tick += (_, _) => { _angle = (_angle + 12) % 360; Invalidate(); };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _spinnerTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _spinnerTimer.Stop();
        _spinnerTimer.Dispose();
        base.OnFormClosing(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // whale icon
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                using var icon = new Icon(iconPath);
                g.DrawIcon(icon, Width / 2 - 40, 80);
            }
        }
        catch { /* ignore */ }

        // "DeepSeek Harness 正在启动..."
        using var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
        using var titleBrush = new SolidBrush(Color.White);
        var title = "DeepSeek Harness";
        var titleSize = g.MeasureString(title, titleFont);
        g.DrawString(title, titleFont, titleBrush, (Width - titleSize.Width) / 2, 200);

        // spinning arc (loading indicator)
        using var arcPen = new Pen(Color.FromArgb(100, 128, 180, 255), 3f) { EndCap = LineCap.Round };
        var cx = Width / 2;
        var cy = 260;
        g.DrawArc(arcPen, cx - 14, cy - 14, 28, 28, _angle, 90);

        // subtitle
        using var subFont = new Font("Segoe UI", 9.5f);
        using var subBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
        var sub = "正在本机启动 dsh web 服务，首次启动可能需要几十秒。";
        var subSize = g.MeasureString(sub, subFont, Width - 80);
        g.DrawString(sub, subFont, subBrush, (Width - subSize.Width) / 2, 290);

        // "DSH Launcher — 数据目录中的日志可帮助排查启动问题。"
        using var hintFont = new Font("Segoe UI", 8f);
        using var hintBrush = new SolidBrush(Color.FromArgb(120, 128, 140));
        var hint = "DSH Launcher — 数据目录中的日志可帮助排查启动问题。";
        var hintSize = g.MeasureString(hint, hintFont, Width - 60);
        g.DrawString(hint, hintFont, hintBrush, (Width - hintSize.Width) / 2, 320);
    }
}
