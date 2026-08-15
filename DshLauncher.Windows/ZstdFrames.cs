using System.Text;
using System.Text.Json;
using ZstdSharp;

namespace DshLauncher;

/// <summary>
/// zstd frame structure scanning + decoding, matching the dsh session
/// persistence format (concatenated zstd frames, each holding JSONL rows).
/// </summary>
public static class ZstdFrames
{
    /// <summary>zstd frame magic (28 B5 2F FD little-endian).</summary>
    public const uint Magic = 4247762216;

    /// <summary>Scan complete [start, end) frame ranges; tornStart is the offset
    /// of the last incomplete (truncated) frame, or buffer.Length if none.</summary>
    public static List<(int Start, int End)> Scan(ReadOnlySpan<byte> buffer, out int tornStart)
    {
        var frames = new List<(int, int)>();
        int offset = 0;
        tornStart = buffer.Length;

        while (offset < buffer.Length)
        {
            int start = offset;
            if (buffer.Length - offset < 4) { tornStart = start; return frames; }
            if (BitConverter.ToUInt32(buffer.Slice(offset, 4)) != Magic) { tornStart = start; return frames; }
            offset += 4;
            if (offset == buffer.Length) { tornStart = start; return frames; }

            byte descriptor = buffer[offset];
            offset += 1;
            if ((descriptor & 24) != 0) { tornStart = start; return frames; } // reserved bits must be 0

            int contentSizeFlag = descriptor >>> 6;
            bool singleSegment = (descriptor & 32) != 0;
            bool checksum = (descriptor & 4) != 0;
            int dictionaryFlag = descriptor & 3;
            int dictionaryBytes = dictionaryFlag == 3 ? 4 : dictionaryFlag;
            int contentSizeBytes = contentSizeFlag == 0 ? (singleSegment ? 1 : 0) : (1 << contentSizeFlag);
            int remainingHeaderBytes = (singleSegment ? 0 : 1) + dictionaryBytes + contentSizeBytes;
            if (buffer.Length - offset < remainingHeaderBytes) { tornStart = start; return frames; }
            offset += remainingHeaderBytes;

            for (;;)
            {
                if (buffer.Length - offset < 3) { tornStart = start; return frames; }
                int blockHeader = buffer[offset] | (buffer[offset + 1] << 8) | (buffer[offset + 2] << 16);
                offset += 3;
                bool lastBlock = (blockHeader & 1) != 0;
                int blockType = (blockHeader >>> 1) & 3;
                int blockSize = blockHeader >>> 3;
                if (blockType == 3) { tornStart = start; return frames; } // reserved block type
                int payloadBytes = blockType == 1 ? 1 : blockSize;
                if (buffer.Length - offset < payloadBytes) { tornStart = start; return frames; }
                offset += payloadBytes;
                if (lastBlock) break;
            }

            if (checksum)
            {
                if (buffer.Length - offset < 4) { tornStart = start; return frames; }
                offset += 4;
            }
            frames.Add((start, offset));
        }
        return frames;
    }

    /// <summary>Decompress one complete frame to UTF-8 text.</summary>
    public static string Decode(ReadOnlySpan<byte> frame)
    {
        using var decompressor = new Decompressor();
        return Encoding.UTF8.GetString(decompressor.Unwrap(frame.ToArray()));
    }

    /// <summary>Expand one JSONL row into its events (storage rows pack chunk events).</summary>
    public static IReadOnlyList<JsonElement> ExpandRow(string line)
    {
        JsonElement row;
        try { row = JsonDocument.Parse(line).RootElement; }
        catch { return Array.Empty<JsonElement>(); }
        if (row.ValueKind != JsonValueKind.Object) return Array.Empty<JsonElement>();
        switch (row.GetPropertyOrNull("type")?.GetString())
        {
            case "text-chunks":
            case "reasoning-chunks":
            {
                var texts = row.GetPropertyOrNull("data")?.GetPropertyOrNull("texts");
                if (texts is { ValueKind: JsonValueKind.Array })
                    return texts.Value.EnumerateArray().ToList();
                return Array.Empty<JsonElement>();
            }
            case "tool-call-chunks":
            {
                var args = row.GetPropertyOrNull("data")?.GetPropertyOrNull("args");
                if (args is { ValueKind: JsonValueKind.Array })
                    return args.Value.EnumerateArray().ToList();
                return Array.Empty<JsonElement>();
            }
            default:
                return new[] { row };
        }
    }

    /// <summary>Get a property of a JSON object, or null when absent.</summary>
    internal static JsonElement? GetPropertyOrNull(this JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return null;
        return obj.TryGetProperty(name, out var value) ? value : null;
    }
}
