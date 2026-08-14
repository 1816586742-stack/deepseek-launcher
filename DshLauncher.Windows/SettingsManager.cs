namespace DshLauncher;

/// <summary>
/// Simple settings dialog: port number, auto-start dsh toggle.
/// </summary>
internal static class SettingsManager
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DshLauncher", "settings.json");

    public static int Port { get; set; } = 3080;
    public static bool AutoStartDsh { get; set; } = true;

    static SettingsManager()
    {
        Load();
    }

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("port", out var p))
                    Port = p.GetInt32();
                if (doc.RootElement.TryGetProperty("autoStartDsh", out var a))
                    AutoStartDsh = a.GetBoolean();
            }
        }
        catch { /* use defaults */ }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = $"{{\"port\":{Port},\"autoStartDsh\":{AutoStartDsh.ToString().ToLower()}}}";
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* ignore */ }
    }

    public static void ShowDialog()
    {
        var form = new Form
        {
            Text = "DSH Launcher Settings",
            ClientSize = new Size(350, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };

        var lblPort = new Label { Text = "dsh port:", Location = new Point(20, 25), AutoSize = true };
        var txtPort = new TextBox { Text = Port.ToString(), Location = new Point(120, 22), Width = 100 };

        var chkAuto = new CheckBox
        {
            Text = "Auto-start dsh if not running",
            Location = new Point(20, 60),
            Width = 280,
            Checked = AutoStartDsh
        };

        var btnSave = new Button
        {
            Text = "Save",
            Location = new Point(20, 130),
            Width = 100,
            DialogResult = DialogResult.OK
        };

        var btnCheckUpdate = new Button
        {
            Text = "Check for updates",
            Location = new Point(140, 130),
            Width = 150,
        };

        btnCheckUpdate.Click += async (_, _) =>
        {
            btnCheckUpdate.Enabled = false;
            btnCheckUpdate.Text = "Checking...";
            var hasUpdate = await UpdateChecker.CheckAndPromptAsync();
            btnCheckUpdate.Text = hasUpdate ? "Update available!" : "Up to date";
            btnCheckUpdate.Enabled = true;
        };

        btnSave.Click += (_, _) =>
        {
            if (int.TryParse(txtPort.Text, out var port) && port > 0 && port < 65536)
                Port = port;
            AutoStartDsh = chkAuto.Checked;
            Save();
        };

        form.Controls.AddRange([lblPort, txtPort, chkAuto, btnSave, btnCheckUpdate]);
        form.AcceptButton = btnSave;

        form.ShowDialog();
    }

    /// <summary>
    /// Get a timestamp value from settings.
    /// </summary>
    public static DateTime? GetTimestamp(string key)
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(key, out var val) && val.GetString() is string s)
                    return DateTime.Parse(s);
            }
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Set a timestamp value in settings.
    /// </summary>
    public static void SetTimestamp(string key, DateTime value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var json = File.Exists(SettingsPath) ? File.ReadAllText(SettingsPath) : "{}";
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var obj = doc.RootElement;
            // Rebuild JSON with new key
            var entries = new List<string>();
            foreach (var prop in obj.EnumerateObject())
            {
                if (prop.Name != key)
                    entries.Add($"\"{prop.Name}\":\"{prop.Value}\"");
            }
            entries.Add($"\"{key}\":\"{value:O}\"");
            File.WriteAllText(SettingsPath, "{" + string.Join(",", entries) + "}");
        }
        catch { /* ignore */ }
    }
}
