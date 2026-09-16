using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace EliteGraphics.Core;

public sealed record ProfileRevision
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string PresetId { get; init; } = "";
    public string Name { get; init; } = "New profile";
    public int Number { get; init; } = 1;
    public string? ParentId { get; init; }
    public string Mode { get; init; } = "VR";
    public string Status { get; init; } = "Untested";
    public bool Protected { get; init; }
    public bool Historical { get; init; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public string Notes { get; init; } = "";
    public string GameBuild { get; init; } = "Unknown";
    public string DefinitionHash { get; init; } = "";
    public Dictionary<string, string> Hashes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string DisplayName => $"{Name} · r{Number:000}{(Protected ? "  ◆" : "")}{(Historical ? "  [legacy]" : "")}";
    public override string ToString() => DisplayName;
}

public sealed class FileSet : Dictionary<string, byte[]>
{
    public FileSet() : base(StringComparer.OrdinalIgnoreCase) { }
    public FileSet Clone() { var copy = new FileSet(); foreach (var (k,v) in this) copy[k] = v.ToArray(); return copy; }
    public Dictionary<string,string> Hashes() => this.ToDictionary(k => k.Key, v => Hash(v.Value), StringComparer.OrdinalIgnoreCase);
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    public string Fingerprint() => Hash(Encoding.UTF8.GetBytes(string.Join("\n", Hashes().OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.Key.ToLowerInvariant()+":"+x.Value))));
    public static bool Allowed(string name) => name == Path.GetFileName(name) && !name.Contains(':') && !name.Contains('\\') && !name.Contains('/') && !name.StartsWith('.') && new[]{".xml",".fxcfg",".start"}.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase);
    public static FileSet Read(string folder)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Graphics directory not found: " + folder);
        var files = new FileSet();
        foreach (var file in Directory.GetFiles(folder).Where(x => Allowed(Path.GetFileName(x))))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Linked graphics files are not supported.");
            if (new FileInfo(file).Length > 4_000_000) throw new InvalidDataException("Unexpectedly large graphics file.");
            files.Add(Path.GetFileName(file), File.ReadAllBytes(file));
        }
        return files;
    }
    public void Validate()
    {
        if (!ContainsKey("Settings.xml")) throw new InvalidDataException("Settings.xml is required.");
        foreach (var (name, bytes) in this)
        {
            if (!Allowed(name)) throw new InvalidDataException("Unsafe graphics file name.");
            if (bytes.Length > 4_000_000) throw new InvalidDataException("Unexpectedly large graphics file.");
            if (!name.EndsWith(".start",StringComparison.OrdinalIgnoreCase)) _ = XmlIO.Read(bytes);
        }
    }
}

public static class XmlIO
{
    public static XDocument Read(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 4_000_000 });
        return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
    }
    public static byte[] Write(XDocument doc) { using var stream = new MemoryStream(); doc.Save(stream,SaveOptions.DisableFormatting); return stream.ToArray(); }
}

public static class JsonIO
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public static T Load<T>(string path) => JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options) ?? throw new InvalidDataException("Invalid metadata.");
    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(value, Options));
        File.Move(temp,path,true);
    }
}

public sealed record AppPaths
{
    public string Graphics { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Frontier Developments","Elite Dangerous","Options","Graphics");
    public string Game { get; set; } = "";
    public string Legacy { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"ED scripts","presets");
    public string Definitions => Path.Combine(Game,"GraphicsConfiguration.xml");
    public byte[] ReadDefinitions() => File.Exists(Definitions) ? File.ReadAllBytes(Definitions) : [];
    public string Build()
    {
        try { using var d=JsonDocument.Parse(File.ReadAllText(Path.Combine(Game,"VersionInfo.txt"))); return d.RootElement.GetProperty("Version").GetString() ?? "Unknown"; }
        catch (Exception ex) when(ex is IOException or JsonException or KeyNotFoundException) { return "Unknown"; }
    }
}
