using DshLauncher;
using Microsoft.Web.WebView2.Core;
using Xunit;

namespace DshLauncher.Tests;

/// <summary>ShellLogic pure-policy tests: popup classification, permission policy,
/// download name derivation and sanitization.</summary>
public class ShellLogicTests
{
    // ---------- popup classification ----------

    [Theory]
    [InlineData("http://127.0.0.1:3080/foo", ShellLogic.PopupTarget.Internal)]
    [InlineData("http://localhost:3080/foo", ShellLogic.PopupTarget.Internal)]
    [InlineData("https://127.0.0.1:3080/foo", ShellLogic.PopupTarget.Internal)]
    [InlineData("https://github.com/omdsh-dev/dsh-notification", ShellLogic.PopupTarget.External)]
    [InlineData("http://example.com/a?b=1", ShellLogic.PopupTarget.External)]
    [InlineData("blob:http://127.0.0.1:3080/uuid-123", ShellLogic.PopupTarget.Default)]
    [InlineData("data:text/plain,hello", ShellLogic.PopupTarget.Default)]
    [InlineData("about:blank", ShellLogic.PopupTarget.Default)]
    [InlineData("not a uri", ShellLogic.PopupTarget.Default)]
    [InlineData("", ShellLogic.PopupTarget.Default)]
    [InlineData(null, ShellLogic.PopupTarget.Default)]
    public void ClassifyPopup_ReturnsExpected(string? raw, ShellLogic.PopupTarget expected) =>
        Assert.Equal(expected, ShellLogic.ClassifyPopup(raw));

    // ---------- permission policy ----------

    [Theory]
    [InlineData(CoreWebView2PermissionKind.Notifications, true)]
    [InlineData(CoreWebView2PermissionKind.ClipboardRead, true)]
    [InlineData(CoreWebView2PermissionKind.Autoplay, true)]
    [InlineData(CoreWebView2PermissionKind.MultipleAutomaticDownloads, true)]
    [InlineData(CoreWebView2PermissionKind.PersistentStorage, true)]
    [InlineData(CoreWebView2PermissionKind.Microphone, false)]
    [InlineData(CoreWebView2PermissionKind.Camera, false)]
    [InlineData(CoreWebView2PermissionKind.Geolocation, false)]
    [InlineData(CoreWebView2PermissionKind.UnknownPermission, false)]
    public void IsAutoGrantedPermission_MatchesPolicy(CoreWebView2PermissionKind kind, bool expected) =>
        Assert.Equal(expected, ShellLogic.IsAutoGrantedPermission(kind));

    // ---------- download name derivation ----------

    [Theory]
    [InlineData("attachment; filename=report.pdf", null, null, "report.pdf")]
    [InlineData("attachment; filename=\"my file.txt\"", null, null, "my file.txt")]
    [InlineData("attachment; filename*=UTF-8''%E6%B5%8B%E8%AF%95.txt", null, null, "测试.txt")] // RFC 5987 Chinese decode
    [InlineData(null, "https://example.com/a/b/archive.tar.gz", null, "archive.tar.gz")]
    [InlineData(null, "http://127.0.0.1:3080/api/export?fmt=json", null, "export")] // URI tail, query stripped
    [InlineData("attachment; filename=dup.txt", "https://example.com/other.bin", null, "dup.txt")] // disposition wins
    public void SuggestDownloadName_CoreCases(string? disposition, string? uri, string? mime, string expected) =>
        Assert.Equal(expected, ShellLogic.SuggestDownloadName(disposition, uri, mime));

    [Theory]
    [InlineData("blob:http://127.0.0.1:3080/abc", "application/zip", ".zip")]
    [InlineData("blob:http://127.0.0.1:3080/abc", "image/png", ".png")]
    [InlineData("blob:http://127.0.0.1:3080/abc", "application/octet-stream", null)]   // unknown MIME, no extension
    [InlineData("blob:http://127.0.0.1:3080/abc", "text/plain; charset=utf-8", ".txt")] // MIME with charset
    [InlineData("data:text/plain,hello", "text/plain", ".txt")]
    public void SuggestDownloadName_BlobMimeFallback(string? uri, string? mime, string? expectedExt)
    {
        var name = ShellLogic.SuggestDownloadName(null, uri, mime);
        Assert.StartsWith("dsh-", name);
        if (expectedExt is null)
            Assert.DoesNotContain(".", name[(name.IndexOf('-') + 1)..]); // timestamp name has no dot
        else
            Assert.EndsWith(expectedExt, name);
    }

    // ---------- file name sanitization ----------

    [Theory]
    [InlineData("a<b>c:d|e?f*g", "a_b_c_d_e_f_g")]
    [InlineData("  spaced  ", "spaced")]
    [InlineData("trailing.", "trailing")]
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("NUL", "_NUL")]
    [InlineData("COM1", "_COM1")]
    [InlineData("LPT9.json", "_LPT9.json")]
    [InlineData("normal-name.json", "normal-name.json")]
    [InlineData("中文名.txt", "中文名.txt")]
    public void SanitizeFileName_HandlesEdgeCases(string input, string expected) =>
        Assert.Equal(expected, ShellLogic.SanitizeFileName(input));

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("...", true)]
    public void SanitizeFileName_EmptyFallsBackToTimestamp(string input, bool _) =>
        Assert.StartsWith("dsh-", ShellLogic.SanitizeFileName(input));

    // ---------- auto-open safety ----------

    [Theory]
    [InlineData("report.pdf", true)]
    [InlineData("photo.png", true)]
    [InlineData("song.mp3", true)]
    [InlineData("archive.zip", true)]
    [InlineData("page.html", false)]   // executable code surface, never auto-open
    [InlineData("image.svg", false)]
    [InlineData("installer.exe", false)]
    [InlineData("script.hta", false)]
    [InlineData("data.unknown", false)]
    public void IsSafeToAutoOpen_MatchesPolicy(string name, bool expected) =>
        Assert.Equal(expected, ShellLogic.IsSafeToAutoOpen(name));
}
