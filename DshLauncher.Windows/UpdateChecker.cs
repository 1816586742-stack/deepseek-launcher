using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace DshLauncher;

/// <summary>
/// Update dialog with version comparison and changelog display.
/// Inspired by Bili23-Downloader update UI.
/// </summary>
internal static class UpdateChecker
{
    private const string Repo = "1816586742-stack/dsh-launcher-cross";
    private const string CurrentVersion = "0.2.5";

    /// <summary>
    /// Check for updates and show dialog if available.
    /// Respects "skip this version" setting.
    /// </summary>
    public static async Task CheckAndPromptAsync()
    {
        try
        {
            // Check if this version was skipped
            var skippedVersion = SettingsManager.GetSkippedVersion();
            if (skippedVersion == CurrentVersion)
                return; // User chose to skip this version

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DSH-Launcher");

            var url = $"https://api.github.com/repos/{Repo}/releases/latest";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            var latestTag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var latestVersion = latestTag.TrimStart('v');

            if (CompareVersions(latestVersion, CurrentVersion) <= 0)
                return; // Up to date

            // Parse changelog
            var body = doc.RootElement.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";

            // Show update dialog on UI thread
            var result = false;
            var skipVersion = false;

            var dialog = new UpdateDialog(CurrentVersion, latestVersion, body);
            var dialogResult = dialog.ShowDialog();
            result = dialogResult == System.Windows.Forms.DialogResult.Yes;
            skipVersion = dialog.SkipThisVersion;

            if (skipVersion)
            {
                SettingsManager.SetSkippedVersion(latestVersion);
            }

            if (result)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"https://github.com/{Repo}/releases/latest",
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Network error, ignore silently
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
