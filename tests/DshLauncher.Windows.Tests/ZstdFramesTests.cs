using System.Text;
using DshLauncher;
using Xunit;
using ZstdSharp;

namespace DshLauncher.Tests;

/// <summary>zstd frame scanner tests: round-trip through ZstdSharp.Port's
/// Compressor, matching the dsh persistence layout (concatenated frames).</summary>
public class ZstdFramesTests
{
    private static byte[] Compress(string text)
    {
        using var c = new Compressor();
        return c.Wrap(Encoding.UTF8.GetBytes(text)).ToArray();
    }

    [Fact]
    public void Scan_SingleFrame_RoundTrips()
    {
        var frame = Compress("{\"type\":\"session\",\"id\":\"x\"}\n");
        var frames = ZstdFrames.Scan(frame, out var torn);
        Assert.Single(frames);
        Assert.Equal(0, frames[0].Start);
        Assert.Equal(frame.Length, frames[0].End);
        Assert.Equal(frame.Length, torn);
        Assert.Equal("{\"type\":\"session\",\"id\":\"x\"}\n", ZstdFrames.Decode(frame.AsSpan()));
    }

    [Fact]
    public void Scan_MultipleFrames_FindsAll()
    {
        var f1 = Compress("{\"a\":1}\n");
        var f2 = Compress("{\"a\":2}\n");
        var f3 = Compress("{\"a\":3}\n");
        var all = f1.Concat(f2).Concat(f3).ToArray();
        var frames = ZstdFrames.Scan(all, out var torn);
        Assert.Equal(3, frames.Count);
        Assert.Equal(0, frames[0].Start);
        Assert.Equal(f1.Length, frames[0].End);
        Assert.Equal(f1.Length, frames[1].Start);
        Assert.Equal(f1.Length + f2.Length, frames[1].End);
        Assert.Equal(all.Length, frames[2].End);
        Assert.Equal(all.Length, torn);
    }

    [Fact]
    public void Scan_TornFrame_StopsAtTornStart()
    {
        var f1 = Compress("{\"a\":1}\n");
        var f2 = Compress("{\"a\":2}\n");
        var all = f1.Concat(f2).ToArray();
        var tornTail = all.Take(all.Length - 3).ToArray(); // cut 3 bytes off f2's tail
        var frames = ZstdFrames.Scan(tornTail, out var torn);
        Assert.Single(frames);
        Assert.Equal(f1.Length, torn); // second frame incomplete → tornStart at its start
    }

    [Fact]
    public void Scan_GarbageBeforeMagic_StopsImmediately()
    {
        var junk = Encoding.UTF8.GetBytes("garbage...").Concat(Compress("{\"a\":1}\n")).ToArray();
        var frames = ZstdFrames.Scan(junk, out var torn);
        Assert.Empty(frames);
        Assert.Equal(0, torn);
    }

    [Fact]
    public void Scan_EmptyInput_NoFrames()
    {
        var frames = ZstdFrames.Scan(Array.Empty<byte>(), out var torn);
        Assert.Empty(frames);
        Assert.Equal(0, torn);
    }

    [Fact]
    public void ExpandRow_TextChunks_ExpandsTexts()
    {
        var events = ZstdFrames.ExpandRow("{\"type\":\"text-chunks\",\"data\":{\"texts\":[\"a\",\"b\"]}}");
        Assert.Equal(2, events.Count);
        Assert.Equal("a", events[0].GetString());
        Assert.Equal("b", events[1].GetString());
    }

    [Fact]
    public void ExpandRow_PlainRow_ReturnsSelf()
    {
        var events = ZstdFrames.ExpandRow("{\"type\":\"turn/end\",\"data\":{\"x\":1}}");
        var ev = Assert.Single(events);
        Assert.Equal("turn/end", ev.GetProperty("type").GetString());
    }

    [Fact]
    public void ExpandRow_InvalidJson_ReturnsEmpty()
    {
        Assert.Empty(ZstdFrames.ExpandRow("not json"));
    }
}
