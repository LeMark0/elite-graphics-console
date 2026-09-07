using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace EliteGraphics.Core;

public sealed record SettingDiff(string File,string Path,string Before,string After);
public sealed record EffectiveValue(string Label,string Value,string Source);

public static partial class GraphicsModel
{
    [GeneratedRegex(@"^Custom\.(\d+)\.(\d+)\.fxcfg$",RegexOptions.IgnoreCase)] private static partial Regex PresetPattern();
    public static string ActiveFile(FileSet files)
    {
        var preset=XmlIO.Read(files["Settings.xml"]).Root?.Element("PresetName")?.Value;
        if(preset!="Custom") throw new InvalidDataException("This version edits Custom presets. Select Custom in Elite once, then capture its settings.");
        var result=files.Keys.Select(x=>(Name:x,Match:PresetPattern().Match(x))).Where(x=>x.Match.Success).OrderByDescending(x=>int.Parse(x.Match.Groups[1].Value,CultureInfo.InvariantCulture)).ThenByDescending(x=>int.Parse(x.Match.Groups[2].Value,CultureInfo.InvariantCulture)).FirstOrDefault();
        if(result.Name==null) throw new InvalidDataException("No Custom quality preset found.");
        var root=XmlIO.Read(files[result.Name]).Root!;
        if(root.Attribute("MajorVersion")?.Value!=result.Match.Groups[1].Value || root.Attribute("MinorVersion")?.Value!=result.Match.Groups[2].Value) throw new InvalidDataException("Preset filename and schema version disagree.");
        return result.Name;
    }
    public static string Quality(FileSet files,string field) => XmlIO.Read(files[ActiveFile(files)]).Root?.Element(field)?.Value ?? "";
    public static string General(FileSet files,string field) => XmlIO.Read(files["Settings.xml"]).Root?.Element(field)?.Value ?? "";
    public static string Display(FileSet files,string field) => files.TryGetValue("DisplaySettings.xml",out var b)?XmlIO.Read(b).Root?.Element(field)?.Value??"":"";
    public static void Set(FileSet files,string filename,string field,string value)
    {
        if(!files.TryGetValue(filename,out var bytes)) throw new InvalidDataException(filename+" is missing.");
        var xml=XmlIO.Read(bytes);var nodes=xml.Root!.Elements(field).ToList();
        if(nodes.Count==0)xml.Root.Add(new XElement(field,value));else foreach(var n in nodes)n.Value=value;
        files[filename]=XmlIO.Write(xml);
    }
    static XElement Definitions(byte[] bytes) => bytes.Length>0?XmlIO.Read(bytes).Root!:throw new InvalidDataException("Locate the game's GraphicsConfiguration.xml to resolve effective settings.");
    static int Number(string value,string label) => int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)&&n>=0?n:throw new InvalidDataException("Invalid "+label+" value.");
    public static string Tier(FileSet files,byte[] definitions,string feature)
    {
        var def=Definitions(definitions);var index=Number(Quality(files,"EnvironmentQuality"),"Environment Quality");
        var environment=def.Element("Environment")?.Elements().ElementAtOrDefault(index)??throw new InvalidDataException("Unknown environment tier.");
        var setting=environment.Elements("Item").SingleOrDefault(x=>x.Element("Feature")?.Value==feature)?.Element("QualitySetting")?.Value??throw new InvalidDataException("Missing environment selector for "+feature);
        return def.Element(feature)?.Elements().ElementAtOrDefault(Number(setting,feature))?.Name.LocalName??throw new InvalidDataException("Unknown "+feature+" tier.");
    }
    public static EffectiveValue Texture(FileSet files,byte[] definitions,string feature)
    {
        var tier=Tier(files,definitions,feature);var defaultValue=Definitions(definitions).Element(feature)?.Element(tier)?.Element("TextureSize")?.Value??"Unknown";
        var values=OverrideValues(files,feature,tier,"TextureSize");
        return new EffectiveValue(feature=="Planets"?"Planet textures":"Galaxy background",values.Distinct().Count()>1?"Ambiguous":values.FirstOrDefault()??defaultValue,$"{feature}/{tier} · "+(values.Count>0?"XML override":"installed definition")+" · inferred selector");
    }
    public static List<string> OverrideValues(FileSet files,string feature,string tier,string field)
    {
        if(!files.TryGetValue("GraphicsConfigurationOverride.xml",out var b))return [];
        return XmlIO.Read(b).Root!.Elements(feature).SelectMany(x=>x.Elements(tier)).SelectMany(x=>x.Elements(field)).Select(x=>x.Value).ToList();
    }
    public static void SetTexture(FileSet files,byte[] definitions,string feature,int size)
    {
        if(feature is not ("Planets" or "GalaxyBackground") || !new[]{512,1024,2048,2560,4096}.Contains(size))throw new ArgumentException("Unsupported texture size.");
        var tier=Tier(files,definitions,feature);
        var xml=files.TryGetValue("GraphicsConfigurationOverride.xml",out var b)?XmlIO.Read(b):new XDocument(new XElement("GraphicsConfig"));
        var sections=xml.Root!.Elements(feature).ToList();
        if(sections.Count==0){var section=new XElement(feature);xml.Root.Add(section);sections.Add(section);}
        var tiers=sections.SelectMany(x=>x.Elements(tier)).ToList();
        if(tiers.Count==0){var node=new XElement(tier);sections[0].Add(node);tiers.Add(node);}
        foreach(var node in tiers){var targets=node.Elements("TextureSize").ToList();if(targets.Count==0)node.Add(new XElement("TextureSize",size));else foreach(var target in targets)target.Value=size.ToString(CultureInfo.InvariantCulture);}
        files["GraphicsConfigurationOverride.xml"]=XmlIO.Write(xml);
    }
    public static FileSet Migrate(FileSet historical,FileSet current)
    {
        var target=ActiveFile(current);var old=ActiveFile(historical);var data=current.Clone();
        var latest=XmlIO.Read(data[target]);var previous=XmlIO.Read(historical[old]);
        foreach(var node in previous.Root!.Elements())
        {
            var existing=latest.Root!.Element(node.Name);if(existing!=null)existing.Value=node.Value;
        }
        data[target]=XmlIO.Write(latest);
        foreach(var name in new[]{"Settings.xml","DisplaySettings.xml","GraphicsConfigurationOverride.xml","StartPreset.start"})if(historical.TryGetValue(name,out var bytes))data[name]=bytes.ToArray();
        // Keep current schema attributes and preserve historical snapshots separately.
        return data;
    }
    public static IReadOnlyList<SettingDiff> Diff(FileSet a,FileSet b)
    {
        var result=new List<SettingDiff>();
        foreach(var file in a.Keys.Union(b.Keys,StringComparer.OrdinalIgnoreCase).OrderBy(x=>x))
        {
            if(!a.ContainsKey(file)||!b.ContainsKey(file)){result.Add(new(file,"(file)",a.ContainsKey(file)?"Present":"Missing",b.ContainsKey(file)?"Present":"Missing"));continue;}
            if(a[file].SequenceEqual(b[file]))continue;
            if(file.EndsWith(".start",StringComparison.OrdinalIgnoreCase)){result.Add(new(file,"(text)",System.Text.Encoding.UTF8.GetString(a[file]),System.Text.Encoding.UTF8.GetString(b[file])));continue;}
            var left=Flatten(XmlIO.Read(a[file]));var right=Flatten(XmlIO.Read(b[file]));var count=result.Count;
            foreach(var key in left.Keys.Union(right.Keys).OrderBy(x=>x)){var l=left.GetValueOrDefault(key,"<missing>");var r=right.GetValueOrDefault(key,"<missing>");if(l!=r)result.Add(new(file,key,l,r));}
            if(result.Count==count)result.Add(new(file,"(formatting)","Original bytes","Formatting/encoding only"));
        }
        return result;
    }
    static Dictionary<string,string> Flatten(XDocument doc)
    {
        var values=new Dictionary<string,string>();
        void Walk(XElement node,string path){foreach(var attr in node.Attributes())values[path+"/@"+attr.Name]=attr.Value;if(!node.HasElements)values[path]=node.Value.Trim();foreach(var group in node.Elements().GroupBy(x=>x.Name)){int i=0;foreach(var child in group){i++;Walk(child,path+"/"+child.Name+"["+i+"]");}}}
        Walk(doc.Root!,"/"+doc.Root!.Name+"[1]");return values;
    }
    public static IReadOnlyList<string> Warnings(FileSet files,byte[] definitions)
    {
        var messages=new List<string>{"Effective quality is inferred from the highest saved Custom schema, not live game inspection."};
        if(General(files,"TerrainCheckerboardRenderingEnabled")!=Quality(files,"TerrainCheckerboardRenderingEnabled"))messages.Add("Terrain checkerboard values disagree between Settings.xml and the quality preset. Precedence is unverified.");
        if(files.TryGetValue("GraphicsConfigurationOverride.xml",out var b)&&XmlIO.Read(b).Root!.Elements().GroupBy(x=>x.Name).Any(x=>x.Count()>1))messages.Add("Duplicate override sections preserved. Conflicting selected values are reported as ambiguous.");
        if(definitions.Length==0)messages.Add("Game definitions unavailable. Set the game directory in Paths.");
        return messages;
    }
}
