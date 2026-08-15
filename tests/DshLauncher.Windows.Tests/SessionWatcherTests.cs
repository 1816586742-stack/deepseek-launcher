using System.Text;
using DshLauncher;
using Xunit;
using ZstdSharp;

namespace DshLauncher.Tests;

/// <summary>SessionWatcher integration tests: real zstd session logs written to
/// a temp directory, verifying baseline, incremental parsing, turn/end
/// notification semantics and subagent filtering.</summary>
public class SessionWatcherTests : IDisposable
{
    private readonly string _root;
    private readonly string _sessions;
    private readonly List<TurnEndEvent> _events = new();
    private SessionWatcher? _watcher;

    public SessionWatcherTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "dsh-watcher-test-" + Guid.NewGuid().ToString("N")[..8]);
        _sessions = Path.Combine(_root, "sessions");
        Directory.CreateDirectory(_sessions);
        _watcher = new SessionWatcher(_sessions, _events.Add);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private static byte[] Compress(string text)
    {
        using var c = new Compressor();
        return c.Wrap(Encoding.UTF8.GetBytes(text)).ToArray();
    }

    private static void AppendFrame(string file, string text)
    {
        using var fs = File.Open(file, FileMode.Append);
        fs.Write(Compress(text));
    }

    /// <summary>Create a session dir, write the first frame (header + optional rows).</summary>
    private string WriteLog(string dirName, string header, params string[] rows)
    {
        var dir = Path.Combine(_sessions, dirName);
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, "session.jsonl.zstd");
        AppendFrame(file, header + "\n" + string.Join("\n", rows) + "\n");
        return file;
    }

    [Fact]
    public void Baseline_ThenIncrementalTurnEnd_FiresNotification()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":0,\"cwd\":\"/home/user/proj\"}";
        var file = WriteLog("s1", header, "{\"type\":\"session/title\",\"data\":{\"title\":\"修复登录 bug\"}}");

        // first scan: baseline, reads header/title only, no notification
        _watcher!.Scan();
        Assert.Empty(_events);

        // incremental: append one full turn
        AppendFrame(file,
            "{\"type\":\"turn/start\"}\n" +
            "{\"type\":\"assistant/message\"}\n" +
            "{\"type\":\"turn/end\"}");
        _watcher.Scan();

        var ev = Assert.Single(_events);
        Assert.Equal("修复登录 bug", ev.Title);   // title from session/title
        Assert.Contains("proj", ev.Body);          // cwd tail segment
        Assert.Equal("sess_0123456789abcdef", ev.SessionId);
        Assert.Equal("/home/user/proj", ev.Cwd);
    }

    [Fact]
    public void NoTurnEvents_FallsBackToAssistantMessages()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_abcdefghijkl\",\"delegationDepth\":0}";
        var file = WriteLog("s2", header);
        _watcher!.Scan(); // baseline

        AppendFrame(file, "{\"type\":\"assistant/message\"}");
        _watcher.Scan();

        var ev = Assert.Single(_events);
        Assert.Equal("DSH 任务完成", ev.Title); // fallback title without session/title
    }

    [Fact]
    public void SubagentLogs_AreIgnored()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":1,\"cwd\":\"/x/y\"}";
        var file = WriteLog("s3", header);
        _watcher!.Scan(); // baseline

        AppendFrame(file, "{\"type\":\"turn/end\"}");
        _watcher.Scan();

        Assert.Empty(_events); // subagent turns don't notify
    }

    [Fact]
    public void MultipleTurnEnds_CountInBody()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":0}";
        var file = WriteLog("s4", header);
        _watcher!.Scan(); // baseline

        AppendFrame(file,
            "{\"type\":\"turn/start\"}\n{\"type\":\"turn/end\"}\n" +
            "{\"type\":\"turn/start\"}\n{\"type\":\"turn/end\"}");
        _watcher.Scan();

        var ev = Assert.Single(_events);
        Assert.Contains("2 轮任务完成", ev.Body);
    }

    [Fact]
    public void TruncatedAndRewritten_FileRebaselines()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":0}";
        var file = WriteLog("s5", header);
        _watcher!.Scan(); // baseline
        AppendFrame(file, "{\"type\":\"turn/end\"}");
        _watcher.Scan();
        Assert.Single(_events);
        _events.Clear();

        // simulate a repair script rewriting the file: clear, new header (rebaseline)
        File.WriteAllText(file, string.Empty);
        AppendFrame(file, "{\"type\":\"session\",\"id\":\"sess_99999999999999\",\"delegationDepth\":0}");
        _watcher.Scan(); // rebaseline

        AppendFrame(file, "{\"type\":\"turn/end\"}");
        _watcher.Scan();

        Assert.Single(_events); // new events still fire after rewrite
        Assert.Equal("sess_99999999999999", _events[0].SessionId);
    }

    [Fact]
    public void UnchangedFile_NoDuplicateNotifications()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":0}";
        var file = WriteLog("s6", header);
        _watcher!.Scan(); // baseline

        AppendFrame(file, "{\"type\":\"turn/end\"}");
        _watcher.Scan();
        Assert.Single(_events);

        _watcher.Scan(); // no new bytes
        _watcher.Scan();
        Assert.Single(_events); // no duplicates
    }

    [Fact]
    public void SessionTitle_AppearsLaterInLog_IsPickedUp()
    {
        var header = "{\"type\":\"session\",\"id\":\"sess_0123456789abcdef\",\"delegationDepth\":0}";
        var file = WriteLog("s7", header);
        _watcher!.Scan(); // baseline

        // title written after the first turn (delayed title generation)
        AppendFrame(file, "{\"type\":\"session/title\",\"data\":{\"title\":\"延迟的标题\"}}\n{\"type\":\"turn/end\"}");
        _watcher.Scan();

        var ev = Assert.Single(_events);
        Assert.Equal("延迟的标题", ev.Title);
    }
}
