using System.Globalization;
using System.Text.Json;

namespace HD2_Helper;

internal static class UserCodeStore
{
    internal static Dictionary<uint, string> Load(string path, IEnumerable<string> names)
    {
        if (!File.Exists(path)) return new();
        var valid = names.ToHashSet(StringComparer.Ordinal);
        var result = new Dictionary<uint, string>();
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            var name = p.Value.GetString();
            if (name == "고속 정찰 차량") name = "포격 FRV";
            if (p.Name.Length != 8 || !uint.TryParse(p.Name, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)
                || name == null || !valid.Contains(name) || !result.TryAdd(code, name))
                throw new InvalidDataException("사용자 코드표의 코드 또는 스트라타잼 이름을 확인해 주세요: " + path);
        }
        return result;
    }

    internal static void Save(string path, IReadOnlyDictionary<uint, string> codes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var data = codes.OrderBy(p => p.Key).ToDictionary(p => p.Key.ToString("X8"), p => p.Value);
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, data, new JsonSerializerOptions { WriteIndented = true });
                stream.Flush(true);
            }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
