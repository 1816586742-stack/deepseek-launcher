using System.Text.Json;

namespace DshLauncher;

/// <summary>Event raised when a top-level dsh session finishes an agent turn.</summary>
public sealed record TurnEndEvent(string Title, string Body, string? SessionId, string? Cwd);

/// <summary>
/// Watches <c>session.jsonl.zstd</c> logs under the dsh sessions directory and
/// raises <see cref="TurnEnd"/> when a top-level session's agent turn ends.
/// Incremental reads only (tail after last consumed offset); the first scan
/// establishes a baseline from the header frame only, and the directory
/// enumeration result is cached for 5 seconds to avoid desktop stalls.
/// </summary>
public sealed class SessionWatcher : IDisposable
{
    private readonly string _sessionsDir;
    private readonly Action<TurnEndEvent> _onTurnEnd;
    private readonly Action<string, string> _log;
    private readonly Dictionary<string, FileRec> _files = new();
    private (long At, List<string> Files) _dirCache;
    private System.Threading.Timer? _timer;
    private bool _disposed;

    private sealed class FileRec
    {
        public long Size;
        public long Consumed;
        public JsonElement? Header;
        public string? Title;
        public bool Baseline;
        public bool HasTurnEvents;
    }

    public SessionWatcher(string sessionsDir, Action<TurnEndEvent> onTurnEnd, Action<string, string>? log = null)
    {
        _sessionsDir = sessionsDir;
        _onTurnEnd = onTurnEnd;
        _log = log ?? ((_, _) => { });
    }

    /// <summary>Start polling (default 3s); the first scan is deferred one beat
    /// and processed in batches so startup is not blocked by a full decode.</summary>
    public void Start(int intervalMs = 3000)
    {
        ThreadPool.QueueUserWorkItem(_ => Scan(4));
        _timer = new System.Threading.Timer(_ => Scan(), null, intervalMs, intervalMs);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>List all session.jsonl.zstd logs (directory enumeration cached 5s).</summary>
    public List<string> ListLogs()
    {
        var now = Environment.TickCount64;
        if (now - _dirCache.At < 5000) return _dirCache.Files;
        var outFiles = new List<string>();
        if (Directory.Exists(_sessionsDir))
            Walk(_sessionsDir, outFiles);
        _dirCache = (now, outFiles);
        return outFiles;
    }

    private static void Walk(string dir, List<string> outFiles)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(dir))
        {
            try
            {
                var attrs = File.GetAttributes(entry);
                if ((attrs & FileAttributes.Directory) != 0) Walk(entry, outFiles);
                else if (Path.GetFileName(entry) == "session.jsonl.zstd") outFiles.Add(entry);
            }
            catch { /* skip inaccessible entries */ }
        }
    }

    /// <summary>Scan one round; returns whether any file grew. Public for tests.</summary>
    public bool Scan(int maxChanged = int.MaxValue)
    {
        bool any = false;
        int changed = 0;
        foreach (var file in ListLogs())
        {
            try
            {
                if (Process(file))
                {
                    any = true;
                    if (++changed >= maxChanged) break;
                }
            }
            catch (Exception err) { _log("watch", $"处理失败 {file}: {err.Message}"); }
        }
        return any;
    }

    /// <summary>Incrementally process one log file; returns whether it grew.</summary>
    public bool Process(string file)
    {
        long size;
        try { size = new FileInfo(file).Length; }
        catch { _files.Remove(file); return false; }

        if (!_files.TryGetValue(file, out var rec))
        {
            rec = new FileRec();
            _files[file] = rec;
        }

        // Truncated/rewritten file (e.g. a repair script) → rebaseline.
        // Must come before the "no new bytes" check: a rewritten file can be
        // smaller than the old consumed offset without being truly unchanged.
        if (size < rec.Consumed)
        {
            rec.Consumed = 0; rec.Header = null; rec.Title = null; rec.Baseline = false; rec.HasTurnEvents = false;
        }
        if (size <= rec.Consumed && rec.Baseline) return false; // no new bytes

        bool first = !rec.Baseline;
        long readFrom = rec.Consumed;
        byte[] tail;
        try { tail = ReadTail(file, readFrom, size); }
        catch { return false; }

        // Tail not on a frame boundary (rewritten/concatenated oddly) → rebaseline.
        if (!first && tail.Length >= 4 && BitConverter.ToUInt32(tail, 0) != ZstdFrames.Magic)
        {
            rec.Consumed = 0; rec.Header = null; rec.Title = null; rec.Baseline = false; rec.HasTurnEvents = false;
            return Process(file);
        }

        var frames = ZstdFrames.Scan(tail, out _);

        // First sight of this session (baseline): parse header and title from the
        // first frame only; historical events never trigger notifications anyway,
        // and skipping the full decode avoids startup stalls.
        if (first)
        {
            if (frames.Count > 0)
            {
                try
                {
                    var text = ZstdFrames.Decode(tail.AsSpan(frames[0].Start, frames[0].End - frames[0].Start));
                    foreach (var line in text.Split('\n'))
                    {
                        if (line.Length == 0) continue;
                        foreach (var ev in ZstdFrames.ExpandRow(line))
                        {
                            if (ev.ValueKind != JsonValueKind.Object) continue;
                            var type = ev.GetPropertyOrNull("type")?.GetString();
                            if (type == "session" && rec.Header is null)
                                rec.Header = ev.Clone();
                            if (type == "session/title" && rec.Title is null)
                                rec.Title = ev.GetPropertyOrNull("data")?.GetPropertyOrNull("title")?.GetString();
                        }
                    }
                }
                catch { /* retry next round if the header is corrupted */ }
                rec.Consumed = readFrom + frames[^1].End;
            }
            rec.Baseline = true;
            rec.Size = size;
            return true; // counts as "heavy work" for batch limiting
        }

        // Incremental: decode only complete frames after the consumed offset.
        int turnEnds = 0, assistantMessages = 0;
        long consumed = readFrom;
        foreach (var (start, end) in frames)
        {
            string text;
            try { text = ZstdFrames.Decode(tail.AsSpan(start, end - start)); }
            catch { break; }
            foreach (var line in text.Split('\n'))
            {
                if (line.Length == 0) continue;
                foreach (var ev in ZstdFrames.ExpandRow(line))
                {
                    if (ev.ValueKind != JsonValueKind.Object) continue;
                    var type = ev.GetPropertyOrNull("type")?.GetString();
                    if (type == "session/title")
                        rec.Title = ev.GetPropertyOrNull("data")?.GetPropertyOrNull("title")?.GetString();
                    if (type is "turn/start" or "turn/end") rec.HasTurnEvents = true;
                    if (type == "turn/end") turnEnds++;
                    if (type == "assistant/message") assistantMessages++;
                }
            }
            consumed = readFrom + end;
        }
        rec.Consumed = consumed;
        rec.Size = size;

        // Notification semantics: count turn/end once turn events exist,
        // otherwise fall back to assistant/message.
        int count = rec.HasTurnEvents ? turnEnds : assistantMessages;
        if (count > 0) Emit(rec, count);
        return count > 0 || consumed > readFrom;
    }

    private void Emit(FileRec rec, int count)
    {
        var h = rec.Header;
        // subagent logs are noise for notifications
        if (h is { ValueKind: JsonValueKind.Object } header
            && header.GetPropertyOrNull("delegationDepth") is { } depth
            && depth.ValueKind == JsonValueKind.Number
            && depth.GetInt64() > 0) return;

        string title = "DSH 任务完成";
        if (!string.IsNullOrWhiteSpace(rec.Title)) title = rec.Title!;

        var cwdBase = h?.GetPropertyOrNull("cwd")?.GetString() is string cwd && cwd.Length > 0
            ? Path.GetFileName(cwd.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            : null;
        var shortId = h?.GetPropertyOrNull("id")?.GetString() is string id && id.Length > 0
            ? "会话 " + id[^Math.Min(8, id.Length)..]
            : null;
        var body = string.Join(" · ", new[] { cwdBase, shortId }.Where(s => s is not null));
        if (count > 1) body += $"（{count} 轮任务完成）";

        try { _onTurnEnd(new TurnEndEvent(title, body, h?.GetPropertyOrNull("id")?.GetString(), h?.GetPropertyOrNull("cwd")?.GetString())); }
        catch (Exception err) { _log("watch", "onTurnEnd 回调异常: " + err.Message); }
    }

    /// <summary>Read the tail bytes of a file from offset (incremental read).</summary>
    private static byte[] ReadTail(string file, long offset, long size)
    {
        int len = (int)(size - offset);
        var tail = new byte[len];
        using var fs = File.OpenRead(file);
        fs.Position = offset;
        int pos = 0;
        while (pos < len)
        {
            int n = fs.Read(tail, pos, len - pos);
            if (n <= 0) break;
            pos += n;
        }
        if (pos < len) Array.Resize(ref tail, pos);
        return tail;
    }
}
