using System.Globalization;
using System.Text.RegularExpressions;

namespace EliteGraphics.Core;

// Evidence and mapping confidence: docs/SETTING-REFERENCE.md.
public static class SettingReference
{
    public static readonly IReadOnlyDictionary<string,(string Value,string Label)[]> Modes =
        new Dictionary<string,(string,string)[]>
        {
            ["AAMode"]=[("0","Off"),("1","FXAA"),("4","SMAA")],
            ["UpscalingQuality"]=[("0","Normal"),("1","AMD FidelityFX CAS"),("2","AMD FSR 1.0")],
            ["TextureFilterQuality"]=[("0","Trilinear"),("1","Anisotropic 2×"),("2","Anisotropic 4×"),("3","Anisotropic 8×"),("4","Anisotropic 16×")],
            ["FullScreen"]=[("0","Windowed"),("1","Fullscreen"),("2","Borderless")]
        };

    public static string Field(string path)=>Regex.Replace(path.Split('/').Last(),@"\[\d+\]$","");
    public static string LocalisationLabel(string token)
    {
        var key=token.Trim('$',';');
        if(key.StartsWith("QUALITY_",StringComparison.Ordinal))key=key[8..];
        return key switch {"ULTRAPLUS" or "ULTRA_PLUS"=>"Ultra+","MID"=>"Medium",_=>CultureInfo.InvariantCulture.TextInfo.ToTitleCase(key.Replace('_',' ').ToLowerInvariant())};
    }

    public static string Describe(string path)
    {
        var field=Field(path);
        if(Descriptions.TryGetValue(field,out var description))return description;
        if(field=="TextureSize")return path.Contains("Planets") ? "Planet texture resolution, in pixels. Higher values can sharpen orbital views and use more VRAM. Applies only to this tier." :
            path.Contains("GalaxyBackground") ? "Galaxy background texture resolution, in pixels. Higher values can sharpen the sky and use more VRAM. Applies only to this tier." :
            "Texture resolution for this feature and tier, in pixels. Higher values increase memory use; the visible effect depends on the feature.";
        if(field.StartsWith("Matrix"))return "HUD colour transform coefficients. Changes can also affect other interface colours; preserve custom HUD/mod settings.";
        if(field.StartsWith('@'))return "Configuration metadata retained with the preset; not a rendering quality control.";
        return "Advanced engine parameter. Its exact effect and valid range are not verified; the source and XML path identify the feature it belongs to.";
    }

    static readonly Dictionary<string,string> Descriptions=new()
    {
        ["AAMode"]="Smooths jagged edges using post-process AA. FXAA can soften the image; SMAA is another edge filter. Neither eliminates all shimmering.",
        ["HMDRenderTargetMultiplier"]="Scales the headset render target. Higher values can sharpen VR but increase GPU work; 1.0× uses the runtime's recommended target.",
        ["SSAAMultiplier"]="Scales internal rendering. Above 1.0× can reduce aliasing at substantial GPU cost; below 1.0× trades detail for speed. Also used with upscaling.",
        ["UpscalingQuality"]="Chooses normal rendering, CAS sharpening or FSR 1.0 spatial upscaling. Works with supersampling; does not select the VR runtime.",
        ["FFXCASIntensity"]="Strength of Contrast Adaptive Sharpening (0–1). A stored value does not prove CAS is active; behaviour depends on the selected upscaler/game version.",
        ["TextureQualityEx"]="Selects texture detail. Higher quality generally needs more VRAM; planet and galaxy texture overrides are separate controls.",
        ["TextureQuality"]="Legacy texture selector retained by some schemas. Its precedence relative to TextureQualityEx is unverified.",
        ["TextureFilterQuality"]="Keeps textures clearer at oblique viewing angles. Higher anisotropic filtering is most noticeable on receding surfaces.",
        ["DirectionalShadowQuality"]="Quality of shadows from directional lighting, such as a star. Higher tiers can improve definition at extra GPU cost.",
        ["SpotShadowQuality"]="Quality of shadows cast by local spotlights. Cost depends on the number of lights and shadowed objects in the scene.",
        ["ShadowQuality"]="Legacy combined shadow quality selector; newer schemas separate directional and spot shadows.",
        ["AOQuality"]="Adds contact shading around creases and nearby objects using ambient occlusion. Higher tiers can improve depth at extra GPU cost.",
        ["DOFEnabled"]="Depth-of-field quality: blurs areas outside the camera's focus. Despite the XML name, this is a quality tier, not a simple toggle.",
        ["BlurEnabled"]="Enables the game's blur effect. It may soften moving imagery; visibility depends on the scene and camera.",
        ["BloomQuality"]="Controls the glow around bright lights and emissive surfaces. Higher tiers change the bloom rendering, not screen brightness.",
        ["EnvmapQuality"]="Selects environment-map reflection quality. This affects reflected scene detail; it is separate from the galaxy sky background.",
        ["MaterialQuality"]="Selects material rendering detail for object surfaces. Exact shader differences depend on the material and game build.",
        ["EnvironmentQuality"]="Selects an environment tier containing several feature settings, including planet and galaxy background textures.",
        ["FXQuality"]="Quality tier for visual effects. Effects and GPU cost vary by scene; this does not change every graphics setting.",
        ["ParticleEffectsQuality"]="Quality tier for particle effects such as smoke, sparks and debris. Busy scenes can increase the cost.",
        ["GalaxyMapQuality"]="Detail used by the galaxy map. This is distinct from the galaxy background texture seen while flying.",
        ["GUIColourQuality"]="Selects a HUD colour definition. Custom colour matrices and HUD mods may change the resulting colours.",
        ["TerrainQuality"]="Planetary terrain rendering tier. Ultra+ is an extra high-detail mode intended for high-end GPUs; assess it separately in VR.",
        ["TerrainLodBlendingQuality"]="Controls blending between terrain levels of detail. Can soften transitions as you approach; higher tiers may cost GPU time.",
        ["SurfaceMaterialQuality"]="Quality of terrain surface materials. Affects nearby ground appearance, separately from orbital planet texture resolution.",
        ["SurfaceSamplerQuality"]="Terrain surface sampling quality. Higher tiers improve surface evaluation; exact sampling counts are not verified.",
        ["GpuSchedulerMultiplier"]="Terrain work budget (0–1). More work can help terrain finish sooner, but competes with frame rendering; test approach smoothness.",
        ["LODDistanceScale"]="Model draw-distance slider (0–1). Higher values retain detail farther away and can increase rendering work.",
        ["TerrainCheckerboardRenderingEnabled"]="Enables the terrain checkerboard rendering path to reduce work, with a potential detail trade-off. If files disagree, precedence is unverified.",
        ["JetConeQuality"]="Quality of stellar jet-cone effects, such as those around neutron stars. Cost is most relevant when those effects are visible.",
        ["VolumetricsQuality"]="Quality of volumetric effects such as illuminated fog. Higher tiers can be expensive in effect-heavy scenes.",
        ["ScreenWidth"]="Flat-screen or mirror-window width in pixels. Does not set the headset's per-eye render resolution.",
        ["ScreenHeight"]="Flat-screen or mirror-window height in pixels. Does not set the headset's per-eye render resolution.",
        ["FullScreen"]="Chooses windowed, exclusive fullscreen or borderless display for the desktop game window.",
        ["VSync"]="Synchronises desktop presentation to the display to reduce tearing. The VR compositor manages headset presentation separately.",
        ["LimitFrameRate"]="Enables the game's frame-rate cap. The cap value is stored separately; headset refresh is managed outside this setting.",
        ["MaxFramesPerSecond"]="Requested frame-rate limit in FPS when the limiter is enabled. This is not the headset refresh rate.",
        ["DX11_RefreshRateNumerator"]="Desktop refresh-rate numerator. Divide by the denominator for Hz; this is not the Virtual Desktop headset refresh setting.",
        ["DX11_RefreshRateDenominator"]="Desktop refresh-rate denominator. For example, 60000 / 1000 gives 60 Hz. Does not configure headset refresh.",
        ["Adapter"]="Saved graphics-adapter index. Available indices depend on the local hardware; they are not quality levels.",
        ["Monitor"]="Saved desktop monitor index. Available indices depend on connected displays.",
        ["StereoscopicMode"]="Game stereo/headset output mode. Numeric mode mapping is not fully verified; this does not select VDXR or SteamVR.",
        ["IPDAmount"]="Saved stereo separation/IPD adjustment. Its units and effect in the active VR runtime are not verified.",
        ["StereoFocalDistance"]="Focus/convergence parameter for stereoscopic rendering. Its effect in the active VR runtime is not verified.",
        ["FOV"]="Ship-camera field of view, in degrees. Headset field of view is normally supplied by the VR runtime.",
        ["HumanoidFOV"]="On-foot camera field of view, in degrees. Wider views show more of the scene.",
        ["GammaOffset"]="Adjusts the game's midtone brightness. Use the in-game calibration image when choosing a value.",
        ["DisableGuiEffects"]="Disables interface visual effects. This does not change the HUD colour matrix.",
        ["VehicleMotionBlackout"]="Vehicle-camera comfort option that masks extreme movement; intended to reduce discomfort.",
        ["VehicleMaintainHorizonCamera"]="Keeps the vehicle camera aligned with the horizon instead of following all vehicle rotation.",
        ["DisableCameraShake"]="Disables camera shake where supported, making the view steadier.",
        ["HeadBobScale"]="Scales camera bob from movement. Lower values reduce the motion; this is not render resolution.",
        ["HighResScreenCapAntiAlias"]="Anti-aliasing option for high-resolution screenshots. Separate from normal gameplay AA.",
        ["HighResScreenCapScale"]="Resolution multiplier for high-resolution screenshots. Can greatly increase capture memory and processing requirements.",
        ["LocalisationName"]="Game interface label for this quality tier. Shown in readable form; the original localisation identifier is preserved.",
        ["ShaderWarming"]="Prepares shaders on startup to reduce compilation work during play. Startup time and behaviour depend on the game and driver cache.",
        ["PresentInterval"]="Desktop presentation interval. Interacts with vertical sync; its current engine semantics are not independently verified.",
        ["AMDCrashFix"]="Legacy compatibility flag for AMD-related crashes. Its effect in the current build is unverified; not a quality control.",
        ["StencilDump"]="Internal stencil-debugging flag. Its current behaviour is unverified; not a quality control.",
        ["Version"]="Configuration format version. Retained as file metadata; not a rendering quality level.",
        ["NebulasCount"]="Nebula count in this galaxy-map tier. Higher counts may add map detail and processing cost.",
        ["NebulasInBackgroundCount"]="Nebula count requested for the background by this tier. Actual visibility depends on position and rendering limits.",
        ["LowResNebulasCount"]="Low-resolution nebula allowance in this tier. Increasing it can add background detail and rendering work.",
        ["HighResNebulasCount"]="High-resolution nebula allowance in this tier. Increasing it can use more memory and rendering work.",
        ["StarInstanceCount"]="Star instance budget for this feature and tier. Higher values can show more stars at additional rendering cost.",
        ["PresetName"]="Game configuration preset identifier. This is separate from the name of a preset in this app's library.",
        ["Planets"]="Effective planet texture size at the selected environment tier. Higher values can sharpen orbital views and use more VRAM.",
        ["GalaxyBackground"]="Effective galaxy background texture size at the selected environment tier. Higher values can sharpen the sky and use more VRAM.",
        ["WorkPerFrame"]="Per-frame work allowance for this feature. Raising it may reduce update delays but competes with frame rendering; engine units are unverified.",
        ["NumMips"]="Number of mip levels in this texture definition. Mips provide smaller texture versions for distant views; retain a compatible texture configuration.",
        ["PerformanceQualitySetting"]="Internal performance selector. Its exact menu mapping is not verified; preserve the saved value unless testing a known change.",
        ["ResolutionSetting"]="Internal resolution selector. Its mapping is not verified; desktop dimensions and VR render scale are separate fields."
    };
}
