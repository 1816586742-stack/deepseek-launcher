namespace DshLauncher;

/// <summary>
/// About dialog with version info, license, and links.
/// Inspired by Bili23-Downloader about dialog.
/// </summary>
internal class AboutDialog : Form
{
    public AboutDialog()
    {
        Text = "关于 DSH Launcher";
        ClientSize = new Size(480, 380);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        // Title
        var lblTitle = new Label
        {
            Text = "关于 DSH Launcher",
            Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(24, 20),
            AutoSize = true
        };

        // Version info
        var lblVersion = new Label
        {
            Text = $"版本 {UpdateChecker.CurrentVersion}\n基于 .NET 10 和 WebView2 构建",
            Font = new Font("Microsoft YaHei", 10F),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(24, 65),
            Size = new Size(430, 45)
        };

        // License
        var lblLicense = new Label
        {
            Text = "本软件为免费开源软件，使用 MIT 许可证授权。\nCopyright © 2026 dsh-launcher contributors. All Rights Reserved.",
            Font = new Font("Microsoft YaHei", 10F),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(24, 120),
            Size = new Size(430, 50)
        };

        // Sponsorship message
        var lblSponsor = new Label
        {
            Text = "如果这个项目节省了你的时间或解决了你的问题，\n欢迎在 GitHub 上点个 Star 支持开源！",
            Font = new Font("Microsoft YaHei", 10F),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(24, 185),
            Size = new Size(430, 45)
        };

        // Links
        var linkLicense = new LinkLabel
        {
            Text = "📄 使用协议",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(24, 250),
            AutoSize = true
        };
        linkLicense.Click += (_, _) => OpenUrl("https://github.com/1816586742-stack/dsh-launcher-cross/blob/main/LICENSE");

        var linkDocs = new LinkLabel
        {
            Text = "❓ 帮助文档",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(130, 250),
            AutoSize = true
        };
        linkDocs.Click += (_, _) => OpenUrl("https://github.com/1816586742-stack/dsh-launcher-cross/blob/main/README.md");

        var linkGithub = new LinkLabel
        {
            Text = "🐙 GitHub",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(250, 250),
            AutoSize = true
        };
        linkGithub.Click += (_, _) => OpenUrl("https://github.com/1816586742-stack/dsh-launcher-cross");

        var linkStar = new LinkLabel
        {
            Text = "❤️ Star",
            Font = new Font("Microsoft YaHei", 10F),
            Location = new Point(360, 250),
            AutoSize = true
        };
        linkStar.Click += (_, _) => OpenUrl("https://github.com/1816586742-stack/dsh-launcher-cross");

        // Confirm button
        var btnConfirm = new Button
        {
            Text = "确认",
            Location = new Point(150, 310),
            Size = new Size(180, 40),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 180, 236),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold),
            DialogResult = DialogResult.OK
        };
        btnConfirm.FlatAppearance.BorderSize = 0;

        Controls.AddRange([
            lblTitle, lblVersion, lblLicense, lblSponsor,
            linkLicense, linkDocs, linkGithub, linkStar,
            btnConfirm
        ]);
    }

    private static void OpenUrl(string url)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
}
