using System.Text.RegularExpressions;

namespace EliteGraphics.Core;

/// <summary>Write permissions are narrower than snapshot capture permissions.</summary>
public static partial class ManagedGraphicsFiles
{
    [GeneratedRegex(@"\ACustom\.[0-9]+\.[0-9]+\.fxcfg\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CustomPreset();

    public static bool CanWrite(string name) => FileSet.Allowed(name) &&
        (new[] { "Settings.xml", "DisplaySettings.xml", "GraphicsConfigurationOverride.xml", "StartPreset.start" }
            .Contains(name, StringComparer.OrdinalIgnoreCase) || CustomPreset().IsMatch(name));

    public static FileSet ForApply(FileSet desired, FileSet current)
    {
        var managed = new FileSet();
        foreach (var (name, bytes) in desired)
        {
            if (CanWrite(name)) managed.Add(name, bytes);
            else if (!current.TryGetValue(name, out var existing) || !existing.SequenceEqual(bytes))
                throw new InvalidDataException("Cannot apply an unrecognised graphics file: " + name +
                    ". Capture current settings to preserve it unchanged.");
        }
        return managed;
    }
}
