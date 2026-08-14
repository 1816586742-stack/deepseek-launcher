namespace DshLauncher;

/// <summary>
/// Professional update dialog with version comparison and changelog display.
/// Inspired by Bili23-Downloader update UI.
/// </summary>
internal class UpdateDialog : Form
{
    public bool SkipThisVersion { get; private set; }

    public UpdateDialog(string currentVersion, string latestVersion, string changelog)
    {
        Text = "DSH Launcher — 软件更新";
        ClientSize = new Size(520, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;

        // Header
        var lblTitle = new Label
        {
            Text = "新版本已经发布",
            Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(24, 20),
            AutoSize = true
        };

        var lblVersion = new Label
        {
            Text = $"v{latestVersion} 版本可供下载，你当前使用的版本是 v{currentVersion}。是否现在更新？",
            Font = new Font("Microsoft YaHei", 10F),
            ForeColor = Color.FromArgb(180, 180, 180),
            Location = new Point(24, 60),
            Size = new Size(470, 25)
        };

        // Changelog section
        var lblChangelogTitle = new Label
        {
            Text = "更新内容：",
            Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(24, 100),
            AutoSize = true
        };

        var txtChangelog = new RichTextBox
        {
            Location = new Point(24, 125),
            Size = new Size(470, 280),
            BackColor = Color.FromArgb(45, 45, 45),
            ForeColor = Color.FromArgb(200, 200, 200),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Font = new Font("Microsoft YaHei", 9.5F),
            ScrollBars = RichTextBoxScrollBars.Vertical
        };

        // Parse and format changelog
        FormatChangelog(txtChangelog, changelog);

        // Buttons
        var btnSkip = new Button
        {
            Text = "跳过此版本",
            Location = new Point(24, 420),
            Size = new Size(120, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 50, 50),
            ForeColor = Color.FromArgb(180, 180, 180),
            Font = new Font("Microsoft YaHei", 9.5F),
            DialogResult = DialogResult.No
        };
        btnSkip.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

        var btnUpdate = new Button
        {
            Text = "马上更新",
            Location = new Point(374, 420),
            Size = new Size(120, 38),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(0, 180, 236),
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold),
            DialogResult = DialogResult.Yes
        };
        btnUpdate.FlatAppearance.BorderSize = 0;

        btnSkip.Click += (_, _) => SkipThisVersion = true;

        Controls.AddRange([lblTitle, lblVersion, lblChangelogTitle, txtChangelog, btnSkip, btnUpdate]);
    }

    private void FormatChangelog(RichTextBox rtb, string markdown)
    {
        rtb.Clear();

        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("### ") || trimmed.StartsWith("## "))
            {
                // Section header
                var headerText = trimmed.TrimStart('#').Trim();
                rtb.SelectionFont = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
                rtb.SelectionColor = Color.FromArgb(0, 200, 255);
                rtb.AppendText(headerText + "\n");
            }
            else if (trimmed.StartsWith("- "))
            {
                // List item
                var itemText = trimmed.Substring(2);
                rtb.SelectionFont = new Font("Microsoft YaHei", 9.5F);
                rtb.SelectionColor = Color.FromArgb(200, 200, 200);
                rtb.AppendText("  • " + itemText + "\n");
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                // Regular text
                rtb.SelectionFont = new Font("Microsoft YaHei", 9.5F);
                rtb.SelectionColor = Color.FromArgb(180, 180, 180);
                rtb.AppendText(trimmed + "\n");
            }
        }
    }
}
