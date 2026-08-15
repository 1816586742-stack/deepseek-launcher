using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Web.WebView2.Core;

namespace DshLauncher;

/// <summary>
/// Pure shell policy logic: popup target classification, permission policy,
/// download filename derivation and sanitization. Kept free of UI wiring so
/// it can be unit tested directly.
/// </summary>
public static class ShellLogic
{
    /// <summary>Where a popup/new-window request should go.</summary>
    public enum PopupTarget
    {
        /// <summary>Keep WebView2 default behavior (blob:/data:/about: etc.).</summary>
        Default,
        /// <summary>External http(s) link → system default browser.</summary>
        External,
        /// <summary>Same-origin http(s) popup → lightweight in-shell window.</summary>
        Internal,
    }

    /// <summary>Windows reserved device names (invalid as file names with any extension).</summary>
    private static readonly string[] ReservedNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>MIME → extension fallback for blob: downloads without a file name.</summary>
    private static readonly Dictionary<string, string> MimeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["text/plain"] = ".txt",
        ["text/markdown"] = ".md",
        ["text/html"] = ".html",
        ["text/csv"] = ".csv",
        ["application/json"] = ".json",
        ["application/pdf"] = ".pdf",
        ["application/zip"] = ".zip",
        ["application/x-zip-compressed"] = ".zip",
        ["application/gzip"] = ".gz",
        ["application/x-tar"] = ".tar",
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp",
        ["image/svg+xml"] = ".svg",
        ["audio/mpeg"] = ".mp3",
        ["audio/wav"] = ".wav",
        ["video/mp4"] = ".mp4",
    };

    /// <summary>Extensions that are safe to auto-open after download (data files only,
    /// never executable code surfaces like .html/.svg/.exe).</summary>
    private static readonly HashSet<string> AutoOpenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".json", ".pdf", ".zip", ".gz", ".tar",
        ".png", ".jpg", ".jpeg", ".gif", ".webp", ".mp3", ".wav", ".mp4",
    };

    /// <summary>Permissions granted automatically (plugin/dsh dependencies); everything
    /// else (mic, camera, geolocation...) stays at the default deny.</summary>
    public static bool IsAutoGrantedPermission(CoreWebView2PermissionKind kind) =>
        kind is CoreWebView2PermissionKind.Notifications
            or CoreWebView2PermissionKind.ClipboardRead
            or CoreWebView2PermissionKind.Autoplay
            or CoreWebView2PermissionKind.MultipleAutomaticDownloads
            or CoreWebView2PermissionKind.PersistentStorage;

    /// <summary>Classify a popup URL: same-origin → in-shell window, external http(s) →
    /// system browser, everything else (blob:/data:/about:) → default behavior.</summary>
    public static PopupTarget ClassifyPopup(string? rawUri)
    {
        if (!Uri.TryCreate(rawUri, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
            return PopupTarget.Default;
        return uri.Host is "127.0.0.1" or "localhost" ? PopupTarget.Internal : PopupTarget.External;
    }

    /// <summary>
    /// Derive a suggested download file name from Content-Disposition / URI / MIME.
    /// blob:/data: URIs have meaningless random tails, so they fall through to the
    /// timestamp + MIME-extension fallback.
    /// </summary>
    public static string SuggestDownloadName(string? disposition, string? downloadUri, string? mimeType)
    {
        string? name = null;
        if (!string.IsNullOrWhiteSpace(disposition))
        {
            var m = Regex.Match(disposition, @"filename\*?=(?:UTF-8'')?[""']?(?<name>[^""';]+)");
            if (m.Success && !string.IsNullOrWhiteSpace(m.Groups["name"].Value))
                name = m.Groups["name"].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(downloadUri)
            && Uri.TryCreate(downloadUri, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            var segment = Path.GetFileName(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(segment))
                name = segment;
        }
        name = string.IsNullOrWhiteSpace(name)
            ? $"dsh-{DateTime.Now:yyyyMMddHHmmss}"
            : Uri.UnescapeDataString(name);

        // blob: etc. without an extension: append one from the MIME type for recognizability
        if (!Path.HasExtension(name) && !string.IsNullOrWhiteSpace(mimeType)
            && MimeExtensions.TryGetValue(mimeType.Split(';')[0].Trim(), out var ext))
            name += ext;
        return name;
    }

    /// <summary>Sanitize a file name: invalid chars → _, trailing dots/spaces stripped,
    /// Windows reserved device names prefixed with underscore.</summary>
    public static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(invalid.Contains(c) ? '_' : c);
        var result = sb.ToString().Trim().TrimEnd('.', ' ');
        if (result.Length == 0)
            return $"dsh-{DateTime.Now:yyyyMMddHHmmss}";
        var stem = Path.GetFileNameWithoutExtension(result).ToUpperInvariant();
        if (Array.IndexOf(ReservedNames, stem) >= 0)
            result = "_" + result;
        return result;
    }

    /// <summary>Whether a downloaded file should be auto-opened with its default app.</summary>
    public static bool IsSafeToAutoOpen(string fileName) =>
        AutoOpenExtensions.Contains(Path.GetExtension(fileName));
}
