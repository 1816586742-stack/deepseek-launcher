using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace DshLauncher;

/// <summary>
/// Checks GitHub Releases for updates and prompts user to download.
/// </summary>
internal static class UpdateChecker
{
    private const string Repo = "1816586742-stack/dsh-launcher-cross";
    private const string CurrentVersion = "0.2.0";
    private const string LastCheckKey = "LastUpdateCheck";

    /// <summary>
    /// Check for updates in background. Only checks once per day.
    /// </summary>
    public static async Task<bool> CheckAndPromptAsync()
    {
        try
        {
            // Only check once per day
            var lastCheck = SettingsManager.GetTimestamp(LastCheckKey);
            if (lastCheck.HasValue && (DateTime.Now - lastCheck.Value).TotalDays < 1)
                return false;

            SettingsManager.SetTimestamp(LastCheckKey, DateTime.Now);

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DSH-Launcher");

            var url = $"https://api.github.com/repos/{Repo}/releases/latest";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var latestVersion = latestTag.TrimStart('v');

            if (CompareVersions(latestVersion, CurrentVersion) <= 0)
                return false; // Up to date

            // Ask user
            var body = doc.RootElement.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            var result = MessageBox.Show(
                $"New version available: v{latestVersion}\n\n{body}\n\nOpen download page?",
                "DSH Launcher — Update Available",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://github.com/{Repo}/releases/latest",
                    UseShellExecute = true
                });
            }

            return true;
        }
        catch
        {
            return false; // Network error, ignore
        }
    }

    private static int CompareVersions(string a, string b)
    {
        var pa = a.Split('.').Select(int.Parse).ToArray();
        var pb = b.Split('.').Select(int.Parse).ToArray();
        for (int i = 0; i < Math.Max(pa.Length, pb.Length); i++)
        {
            var va = i < pa.Length ? pa[i] : 0;
            var vb = i < pb.Length ? pb[i] : 0;
            if (va != vb) return va.CompareTo(vb);
        }
        return 0;
    }
}
