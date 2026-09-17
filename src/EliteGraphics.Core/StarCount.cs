using System.Globalization;
using System.Xml.Linq;

namespace EliteGraphics.Core;

public static partial class GraphicsModel
{
    public static string StarCountTier(FileSet files,byte[] definitions)
    {
        var selectors=XmlIO.Read(files[ActiveFile(files)]).Root!.Elements("GalaxyMapQuality").Select(e=>e.Value).Distinct().ToArray();
        if(selectors.Length!=1)throw new InvalidDataException("Galaxy map quality is missing or conflicting; select a known tier first.");
        var tiers=Definitions(definitions).Element("GalaxyMap")?.Elements().Where(e=>e.Element("LocalisationName")!=null).ToArray();
        var index=Number(selectors[0],"Galaxy Map Quality");
        if(tiers==null||index>=tiers.Length)throw new InvalidDataException("Galaxy map tier is unavailable in this preset's definitions.");
        return tiers[index].Name.LocalName;
    }

    public static EffectiveValue StarCount(FileSet files,byte[] definitions)
    {
        var tier=StarCountTier(files,definitions);
        var values=OverrideValues(files,"GalaxyMap",tier,"StarInstanceCount");
        var defaults=Definitions(definitions).Element("GalaxyMap")?.Element(tier)?.Elements("StarInstanceCount").Select(e=>e.Value).ToArray()??[];
        var candidates=values.Count>0?values.ToArray():defaults;
        var value=candidates.Distinct().Count()>1?"Ambiguous":candidates.FirstOrDefault()??"Unknown";
        return new("Visible star count",value,$"GalaxyMap/{tier}/StarInstanceCount · "+(values.Count>0?"XML override":"installed definition")+" · inferred selector");
    }

    public static void SetStarCount(FileSet files,byte[] definitions,int count)
    {
        if(count<0)throw new ArgumentException("Enter a non-negative whole-number star count.");
        var tier=StarCountTier(files,definitions);
        var xml=files.TryGetValue("GraphicsConfigurationOverride.xml",out var bytes)?XmlIO.Read(bytes):new XDocument(new XElement("GraphicsConfig"));
        var sections=xml.Root!.Elements("GalaxyMap").ToList();
        if(sections.Count==0){var section=new XElement("GalaxyMap");xml.Root.Add(section);sections.Add(section);}
        var tiers=sections.SelectMany(e=>e.Elements(tier)).ToList();
        if(tiers.Count==0){var node=new XElement(tier);sections[0].Add(node);tiers.Add(node);}
        foreach(var node in tiers)
        {
            var targets=node.Elements("StarInstanceCount").ToArray();
            if(targets.Length==0)node.Add(new XElement("StarInstanceCount",count));
            else foreach(var target in targets)target.Value=count.ToString(CultureInfo.InvariantCulture);
        }
        files["GraphicsConfigurationOverride.xml"]=XmlIO.Write(xml);
    }
}
