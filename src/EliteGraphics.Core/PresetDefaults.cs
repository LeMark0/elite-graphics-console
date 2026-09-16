using System.Xml.Linq;

namespace EliteGraphics.Core;

public static class PresetDefaults
{
    public static FileSet Create(FileSet current,byte[] template)
    {
        var result=current.Clone();var active=GraphicsModel.ActiveFile(result);
        var defaults=XmlIO.Read(template).Root??throw new InvalidDataException("Empty default preset.");
        if(defaults.Name.LocalName!="RenderOptions"||!defaults.Elements().Any())throw new InvalidDataException("Not an installed RenderOptions preset.");
        var general=XmlIO.Read(result["Settings.xml"]);var quality=XmlIO.Read(result[active]);
        foreach(var node in defaults.Elements())
        {
            if(node.HasElements)throw new InvalidDataException("Unexpected nested default setting.");
            // Preserve display/headset mode and HUD selection, as stated in the creation dialog.
            if(node.Name.LocalName is "StereoscopicMode" or "GUIColourQuality" or "PresetName")continue;
            var root=general.Root!.Element(node.Name)!=null?general.Root:quality.Root!;
            root.Elements(node.Name).Remove();root.Add(new XElement(node));
        }
        result[active]=XmlIO.Write(quality);result["Settings.xml"]=XmlIO.Write(general);
        if(result.TryGetValue("GraphicsConfigurationOverride.xml",out var bytes))
        {
            var original=XmlIO.Read(bytes);
            result["GraphicsConfigurationOverride.xml"]=XmlIO.Write(new XDocument(new XElement("GraphicsConfig",original.Root!.Elements("GUIColour").Select(e=>new XElement(e)))));
        }
        result.Validate();return result;
    }
}
