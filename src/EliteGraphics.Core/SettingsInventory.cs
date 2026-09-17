using System.Text.RegularExpressions;
using System.Globalization;
using System.Xml.Linq;

namespace EliteGraphics.Core;

public sealed record SettingEntry(string Setting, string Value, string Source, string Path, string Note)
{
    public bool CanEdit=>SettingEditor.CanEdit(this);
    public string DisplayValue { get; init; } = Value;
    public string Description => SettingReference.Describe(Path);
}

// Enumerate the actual files rather than maintaining a whitelist that hides new game settings.
public static class SettingsInventory
{
    static readonly Dictionary<string,string> Features = new()
    {
        ["AOQuality"]="HBAO", ["BloomQuality"]="Bloom", ["EnvmapQuality"]="Envmap",
        ["MaterialQuality"]="Materials", ["EnvironmentQuality"]="Environment", ["FXQuality"]="FX",
        ["ParticleEffectsQuality"]="ParticleEffects", ["GalaxyMapQuality"]="GalaxyMap",
        ["TerrainQuality"]="Terrain", ["TerrainLodBlendingQuality"]="TerrainLodBlending",
        ["SurfaceMaterialQuality"]="SurfaceMaterial", ["JetConeQuality"]="JetCones", ["VolumetricsQuality"]="Volumetrics",
        ["DOFEnabled"]="DOF", ["GUIColourQuality"]="GUIColour"
    };
    static readonly Dictionary<string, string> Names = new()
    {
        ["AAMode"]="Anti-aliasing", ["HMDRenderTargetMultiplier"]="HMD image quality",
        ["SSAAMultiplier"]="Supersampling", ["TextureQualityEx"]="Texture quality",
        ["TextureFilterQuality"]="Anisotropic filtering", ["SurfaceSamplerQuality"]="Terrain surface sampling",
        ["DirectionalShadowQuality"]="Directional shadows", ["SpotShadowQuality"]="Spot shadows",
        ["UpscalingQuality"]="Upscaling", ["AOQuality"]="Ambient occlusion",
        ["DOFEnabled"]="Depth of field", ["BlurEnabled"]="Blur", ["BloomQuality"]="Bloom",
        ["EnvmapQuality"]="Reflections / environment map", ["MaterialQuality"]="Material quality",
        ["EnvironmentQuality"]="Environment quality", ["FXQuality"]="Effects quality",
        ["ParticleEffectsQuality"]="Particle effects", ["GalaxyMapQuality"]="Galaxy map quality",
        ["GUIColourQuality"]="HUD colour preset", ["TerrainQuality"]="Terrain quality",
        ["TerrainLodBlendingQuality"]="Terrain LOD blending", ["SurfaceMaterialQuality"]="Terrain material quality",
        ["JetConeQuality"]="Jet cone quality", ["VolumetricsQuality"]="Volumetric effects",
        ["LODDistanceScale"]="Model draw distance", ["GpuSchedulerMultiplier"]="Terrain work",
        ["FFXCASIntensity"]="CAS sharpening intensity", ["TerrainCheckerboardRenderingEnabled"]="Terrain checkerboard rendering",
        ["ScreenWidth"]="Display resolution width", ["ScreenHeight"]="Display resolution height",
        ["FullScreen"]="Fullscreen / window mode", ["VSync"]="Vertical sync",
        ["LimitFrameRate"]="Frame rate limiter enabled", ["MaxFramesPerSecond"]="Frame rate limit",
        ["DX11_RefreshRateNumerator"]="Display refresh rate numerator", ["DX11_RefreshRateDenominator"]="Display refresh rate denominator",
        ["Adapter"]="Display adapter", ["Monitor"]="Monitor", ["StereoscopicMode"]="3D / headset mode",
        ["IPDAmount"]="Separation / IPD adjustment", ["FOV"]="Ship field of view",
        ["HumanoidFOV"]="On-foot field of view", ["GammaOffset"]="Gamma",
        ["DisableGuiEffects"]="Disable GUI effects", ["StereoFocalDistance"]="Stereo focal distance",
        ["VehicleMotionBlackout"]="Vehicle motion blackout", ["VehicleMaintainHorizonCamera"]="Maintain vehicle horizon",
        ["DisableCameraShake"]="Disable camera shake", ["HeadBobScale"]="Head bob scale",
        ["HighResScreenCapAntiAlias"]="High-resolution screenshot AA", ["HighResScreenCapScale"]="High-resolution screenshot scale"
    };

    public static string Label(string field) => Names.GetValueOrDefault(field, Regex.Replace(field, "(?<=[a-z0-9])(?=[A-Z])", " "));

    public static string FormatValue(string field,string value,XElement? definitions=null,bool selection=true)
    {
        if(value.StartsWith('$'))return SettingReference.LocalisationLabel(value);
        if(bool.TryParse(value,out var flag))return flag?"On":"Off";
        if(!double.TryParse(value,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)||!double.IsFinite(n))return value;
        string number=n.ToString("0.######",CultureInfo.InvariantCulture);
        if(selection && Features.TryGetValue(field,out var feature))
        {
            var tiers=definitions?.Element(feature)?.Elements().Where(e=>e.Element("LocalisationName")!=null).ToArray();
            if(tiers!=null&&n==Math.Truncate(n)&&n>=0&&n<tiers.Length)
                return TierLabel(tiers[(int)n]);
            return $"Unmapped quality ({number})";
        }
        if(selection && SettingReference.Modes.TryGetValue(field,out var modes))return modes.FirstOrDefault(m=>m.Value==number).Label ?? $"Unverified mode ({number})";
        if(selection && field is "DirectionalShadowQuality" or "SpotShadowQuality")return number switch {"0"=>"Off","1"=>"Low","2"=>"Medium","3"=>"High","4"=>"Ultra",_=>$"Unmapped quality ({number})"};
        if(selection && field=="SurfaceSamplerQuality")return number switch {"0"=>"Low","1"=>"Medium","2"=>"High","3"=>"Ultra",_=>$"Unmapped quality ({number})"};
        if(selection && field=="TextureQualityEx")return number switch {"0"=>"Low","1"=>"Medium","2"=>"High",_=>$"Unmapped quality ({number})"};
        if(field is "HMDRenderTargetMultiplier" or "SSAAMultiplier" or "HighResScreenCapScale" or "HeadBobScale")return n.ToString("0.0##",CultureInfo.InvariantCulture)+"×";
        if(field is "LODDistanceScale" or "GpuSchedulerMultiplier" or "FFXCASIntensity")return (n*100).ToString("0.##",CultureInfo.InvariantCulture)+"%";
        if(field=="MaxFramesPerSecond")return number+" FPS";
        if(field is "ScreenWidth" or "ScreenHeight" or "TextureSize" or "BlendTargetsResolution")return number+" px";
        if(field is "FOV" or "HumanoidFOV")return n.ToString("0.##",CultureInfo.InvariantCulture)+"°";
        if(selection && (field.EndsWith("Quality")||field is "TextureQualityEx" or "FullScreen" or "StereoscopicMode"))return $"Unmapped mode ({number})";
        return number;
    }

    static string TierLabel(XElement tier)
    {
        var token=tier.Element("LocalisationName")?.Value;
        if(token?.StartsWith('$')==true)return SettingReference.LocalisationLabel(token);
        return tier.Name.LocalName switch {"Mid"=>"Medium","UltraPlus"=>"Ultra+","Default"=>"Standard",var name=>name};
    }

    public static IReadOnlyList<(string Value,string Label)> FeatureChoices(string field,XElement? definitions)
    {
        if(!Features.TryGetValue(field,out var feature))return [];
        return definitions?.Element(feature)?.Elements().Where(e=>e.Element("LocalisationName")!=null)
            .Select((e,i)=>(i.ToString(CultureInfo.InvariantCulture),TierLabel(e))).ToArray() ?? [];
    }

    public static IReadOnlyList<SettingEntry> Build(FileSet files, byte[] definitions, bool allFiles, bool includeDefaults)
    {
        string? active=null;
        try { active=GraphicsModel.ActiveFile(files); } catch (InvalidDataException) { }
        var source=files.Clone();
        if (!allFiles)
            foreach (var name in source.Keys.Where(n=>n.EndsWith(".fxcfg",StringComparison.OrdinalIgnoreCase)&&n!=active).ToArray()) source.Remove(name);
        if (includeDefaults && definitions.Length>0) source["Installed defaults (snapshot).xml"]=definitions;
        var comparison=new ComparisonSource("Inventory",source,definitions);
        var definitionRoot=definitions.Length>0?XmlIO.Read(definitions).Root:null;
        var rows=new List<SettingEntry>();
        foreach (var row in PresetComparison.Build([comparison,comparison],true,false))
        {
            if (row.Setting=="(file present)") continue;
            var field=Regex.Replace(row.Setting.Split('/').Last(), @"\[\d+\]$", "");
            string note=row.Source==active ? "Saved quality selection; active Custom schema inferred." :
                row.Source=="GraphicsConfigurationOverride.xml" ? "Override; may target an inactive tier. Duplicate paths retained." :
                row.Source=="Installed defaults (snapshot).xml" ? "Shipped definition, not necessarily selected; overrides can replace it." :
                row.Source.EndsWith(".fxcfg",StringComparison.OrdinalIgnoreCase) ? "Older/unselected Custom schema; not treated as active." : "Saved configuration value.";
            if (field=="AAMode") note+=" FXAA=1 verified from an in-game save; SMAA=4 correlated with shipped Ultra presets and community configurations. Other modes remain unverified.";
            if (field=="TerrainCheckerboardRenderingEnabled") note+=" If Custom and Settings disagree, precedence is unresolved.";
            if (field=="FFXCASIntensity") note+=" A saved intensity does not prove sharpening is active.";
            if (field is "UpscalingQuality" or "TextureFilterQuality" or "FullScreen") note+=" Menu labels use an inferred mapping; not independently verified by a current-build game-save comparison. See docs/SETTING-REFERENCE.md.";
            if (field is "DirectionalShadowQuality" or "SpotShadowQuality" or "SurfaceSamplerQuality" or "TextureQualityEx") note+=" Quality labels inferred from shipped Low/Medium/High/Ultra presets.";
            if (row.Source==active && Features.TryGetValue(field,out var feature) && int.TryParse(row.Cells[0].Value,out var tierIndex))
            {
                var tiers=definitionRoot?.Element(feature)?.Elements().Where(e=>e.Element("LocalisationName")!=null).ToArray();
                if(tiers!=null && tierIndex>=0 && tierIndex<tiers.Length) note=$"{tiers[tierIndex].Name.LocalName} — inferred from saved definitions. "+note;
            }
            rows.Add(new(Label(field),row.Cells[0].Value,row.Source,row.Setting,note){DisplayValue=FormatValue(field,row.Cells[0].Value,definitionRoot,row.Source.EndsWith(".fxcfg",StringComparison.OrdinalIgnoreCase)||row.Source is "Settings.xml" or "DisplaySettings.xml")});
        }
        return rows.OrderBy(r=>r.Source).ThenBy(r=>r.Setting).ThenBy(r=>r.Path).ToArray();
    }
}
