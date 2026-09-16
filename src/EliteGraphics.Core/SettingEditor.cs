using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace EliteGraphics.Core;

public static class SettingEditor
{
    public static bool CanEdit(SettingEntry row)=>row.Source!="Installed defaults (snapshot).xml"&&!row.Path.EndsWith("/@MajorVersion")&&!row.Path.EndsWith("/@MinorVersion");
    public static FileSet Change(FileSet source,SettingEntry row,string value)
    {
        if(!CanEdit(row)||!source.ContainsKey(row.Source))throw new InvalidDataException("This row is reference metadata and cannot be edited.");
        if(bool.TryParse(row.Value,out _)&&!bool.TryParse(value,out _))throw new ArgumentException("Enter true or false.");
        if(double.TryParse(row.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out _))
        {
            if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n))throw new ArgumentException("Enter a finite number using a decimal point.");
            if(long.TryParse(row.Value,out _)&&n!=Math.Truncate(n))throw new ArgumentException("Enter a whole number.");
        }
        var result=source.Clone();
        if(row.Source.EndsWith(".start",StringComparison.OrdinalIgnoreCase)){result[row.Source]=Encoding.UTF8.GetBytes(value);return result;}
        var xml=XmlIO.Read(result[row.Source]);var parts=row.Path.TrimStart('/').Split('/');var node=xml.Root??throw new InvalidDataException("Missing root.");
        if(parts[0]!=node.Name.ToString()&&parts[0]!=node.Name+"[1]")throw new InvalidDataException("XML root changed.");
        for(int i=1;i<parts.Length;i++)
        {
            if(parts[i].StartsWith('@')){var attribute=node.Attribute(parts[i][1..])??throw new InvalidDataException("Attribute missing.");attribute.Value=value;result[row.Source]=XmlIO.Write(xml);result.Validate();return result;}
            var match=Regex.Match(parts[i],@"^(.+)\[(\d+)\]$");if(!match.Success)throw new InvalidDataException("Unsupported XML path.");
            node=node.Elements(match.Groups[1].Value).ElementAtOrDefault(int.Parse(match.Groups[2].Value)-1)??throw new InvalidDataException("Setting no longer exists.");
        }
        if(node.HasElements)throw new InvalidDataException("Only leaf values can be edited.");
        node.Value=value;result[row.Source]=XmlIO.Write(xml);result.Validate();return result;
    }
    public static IReadOnlyList<(string Value,string Label)> Choices(SettingEntry row,byte[] definitions)
    {
        if(bool.TryParse(row.Value,out _))return [("true","On"),("false","Off")];
        var field=Regex.Replace(row.Path.Split('/').Last(),@"\[\d+\]$","");
        if(!(row.Source.EndsWith(".fxcfg")||row.Source is "Settings.xml" or "DisplaySettings.xml"))return [];
        var root=definitions.Length>0?XmlIO.Read(definitions).Root:null;
        var choices=new List<(string,string)>();
        for(int i=0;i<16;i++){var raw=i.ToString(CultureInfo.InvariantCulture);var label=SettingsInventory.FormatValue(field,raw,root);if(label!=raw&&!label.StartsWith("Unmapped")&&!label.StartsWith("Unverified")&&(field.EndsWith("Quality")||field is "TextureQualityEx" or "AAMode"))choices.Add((raw,label));}
        if(choices.Count>0&&!choices.Any(x=>x.Item1==row.Value))choices.Add((row.Value,row.DisplayValue));
        return choices;
    }
}
