using System.Buffers.Binary;
using System.Globalization;
using System.Text.Json;

namespace HD2_Helper;

// Read-only parser for the save layout observed in September 2026.
// Offsets are checked against the surrounding structure; unsupported layouts fail closed.
internal static class SavedLoadoutReader
{
    private const int BlockSize = 65536;
    internal const int MaxFileSize = 4 * 1024 * 1024;
    private static uint U32(ReadOnlySpan<byte> b, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(offset, 4));

    internal static byte[] ReadSnapshot(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.Length < 40 || stream.Length > MaxFileSize) throw new InvalidDataException("저장 파일 크기를 확인할 수 없습니다.");
        var bytes = new byte[(int)stream.Length];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new IOException("게임이 파일을 저장 중입니다.");
        return bytes;
    }

    internal static uint[] Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 40 || bytes.Length > MaxFileSize ||
            !bytes[..8].SequenceEqual(Convert.FromHexString("D4FD1C6B5F7B23CC")) ||
            U32(bytes, 8) != 1 || U32(bytes, 16) != bytes.Length - 28 ||
            U32(bytes, 24) != 0xF0000001 || U32(bytes, 32) != 0)
            throw new InvalidDataException("지원하지 않거나 저장 중인 파일 형식입니다.");
        uint size = U32(bytes, 20);
        if (size < 61 || size > 2 * 1024 * 1024 || U32(bytes, 28) != size)
            throw new InvalidDataException("저장 파일의 압축 크기가 맞지 않습니다.");
        int blockCount = checked(((int)size + BlockSize - 1) / BlockSize);
        var payload = new byte[blockCount * BlockSize];
        int cursor = 36;
        for (int block = 0; block < blockCount; block++)
        {
            if (cursor > bytes.Length - 4) throw new InvalidDataException("압축 블록이 잘렸습니다.");
            uint length = U32(bytes, cursor); cursor += 4;
            if (length == 0 || length > bytes.Length - cursor) throw new InvalidDataException("압축 블록 길이가 맞지 않습니다.");
            DecodeBlock(bytes.Slice(cursor, (int)length), payload.AsSpan(block * BlockSize, BlockSize));
            cursor += (int)length;
        }
        if (cursor != bytes.Length) throw new InvalidDataException("예상하지 못한 추가 데이터가 있습니다.");
        for (int i = (int)size; i < payload.Length; i++)
            if (payload[i] != 0) throw new InvalidDataException("압축 뒤쪽 데이터가 맞지 않습니다.");
        // Bytes 21..24 vary independently of the four slots; their meaning is unknown.
        // Validate the surrounding markers, not the previous sample's field value.
        if (!payload.AsSpan(0, 8).SequenceEqual(Convert.FromHexString("060100003EEACEA6")) ||
            U32(payload, 8) != size ||
            !payload.AsSpan(16, 5).SequenceEqual(Convert.FromHexString("01A01FDC17")) ||
            !payload.AsSpan(25, 4).SequenceEqual(Convert.FromHexString("53ECD34F")))
            throw new InvalidDataException("장비 저장 구조가 바뀌었습니다. 연결을 중단합니다.");
        return new[] { U32(payload, 29), U32(payload, 37), U32(payload, 45), U32(payload, 53) };
    }

    private static void DecodeBlock(ReadOnlySpan<byte> src, Span<byte> dst)
    {
        int input = 0, output = 0;
        while (input < src.Length)
        {
            byte token = src[input++];
            int literals = Length(src, ref input, token >> 4);
            if (literals > src.Length - input || literals > dst.Length - output) throw new InvalidDataException("잘못된 LZ4 리터럴입니다.");
            src.Slice(input, literals).CopyTo(dst.Slice(output)); input += literals; output += literals;
            if (input == src.Length) break;
            if (src.Length - input < 2) throw new InvalidDataException("잘린 LZ4 참조입니다.");
            int offset = src[input] | (src[input + 1] << 8); input += 2;
            if (offset == 0 || offset > output) throw new InvalidDataException("잘못된 LZ4 참조입니다.");
            int count = Length(src, ref input, token & 15) + 4;
            if (count > dst.Length - output) throw new InvalidDataException("LZ4 블록이 너무 큽니다.");
            for (int i = 0; i < count; i++) { dst[output] = dst[output - offset]; output++; }
        }
        if (output != dst.Length) throw new InvalidDataException("LZ4 블록 크기가 맞지 않습니다.");
    }

    private static int Length(ReadOnlySpan<byte> src, ref int input, int length)
    {
        if (length != 15) return length;
        int extension;
        do
        {
            if (input >= src.Length) throw new InvalidDataException("잘린 LZ4 길이입니다.");
            extension = src[input++]; length += extension;
            if (length > BlockSize) throw new InvalidDataException("LZ4 길이가 너무 큽니다.");
        } while (extension == 255);
        return length;
    }

    internal static Dictionary<uint, string> LoadCodes(string path, IEnumerable<string> commandNames)
    {
        var names = commandNames.ToHashSet(StringComparer.Ordinal);
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var result = new Dictionary<uint, string>();
        foreach (var item in doc.RootElement.EnumerateObject())
        {
            string? name = item.Value.GetString();
            if (!uint.TryParse(item.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint id) ||
                name == null || !names.Contains(name) || !result.TryAdd(id, name))
                throw new InvalidDataException("장비 코드표의 이름 또는 코드가 잘못되었습니다.");
        }
        if (result.Count == 0) throw new InvalidDataException("장비 코드표가 비어 있습니다.");
        return result;
    }

    internal static string?[] Resolve(uint[] codes, IReadOnlyDictionary<uint, string> mapping)
    {
        if (codes.Length != 4) throw new ArgumentException("Four slots required.");
        return codes.Select(c => mapping.TryGetValue(c, out var name) ? name : null).ToArray();
    }
}

// Two consecutive identical file snapshots prevent accepting an in-progress write.
internal sealed class SavedLoadoutState
{
    private byte[]? pending;
    internal uint[]? Codes { get; private set; }
    internal void Reset() { pending = null; Codes = null; }
    private bool Stage(byte[] snapshot)
    {
        // Parsing validates a first snapshot but connections stay disabled until its repeat.
        try { SavedLoadoutReader.Parse(snapshot); }
        catch { Reset(); throw; }
        pending = (byte[])snapshot.Clone(); Codes = null;
        return false;
    }
    internal bool Poll(byte[] snapshot)
    {
        if (pending != null && snapshot.AsSpan().SequenceEqual(pending))
        {
            Codes ??= SavedLoadoutReader.Parse(snapshot);
            return true;
        }
        return Stage(snapshot);
    }
}
