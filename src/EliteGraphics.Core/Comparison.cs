using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace EliteGraphics.Core;

public sealed record ComparisonSource(string Name, FileSet Files, byte[] Definitions);
public sealed record ComparisonCell(string Value, bool Different);
public sealed record ComparisonRow(string Setting, string Source, ComparisonCell[] Cells)
{
    public bool Changed => Cells.Any(x => x.Different);
}

public static class PresetComparison
{
    static readonly Dictionary<string, string> Labels = new()
    {
        ["AAMode"] = "Anti-aliasing (raw mode)", ["BloomQuality"] = "Bloom",
        ["SurfaceSamplerQuality"] = "Terrain surface sampling (raw quality)",
        ["TerrainLodBlendingQuality"] = "Terrain LOD blending", ["TerrainQuality"] = "Terrain quality",
        ["EnvironmentQuality"] = "Environment quality", ["HMDRenderTargetMultiplier"] = "HMD image quality",
        ["SSAAMultiplier"] = "Supersampling", ["LODDistanceScale"] = "Model draw distance",
        ["AOQuality"] = "Ambient occlusion", ["SurfaceMaterialQuality"] = "Surface material quality"
    };

    public static IReadOnlyList<ComparisonRow> Build(IReadOnlyList<ComparisonSource> sources, bool raw, bool differencesOnly)
    {
        if (sources.Count < 2) return [];
        var maps = sources.Select(x => raw ? Raw(x.Files) : Resolved(x)).ToList();
        var keys = maps.SelectMany(x => x.Keys).Distinct().OrderBy(x => x.Group).ThenBy(x => x.Path).ToList();
        var rows = new List<ComparisonRow>();
        foreach (var key in keys)
        {
            var values = maps.Select(x => x.GetValueOrDefault(key, "— absent")).ToArray();
            var label = raw ? key.Path : Labels.GetValueOrDefault(key.Path, SettingsInventory.Label(key.Path));
            var row = new ComparisonRow(label, key.Group, values.Select(v => new ComparisonCell(v, v != values[0])).ToArray());
            if (!differencesOnly || row.Changed) rows.Add(row);
        }
        return rows;
    }

    static string Display(string field, string value)
    {
        string[]? tiers = field switch
        {
            "BloomQuality" => ["Off", "Medium", "High", "Ultra"],
            "TerrainLodBlendingQuality" => ["Off", "High", "Ultra"],
            "TerrainQuality" => ["Low", "Medium", "High", "Ultra", "Ultra+"],
            "EnvironmentQuality" or "SurfaceMaterialQuality" => ["Low", "Medium", "High", "Ultra"],
            "AOQuality" => ["Off", "Low", "Medium", "High"],
            _ => null
        };
        if (tiers != null && int.TryParse(value, out var index) && index >= 0 && index < tiers.Length)
            return $"{tiers[index]} ({value})";
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            return number.ToString("0.######", CultureInfo.InvariantCulture);
        return value;
    }

    static Dictionary<(string Group, string Path), string> Resolved(ComparisonSource source)
    {
        var result = new Dictionary<(string, string), string>();
        try
        {
            var active = GraphicsModel.ActiveFile(source.Files);
            foreach (var node in XmlIO.Read(source.Files[active]).Root!.Elements())
                result[("Active Custom preset (inferred)", node.Name.LocalName)] = Display(node.Name.LocalName, node.Value);
        }
        catch (InvalidDataException ex) { result[("Active Custom preset (inferred)", "Availability")] = "Unknown: " + ex.Message; }
        foreach (var feature in new[] { "Planets", "GalaxyBackground" })
        {
            string label = feature == "Planets" ? "Planet texture size" : "Galaxy background texture size";
            try { result[("Effective textures (inferred)", label)] = GraphicsModel.Texture(source.Files, source.Definitions, feature).Value; }
            catch (Exception ex) when (ex is InvalidDataException or KeyNotFoundException) { result[("Effective textures (inferred)", label)] = "Unknown"; }
        }
        try
        {
            var tier = GraphicsModel.Tier(source.Files, source.Definitions, "Planets");
            var values = GraphicsModel.OverrideValues(source.Files, "Planets", tier, "AtmosphereSteps");
            result[("Effective textures (inferred)", "Planet atmosphere steps")] = values.Distinct().Count() > 1 ? "Ambiguous" :
                values.FirstOrDefault() ?? XmlIO.Read(source.Definitions).Root?.Element("Planets")?.Element(tier)?.Element("AtmosphereSteps")?.Value ?? "Unknown";
        }
        catch (Exception ex) when (ex is InvalidDataException or KeyNotFoundException) { result[("Effective textures (inferred)", "Planet atmosphere steps")] = "Unknown"; }
        foreach (var name in new[] { "Settings.xml", "DisplaySettings.xml" })
            if (source.Files.TryGetValue(name, out var bytes))
                foreach (var node in XmlIO.Read(bytes).Root!.Elements()) result[(name, node.Name.LocalName)] = Display(node.Name.LocalName, node.Value);
        if (source.Files.TryGetValue("StartPreset.start", out var start)) result[("StartPreset.start", "Startup preset text")] = Encoding.UTF8.GetString(start);
        return result;
    }

    static Dictionary<(string Group, string Path), string> Raw(FileSet files)
    {
        var result = new Dictionary<(string, string), string>();
        foreach (var (name, bytes) in files)
        {
            result[(name, "(file present)")] = "Yes";
            if (name.EndsWith(".start", StringComparison.OrdinalIgnoreCase)) { result[(name, "(text)")] = Encoding.UTF8.GetString(bytes); continue; }
            void Walk(XElement node, string path)
            {
                foreach (var attr in node.Attributes()) result[(name, path + "/@" + attr.Name)] = attr.Value;
                if (!node.HasElements) result[(name, path)] = node.Value.Trim();
                foreach (var group in node.Elements().GroupBy(x => x.Name))
                {
                    int index = 0;
                    foreach (var child in group) Walk(child, path + "/" + child.Name + "[" + (++index) + "]");
                }
            }
            var root = XmlIO.Read(bytes).Root!;
            Walk(root, "/" + root.Name + "[1]");
        }
        return result;
    }
}
