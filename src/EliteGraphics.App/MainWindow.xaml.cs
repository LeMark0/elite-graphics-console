using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using EliteGraphics.Core;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace EliteGraphics.App;

public sealed class LocalSettings
{
    public AppPaths Paths { get; set; } = new();
    public string LastAppliedFingerprint { get; set; } = "";
    public List<string>? ComparisonIds { get; set; }
}

public partial class MainWindow : Window
{
    readonly ProfileStore store;readonly BenchmarkStore benchmarks;readonly ApplyService apply;readonly string settingsFile;
    LocalSettings settings;ProfileRevision? selected;FileSet? selectedFiles;byte[] selectedDefinitions=[];
    string liveFingerprint="",filter="ALL";bool loading;GpuRecorder? recorder;BenchmarkRun? selectedRun;
    List<GpuSample> chartSamples=[];IntPtr handle;
    IReadOnlyList<SettingEntry> inventory=[];
    string inventoryContext="";
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd,int id);
    static bool GameRunning()=>Process.GetProcessesByName("EliteDangerous64").Any()||Process.GetProcessesByName("EliteDangerous").Any();

    public MainWindow(string dataRoot)
    {
        InitializeComponent();foreach(var column in DiffGrid.Columns.OfType<DataGridTextColumn>())column.ElementStyle=(Style)FindResource("CellText");store=new ProfileStore(dataRoot);benchmarks=new BenchmarkStore(dataRoot);apply=new ApplyService(dataRoot,GameRunning);settingsFile=Path.Combine(dataRoot,"settings.json");
        settings=File.Exists(settingsFile)?JsonIO.Load<LocalSettings>(settingsFile):new LocalSettings();
        if(settings.Paths.Game.Length==0)settings.Paths.Game=DiscoverGame();
        ModeBox.ItemsSource=new[]{"VR","FLAT"};PlanetBox.ItemsSource=new[]{512,1024,2048,2560,4096};GalaxyBox.ItemsSource=new[]{512,1024,2048,2560,4096};
        EnvironmentBox.ItemsSource=new[]{"Low","Medium","High","Ultra"};TerrainBox.ItemsSource=new[]{"Low","Medium","High","Ultra","Ultra+"};AoBox.ItemsSource=new[]{"Off","Low","Medium","High"};VolumetricBox.ItemsSource=new[]{"Low","Medium","High","Ultra"};
        Loaded+=(_,_)=>Guard(()=>{if(!store.HasRevisions&&Directory.Exists(settings.Paths.Graphics))Seed();RefreshLibrary();RefreshLive();RefreshRuns();if(apply.List().Any(x=>x.State=="Prepared"))StatusText.Text="An interrupted apply was found. Use Restore previous apply to recover before continuing.";});
        SourceInitialized+=(_,_)=>{handle=new WindowInteropHelper(this).Handle;HwndSource.FromHwnd(handle)?.AddHook(HotkeyHook);var capture=RegisterHotKey(handle,1,0x4006,0x42);var marker=RegisterHotKey(handle,2,0x4006,0x4D);if(!capture||!marker)StatusText.Text="Some benchmark hotkeys are unavailable. Use the on-screen buttons.";};
        Closing+=(_,e)=>{if(!ConfirmLeave()){e.Cancel=true;return;}recorder?.Dispose();UnregisterHotKey(handle,1);UnregisterHotKey(handle,2);};
        Activated+=(_,_)=>{if(IsLoaded&&!loading)Guard(RefreshLive);};
        WireEditor();MainTabs.SelectionChanged+=(_,e)=>{if(e.Source==MainTabs)Guard(UpdateInventory);};SaveSettings();
    }
    IntPtr HotkeyHook(IntPtr hwnd,int msg,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(msg==0x0312){handled=true;if(wParam.ToInt32()==1)Guard(ToggleRecording);if(wParam.ToInt32()==2)Guard(()=>recorder?.Marker("Hotkey checkpoint"));}return IntPtr.Zero;
    }
    static string DiscoverGame()
    {
        var roots=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var steam=Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam","SteamPath",null) as string;
        if(steam!=null)
        {
            roots.Add(steam);var libraries=Path.Combine(steam,"steamapps","libraryfolders.vdf");
            if(File.Exists(libraries))foreach(Match match in Regex.Matches(File.ReadAllText(libraries),"\"path\"\\s+\"([^\"]+)\""))roots.Add(match.Groups[1].Value.Replace("\\\\","\\"));
        }
        foreach(var root in roots){var products=Path.Combine(root,"steamapps","common","Elite Dangerous","Products");if(Directory.Exists(products)){var game=Directory.GetDirectories(products).FirstOrDefault(x=>File.Exists(Path.Combine(x,"GraphicsConfiguration.xml")));if(game!=null)return game;}}
        return "";
    }
    void Seed()
    {
        if(GameRunning())throw new InvalidOperationException("Exit Elite normally, then reopen this app to capture a stable baseline.");
        var files=FileSet.Read(settings.Paths.Graphics);var mode=GraphicsModel.General(files,"StereoscopicMode")=="0"?"FLAT":"VR";var defs=settings.Paths.ReadDefinitions();
        var baseline=store.Save(mode+" Baseline",mode,files,defs,settings.Paths.Build(),"Byte-exact first capture. Effective selections inferred; runtime/visual equivalence awaits in-game confirmation.",protect:true);
        settings.LastAppliedFingerprint=files.Fingerprint();
        if(defs.Length>0)
        {
            var enhanced=files.Clone();GraphicsModel.SetTexture(enhanced,defs,"Planets",4096);GraphicsModel.SetTexture(enhanced,defs,"GalaxyBackground",4096);
            store.Save(mode+" Hi-Res 4096",mode,enhanced,defs,baseline.GameBuild,"Untested candidate: 4096 planets and galaxy background at selected tiers. All other graphics unchanged.",baseline.Id);
        }
        if(Directory.Exists(settings.Paths.Legacy))foreach(var name in new[]{"VR","FLAT"})
        {
            var path=Path.Combine(settings.Paths.Legacy,name,"Graphics");if(!Directory.Exists(path))continue;
            var old=FileSet.Read(path);var historical=store.Save(name+" Historical",name,old,defs,"Unknown","Imported from the existing batch switcher's saved folder. Original folder unchanged.",historical:true);
            if(defs.Length>0&&name=="FLAT")store.Save("FLAT Migrated candidate","FLAT",GraphicsModel.Migrate(old,files),defs,baseline.GameBuild,"Historical FLAT values ported onto current Custom schema; current-only values retained. Untested; not a baseline.",historical.Id);
        }
        SaveSettings();
    }
    void SaveSettings()=>JsonIO.Save(settingsFile,settings);
    void Guard(Action action){try{action();}catch(Exception ex){StatusText.Text=ex.Message;MessageBox.Show(this,ex.Message,"Elite Graphics Console",MessageBoxButton.OK,MessageBoxImage.Warning);}}
    void NoRecording(){if(recorder!=null)throw new InvalidOperationException("Stop benchmark capture before changing graphics profiles.");}
    void NeedSelection(){if(selected==null||selectedFiles==null)throw new InvalidOperationException("Select a profile first.");}
    void RefreshLibrary(string? pick=null)
    {
        loading=true;var all=store.List();var heads=store.Heads();var choice=pick??selected?.Id;ProfilesList.ItemsSource=heads.Where(x=>filter=="ALL"||x.Mode==filter).OrderByDescending(x=>x.Protected).ThenBy(x=>x.Historical).ThenBy(x=>x.Mode=="VR"?0:1).ThenByDescending(x=>x.Created).ToList();ComparePresets.ItemsSource=all.OrderByDescending(x=>x.Protected).ThenBy(x=>x.Historical).ThenByDescending(x=>x.Created).ToList();
        var compareIds=settings.ComparisonIds??all.Where(x=>x.Mode=="VR"&&!x.Historical).OrderByDescending(x=>x.Protected).ThenByDescending(x=>x.Created).Take(4).Select(x=>x.Id).ToList(); foreach(var profile in all.Where(x=>compareIds.Contains(x.Id)))ComparePresets.SelectedItems.Add(profile); loading=false;
        ProfilesList.SelectedItem=((IEnumerable<ProfileRevision>)ProfilesList.ItemsSource).FirstOrDefault(x=>x.Id==choice)??((IEnumerable<ProfileRevision>)ProfilesList.ItemsSource).FirstOrDefault(x=>x.Protected)??((IEnumerable<ProfileRevision>)ProfilesList.ItemsSource).FirstOrDefault();
    }
    FileSet? checkedGameFiles;
    string checkedGameDetail="Not checked yet.";
    void RefreshLive()
    {
        checkedGameFiles=null;
        try
        {
            if(!Directory.Exists(settings.Paths.Graphics))throw new IOException("Graphics folder not found. Set it in Paths.");
            checkedGameFiles=FileSet.Read(settings.Paths.Graphics);checkedGameFiles.Validate();liveFingerprint=checkedGameFiles.Fingerprint();
            var drift=settings.LastAppliedFingerprint.Length>0&&settings.LastAppliedFingerprint!=liveFingerprint;
            checkedGameDetail=(GameRunning()?"Elite is running — saved files may differ from the running session.":"Elite is closed — saved files will be used on next launch.")+(drift?" External changes detected.":"")+" Checked "+DateTime.Now.ToString("HH:mm:ss")+".";
        }
        catch(Exception ex){checkedGameFiles=null;checkedGameDetail="Unable to read saved game settings: "+ex.Message;}
        UpdateApplyHeader();UpdateInventory();
    }
    void UpdateApplyHeader()
    {
        if(AppliedLabel==null)return;
        var known=selected!=null&&checkedGameFiles!=null;
        var same=known&&selected!.Hashes.Count==checkedGameFiles!.Count&&selected.Hashes.All(x=>checkedGameFiles.TryGetValue(x.Key,out var bytes)&&FileSet.Hash(bytes)==x.Value);
        var dirty=IsDirty;
        AppliedLabel.Text=currentWorkspace?(dirty?"UNSAVED CHANGES":"CURRENT GAME SETTINGS"):dirty?"UNSAVED CHANGES":!known?"STATUS UNKNOWN":same?"APPLIED":"NOT APPLIED";
        AppliedBadge.Background=new SolidColorBrush(!dirty&&same?Color.FromRgb(20,43,46):Color.FromRgb(52,34,15));
        LiveStatus.Text=(currentWorkspace?(dirty?"Edited current settings — save as a preset to keep or apply changes.":"Loaded current settings — no preset created."):dirty?(same?"Saved revision is applied; your edits are not.":"Your edits are not applied. Update or fork to apply them."):same?"This preset matches the saved game settings.":known?"This preset differs from the saved game settings.":"")+" "+checkedGameDetail;
        HeaderApplyButton.IsEnabled=!currentWorkspace&&known&&!same&&!dirty&&selected?.Historical==false;
        HeaderApplyButton.Content=currentWorkspace?"Save preset to apply":!dirty&&same?"Already applied":"Apply preset";
        HeaderApplyButton.ToolTip=dirty?"Update or fork your changes first.":same?"Saved game files already match this preset.":"Review changes and apply this saved preset with Elite closed.";
    }
    void UpdateInventory()
    {
        if(SettingsGrid==null||settings==null||InlinePending)return;
        var live=false;
        var files=selectedFiles; if(selected!=null&&!loading){try{files=Edited();}catch(ArgumentException){files=draftFiles??selectedFiles;}catch(InvalidDataException){files=draftFiles??selectedFiles;}catch(InvalidOperationException){files=draftFiles??selectedFiles;}}
        if(files==null){terminalRows=[];inventory=[];inventoryContext="Select a preset.";FilterInventory();return;}
        var definitions=live?settings.Paths.ReadDefinitions():selectedDefinitions;
        inventory=SettingsInventory.Build(files,definitions,InspectOlder.IsChecked==true,InspectDefaults.IsChecked==true);
        BuildTerminalRows(files);
        inventoryContext=(live?"Current saved files (not unsaved game-menu values)":selected!.DisplayName+(IsDirty?" · Unsaved draft":""))+" · All controls shown; unavailable/missing menu values cannot be inferred.";
        FilterInventory();
    }
    void FilterInventory(){if(!InlinePending)ShowTerminalRows();}
    void SettingsSearch_Changed(object sender,TextChangedEventArgs e)=>FilterInventory();
    void InventoryOptions_Changed(object sender,RoutedEventArgs e)=>Guard(UpdateInventory);
    void Profile_Selected(object sender,SelectionChangedEventArgs e){if(loading||ProfilesList.SelectedItem is not ProfileRevision p)return;Guard(()=>{if(!ConfirmLeave()){loading=true;ProfilesList.SelectedItem=currentWorkspace?null:selected;loading=false;return;}LoadProfile(p);MainTabs.SelectedItem=ConfigureTab;});}
    void LoadProfile(ProfileRevision profile,FileSet? current=null,byte[]? definitions=null)
    {
        MainTabs.IsEnabled=true;loading=true;terminalRows=[];TerminalNotes.Text="";draftFiles=null;currentWorkspace=current!=null;selected=profile;selectedFiles=current??store.Files(profile.Id);selectedDefinitions=definitions??store.Definitions(profile.Id);
        ProfileTitle.Text=profile.Name;ProfileSubtitle.Text=$"Revision {profile.Number:000} · {profile.Mode} · {profile.Created.ToLocalTime():dd MMM yyyy HH:mm} · {profile.GameBuild} · {(profile.Protected?"Protected original / edit a derivative":profile.Status)}";
        if(currentWorkspace)ProfileSubtitle.Text="Current game settings · captured from saved files · not saved as a preset";
        NameBox.Text=profile.Name;ModeBox.SelectedItem=profile.Mode;NotesBox.Text="";
        string Q(string field)=>GraphicsModel.Quality(selectedFiles,field);
        HmdBox.Text=Q("HMDRenderTargetMultiplier");SsBox.Text=Q("SSAAMultiplier");LodBox.Text=Q("LODDistanceScale");WorkBox.Text=Q("GpuSchedulerMultiplier");ShadowBox.Text=Q("DirectionalShadowQuality");SpotBox.Text=Q("SpotShadowQuality");
        EnvironmentBox.SelectedIndex=ParseInt(Q("EnvironmentQuality"));TerrainBox.SelectedIndex=ParseInt(Q("TerrainQuality"));AoBox.SelectedIndex=ParseInt(Q("AOQuality"));VolumetricBox.SelectedIndex=ParseInt(Q("VolumetricsQuality"));
        WidthBox.Text=GraphicsModel.Display(selectedFiles,"ScreenWidth");HeightBox.Text=GraphicsModel.Display(selectedFiles,"ScreenHeight");FpsBox.Text=GraphicsModel.Display(selectedFiles,"MaxFramesPerSecond");VsyncBox.IsChecked=GraphicsModel.Display(selectedFiles,"VSync")=="true";LimitBox.IsChecked=GraphicsModel.Display(selectedFiles,"LimitFrameRate")=="true";
        HmdCard.Text=Short(HmdBox.Text)+"×";SsCard.Text=Short(SsBox.Text)+"×";
        if(selectedDefinitions.Length>0)
        {
            var planet=GraphicsModel.Texture(selectedFiles,selectedDefinitions,"Planets");var galaxy=GraphicsModel.Texture(selectedFiles,selectedDefinitions,"GalaxyBackground");PlanetCard.Text=planet.Value;PlanetSource.Text=planet.Source;GalaxyCard.Text=galaxy.Value;GalaxySource.Text=galaxy.Source;PlanetBox.SelectedItem=ParseInt(planet.Value);GalaxyBox.SelectedItem=ParseInt(galaxy.Value);
        }
        else{PlanetCard.Text=GalaxyCard.Text="Unknown";PlanetSource.Text=GalaxySource.Text="Locate game definitions";PlanetBox.SelectedIndex=GalaxyBox.SelectedIndex=-1;}
        WarningsText.Text=string.Join("\n",GraphicsModel.Warnings(selectedFiles,selectedDefinitions))+"\n"+profile.Notes;
        editorKey=EditorKey();loading=false;RefreshDirty();RefreshLive();UpdateComparison();
    }
    static int ParseInt(string text)=>int.TryParse(text,NumberStyles.Integer,CultureInfo.InvariantCulture,out var n)?n:-1;
    static string Short(string text)=>double.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out var n)?n.ToString("0.##",CultureInfo.InvariantCulture):text;
    static double Numeric(TextBox input,double min,double max,bool integer=false)
    {
        if(!double.TryParse(input.Text,NumberStyles.Float,CultureInfo.InvariantCulture,out var value)||!double.IsFinite(value)||value<min||value>max||(integer&&value!=Math.Truncate(value)))throw new ArgumentException($"{input.Name.Replace("Box","")} must be between {min} and {max}"+(integer?" (whole numbers).":". Use a decimal point."));return value;
    }
    FileSet Edited()
    {
        NeedSelection();if(InlinePending&&!inlineCommitting)throw new InvalidOperationException("Finish the inline edit with Enter, or cancel with Esc.");if(EditorKey()==editorKey)return (draftFiles??selectedFiles!).Clone();if(selected!.Historical)throw new InvalidOperationException("Migrate a historical profile to the current schema before editing.");
        var data=(draftFiles??selectedFiles!).Clone();string active=GraphicsModel.ActiveFile(data);
        void SetNumber(string file,string field,double value){var current=XmlIO.Read(data[file]).Root?.Element(field)?.Value;if(double.TryParse(current,NumberStyles.Float,CultureInfo.InvariantCulture,out var existing)&&existing==value)return;GraphicsModel.Set(data,file,field,value.ToString("0.######",CultureInfo.InvariantCulture));}
        if(EnvironmentBox.SelectedIndex<0||TerrainBox.SelectedIndex<0||AoBox.SelectedIndex<0||VolumetricBox.SelectedIndex<0)throw new ArgumentException("Select valid quality tiers.");
        SetNumber(active,"EnvironmentQuality",EnvironmentBox.SelectedIndex);SetNumber(active,"TerrainQuality",TerrainBox.SelectedIndex);SetNumber(active,"AOQuality",AoBox.SelectedIndex);SetNumber(active,"VolumetricsQuality",VolumetricBox.SelectedIndex);
        SetNumber(active,"HMDRenderTargetMultiplier",Numeric(HmdBox,.5,2));SetNumber(active,"SSAAMultiplier",Numeric(SsBox,.5,2));SetNumber(active,"LODDistanceScale",Numeric(LodBox,0,1));SetNumber(active,"GpuSchedulerMultiplier",Numeric(WorkBox,0,1));SetNumber(active,"DirectionalShadowQuality",Numeric(ShadowBox,0,4,true));SetNumber(active,"SpotShadowQuality",Numeric(SpotBox,0,4,true));
        var mode=ModeBox.SelectedItem as string??throw new ArgumentException("Select VR or FLAT.");if(mode!=(GraphicsModel.General(data,"StereoscopicMode")=="0"?"FLAT":"VR"))SetNumber("Settings.xml","StereoscopicMode",mode=="VR"?3:0);
        SetNumber("DisplaySettings.xml","ScreenWidth",Numeric(WidthBox,640,16384,true));SetNumber("DisplaySettings.xml","ScreenHeight",Numeric(HeightBox,480,16384,true));SetNumber("DisplaySettings.xml","MaxFramesPerSecond",Numeric(FpsBox,20,1000,true));
        foreach(var item in new[]{("VSync",VsyncBox.IsChecked==true),("LimitFrameRate",LimitBox.IsChecked==true)})if(GraphicsModel.Display(data,item.Item1)!=(item.Item2?"true":"false"))GraphicsModel.Set(data,"DisplaySettings.xml",item.Item1,item.Item2?"true":"false");
        if(PlanetBox.SelectedItem is not int planet||GalaxyBox.SelectedItem is not int galaxy)throw new ArgumentException("Select planet and background texture sizes.");
        if(GraphicsModel.Texture(data,selectedDefinitions,"Planets").Value!=planet.ToString(CultureInfo.InvariantCulture))GraphicsModel.SetTexture(data,selectedDefinitions,"Planets",planet);
        if(GraphicsModel.Texture(data,selectedDefinitions,"GalaxyBackground").Value!=galaxy.ToString(CultureInfo.InvariantCulture))GraphicsModel.SetTexture(data,selectedDefinitions,"GalaxyBackground",galaxy);
        return data;
    }
    void Import_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();if(!ConfirmLeave())return;var dialog=new OpenFolderDialog{Title="Select a graphics folder containing Settings.xml"};if(dialog.ShowDialog(this)!=true)return;var path=dialog.FolderName;if(Directory.Exists(Path.Combine(path,"Graphics")))path=Path.Combine(path,"Graphics");var files=FileSet.Read(path);var mode=GraphicsModel.General(files,"StereoscopicMode")=="0"?"FLAT":"VR";var name=Prompt("Import historical settings","Profile name",mode+" Imported");if(name==null)return;var p=store.Save(name,mode,files,settings.Paths.ReadDefinitions(),"Unknown","Imported file snapshot. Migrate before applying.",historical:true);FinishSave(p);});
    void Migrate_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();NeedSavedPreset();if(!ConfirmLeave())return;if(GameRunning())throw new InvalidOperationException("Close Elite before using its current files as a migration template.");var current=FileSet.Read(settings.Paths.Graphics);var files=GraphicsModel.Migrate(selectedFiles!,current);var p=store.Save(selected!.Mode+" Migrated candidate",selected.Mode,files,settings.Paths.ReadDefinitions(),settings.Paths.Build(),"Known older values copied onto the current schema. Fields introduced by the current version retain current values. Untested.",selected.Id);FinishSave(p);});
    void Export_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NeedSavedPreset();_=store.Files(selected!.Id);_=store.Definitions(selected.Id);var dialog=new SaveFileDialog{Title="Export this revision",FileName="elite-profile-"+selected.Id[..8]+".zip",Filter="ZIP archive|*.zip"};if(dialog.ShowDialog(this)!=true)return;ZipFile.CreateFromDirectory(Path.Combine(store.Profiles,selected.Id),dialog.FileName);StatusText.Text="Revision exported to "+dialog.FileName;});
    void Apply_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NoRecording();NeedSavedPreset();if(selected!.Historical)throw new InvalidOperationException("Historical snapshots must be migrated first.");if(GameRunning())throw new InvalidOperationException("Close Elite Dangerous first.");
        if(IsDirty||Edited().Fingerprint()!=selectedFiles!.Fingerprint())throw new InvalidOperationException("The editor contains unsaved changes. Update or fork this preset before applying it.");
        var current=FileSet.Read(settings.Paths.Graphics);var expected=current.Fingerprint();var validated=store.Files(selected.Id);var defs=store.Definitions(selected.Id);
        var changes=GraphicsModel.Diff(current,validated);
        if(!ReviewApply(changes))return;
        apply.Apply(settings.Paths.Graphics,validated,expected,defs,settings.Paths.ReadDefinitions(),selected.GameBuild,settings.Paths.Build());settings.LastAppliedFingerprint=FileSet.Read(settings.Paths.Graphics).Fingerprint();SaveSettings();RefreshLive();StatusText.Text="Applied and verified. Restart Elite through your normal launch route.";
    });
    void Restore_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();if(!Review("Restore previous graphics","Restore the exact saved files from the last apply. External edits to managed files will pause restoration rather than be overwritten.","Restore saved files"))return;if(apply.List().Any(x=>x.State=="Prepared"))apply.RecoverPending(settings.Paths.Graphics);else apply.RestorePrevious(settings.Paths.Graphics,FileSet.Read(settings.Paths.Graphics).Fingerprint());settings.LastAppliedFingerprint=FileSet.Read(settings.Paths.Graphics).Fingerprint();SaveSettings();RefreshLive();StatusText.Text="Previous graphics restored and verified.";});
    static string DiffText(IReadOnlyList<SettingDiff> differences)=>differences.Count==0?"No changes.":string.Join("\n\n",differences.Select(x=>$"{x.File}\n{x.Path}\n  {x.Before} → {x.After}"));
    void UpdateComparison()
    {
        if(settings==null||ComparePresets==null||DiffGrid==null)return;
        var chosen=ComparePresets.SelectedItems.Cast<ProfileRevision>().OrderByDescending(x=>x.Protected).ThenBy(x=>x.Created).ToList();
        DiffGrid.Columns.Clear();DiffGrid.ItemsSource=null;
        if(chosen.Count<2){CompareSummary.Text="Select at least two presets. Click a preset to add or remove its column; no Ctrl key needed.";return;}
        var sources=chosen.Select(x=>new ComparisonSource(x.DisplayName,store.Files(x.Id),store.Definitions(x.Id))).ToList();
        bool raw=RawComparison.IsChecked==true;
        var rows=PresetComparison.Build(sources,raw,OnlyDifferences.IsChecked==true);
        var label=new FrameworkElementFactory(typeof(StackPanel));
        var title=new FrameworkElementFactory(typeof(TextBlock));title.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Setting"));title.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);label.AppendChild(title);
        var origin=new FrameworkElementFactory(typeof(TextBlock));origin.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding("Source"));origin.SetValue(TextBlock.ForegroundProperty,new SolidColorBrush(Color.FromRgb(184,170,152)));origin.SetValue(TextBlock.FontSizeProperty,11d);origin.SetValue(TextBlock.TextWrappingProperty,TextWrapping.Wrap);label.AppendChild(origin);label.SetValue(FrameworkElement.MarginProperty,new Thickness(8));
        DiffGrid.Columns.Add(new DataGridTemplateColumn{Header="Setting / source",Width=raw?390:255,CellTemplate=new DataTemplate{VisualTree=label}});
        for(int i=0;i<chosen.Count;i++)
        {
            var value=new FrameworkElementFactory(typeof(TextBlock));value.SetBinding(TextBlock.TextProperty,new System.Windows.Data.Binding($"Cells[{i}].Value"));
            var style=new Style(typeof(TextBlock));style.Setters.Add(new Setter(TextBlock.TextWrappingProperty,TextWrapping.Wrap));style.Setters.Add(new Setter(FrameworkElement.MarginProperty,new Thickness(10)));
            var changed=new DataTrigger{Binding=new System.Windows.Data.Binding($"Cells[{i}].Different"),Value=true};changed.Setters.Add(new Setter(TextBlock.ForegroundProperty,new SolidColorBrush(Color.FromRgb(255,150,0))));changed.Setters.Add(new Setter(TextBlock.FontWeightProperty,FontWeights.SemiBold));style.Triggers.Add(changed);value.SetValue(FrameworkElement.StyleProperty,style);
            DiffGrid.Columns.Add(new DataGridTemplateColumn{Header=new TextBlock{Text=chosen[i].Name+"\n"+chosen[i].Mode+$" · r{chosen[i].Number:000} · "+chosen[i].Id[..6],TextWrapping=TextWrapping.Wrap,MaxWidth=190},Width=chosen.Count<=4?new DataGridLength(1,DataGridLengthUnitType.Star):new DataGridLength(205),MinWidth=160,CellTemplate=new DataTemplate{VisualTree=value}});
        }
        DiffGrid.ItemsSource=rows;
        CompareSummary.Text=$"{chosen.Count} presets · {rows.Count} rows · Amber values differ from the first column ({chosen[0].Name}).\n"+
            (raw?"Raw semantic XML comparison includes older schemas and all overrides; absent means no stored entry. Formatting-only changes are omitted.":"Resolved view: active Custom + general/display settings + inferred planet/background values. Use Raw XML for every override and exact paths.");
        if(chosen.Select(x=>x.GameBuild).Distinct().Count()>1)CompareSummary.Text+="\nMixed game builds: each preset uses its own captured definitions.";
    }
    void Compare_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading||settings==null)return;
        Guard(()=>{settings.ComparisonIds=ComparePresets.SelectedItems.Cast<ProfileRevision>().Select(x=>x.Id).ToList();SaveSettings();UpdateComparison();});
    }
    void CompareOptions_Changed(object sender,RoutedEventArgs e){if(!loading&&settings!=null)Guard(UpdateComparison);}
    void SelectVrComparisons_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        loading=true;ComparePresets.SelectedItems.Clear();foreach(var p in ComparePresets.Items.Cast<ProfileRevision>().Where(x=>x.Mode=="VR"&&!x.Historical))ComparePresets.SelectedItems.Add(p);loading=false;
        settings.ComparisonIds=ComparePresets.SelectedItems.Cast<ProfileRevision>().Select(x=>x.Id).ToList();SaveSettings();UpdateComparison();
    });
    void ClearComparisons_Click(object sender,RoutedEventArgs e)=>ComparePresets.SelectedItems.Clear();
    void Filter_Click(object sender,RoutedEventArgs e){if(!ConfirmLeave())return;if(selected!=null&&!currentWorkspace)LoadProfile(selected);filter=((Button)sender).Tag.ToString()!;Guard(()=>RefreshLibrary());}
    void Refresh_Click(object sender,RoutedEventArgs e)=>Guard(RefreshLive);
    void Paths_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NoRecording();var window=Dialog("Installation paths",730,460);var stack=new StackPanel{Margin=new Thickness(24)};window.Content=stack;
        var boxes=new List<TextBox>();foreach(var pair in new[]{("Elite user Graphics folder",settings.Paths.Graphics),("Game product folder (contains GraphicsConfiguration.xml)",settings.Paths.Game),("Old switcher's presets folder (optional)",settings.Paths.Legacy)}){stack.Children.Add(new TextBlock{Text=pair.Item1});var box=new TextBox{Text=pair.Item2};boxes.Add(box);stack.Children.Add(box);}
        stack.Children.Add(new TextBlock{Text="Saving paths does not move or change game files. Existing revisions retain their captured definitions.",Foreground=Brushes.LightSlateGray});var save=new Button{Content="Save paths",Margin=new Thickness(0,20,0,0),HorizontalAlignment=HorizontalAlignment.Right};save.Click+=(_,_)=>{if(!Directory.Exists(boxes[0].Text)||!File.Exists(Path.Combine(boxes[1].Text,"GraphicsConfiguration.xml"))){MessageBox.Show(window,"Select existing Graphics and game product directories.");return;}settings.Paths.Graphics=Path.GetFullPath(boxes[0].Text);settings.Paths.Game=Path.GetFullPath(boxes[1].Text);settings.Paths.Legacy=boxes[2].Text;settings.LastAppliedFingerprint="";SaveSettings();window.DialogResult=true;};stack.Children.Add(save);window.ShowDialog();RefreshLive();
    });
    Window Dialog(string title,double width,double height)=>new(){Title=title,Owner=this,Width=width,Height=height,MinWidth=width,MinHeight=height,WindowStartupLocation=WindowStartupLocation.CenterOwner,ShowInTaskbar=false};
    string? Prompt(string title,string label,string value){var window=Dialog(title,540,235);var stack=new StackPanel{Margin=new Thickness(24)};window.Content=stack;stack.Children.Add(new TextBlock{Text=label});var box=new TextBox{Text=value};stack.Children.Add(box);var save=new Button{Content="Save",HorizontalAlignment=HorizontalAlignment.Right,IsDefault=true};save.Click+=(_,_)=>{if(!string.IsNullOrWhiteSpace(box.Text))window.DialogResult=true;};stack.Children.Add(save);return window.ShowDialog()==true?box.Text:null;}
    bool Review(string title,string text,string verb){var window=Dialog(title,900,660);var grid=new Grid{Margin=new Thickness(22)};grid.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});window.Content=grid;var box=new TextBox{Text=text,IsReadOnly=true,VerticalContentAlignment=VerticalAlignment.Top,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,FontFamily=new FontFamily("Consolas")};grid.Children.Add(box);var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetRow(buttons,1);grid.Children.Add(buttons);var cancel=new Button{Content="Cancel",IsCancel=true};buttons.Children.Add(cancel);var go=new Button{Content=verb,Style=(Style)FindResource("Primary")};go.Click+=(_,_)=>window.DialogResult=true;buttons.Children.Add(go);return window.ShowDialog()==true;}
    void RefreshRuns(string? id=null){var all=benchmarks.List();RunsList.ItemsSource=all;RunCompareBox.ItemsSource=all;RunsList.SelectedItem=all.FirstOrDefault(x=>x.Id==(id??selectedRun?.Id))??all.FirstOrDefault();}
    void Run_Selected(object sender,SelectionChangedEventArgs e)
    {
        if(RunsList.SelectedItem is not BenchmarkRun run)return;Guard(()=>{selectedRun=run;ScenarioBox.Text=run.Scenario;RuntimeBox.Text=run.RuntimeNotes;OverlayBox.Text=run.OverlayObservations;VisualBox.Text=run.VisualNotes;chartSamples=benchmarks.Samples(run.Id);DrawChart();UpdateRunSummary();});
    }
    void ToggleRecording()
    {
        if(recorder!=null){var id=recorder.Run.Id;recorder.Dispose();recorder=null;RecordButton.Content="Start capture  Ctrl+Shift+B";RecordingText.Text="Capture saved. Add overlay observations and visual notes below.";RefreshRuns(id);return;}
        NeedSavedPreset();var live=FileSet.Read(settings.Paths.Graphics);if(selected!.Hashes.Count!=live.Count||selected.Hashes.Any(x=>!live.TryGetValue(x.Key,out var b)||FileSet.Hash(b)!=x.Value))throw new InvalidOperationException("Selected revision does not match live graphics. Apply it with Elite closed, or capture the current files, before recording.");
        var run=new BenchmarkRun{RevisionId=selected.Id,ProfileName=selected.DisplayName,Mode=selected.Mode,Scenario=ScenarioBox.Text,GameBuild=settings.Paths.Build(),ActualHashes=live.Hashes(),RuntimeNotes=RuntimeBox.Text,GameRunningAtStart=GameRunning()};
        var runtime=Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1","ActiveRuntime",null) as string;run.RuntimeNotes+="\nWindows OpenXR registration at capture: "+(runtime??"Unknown")+" (registration alone does not prove runtime in use).";
        try{using var p=Process.Start(new ProcessStartInfo("nvidia-smi","--query-gpu=name,driver_version,memory.total --format=csv,noheader -i 0"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true});if(p!=null){if(p.WaitForExit(3000))run.Hardware=p.StandardOutput.ReadToEnd().Trim();else p.Kill();}}catch(System.ComponentModel.Win32Exception){}
        recorder=new GpuRecorder(benchmarks,run);recorder.Sample+=sample=>Dispatcher.BeginInvoke(()=>{if(recorder!=null&&selectedRun?.Id==recorder.Run.Id){chartSamples.Add(sample);DrawChart();UpdateRunSummary();RecordingText.Text=$"RECORDING  {sample.Seconds:0}s  /  GPU {sample.Utilisation:0}%  /  {sample.MemoryMiB:0} MiB  /  Ctrl+Shift+M marks a checkpoint";}});recorder.Start();RefreshRuns(run.Id);RecordButton.Content="Stop capture  Ctrl+Shift+B";RecordingText.Text="Recording whole-GPU telemetry. "+(run.GameRunningAtStart?"Elite detected.":"Elite was not running at start; this is a telemetry check.");
    }
    void Record_Click(object sender,RoutedEventArgs e)=>Guard(ToggleRecording);
    void Mark_Click(object sender,RoutedEventArgs e)=>Guard(()=>{if(recorder==null)throw new InvalidOperationException("Start a capture first.");recorder.Marker("Checkpoint");StatusText.Text="Checkpoint recorded.";});
    void SaveRun_Click(object sender,RoutedEventArgs e)=>Guard(()=>{if(selectedRun==null)throw new InvalidOperationException("Select a recorded run.");var run=recorder?.Run.Id==selectedRun.Id?recorder.Run:selectedRun;run.Scenario=ScenarioBox.Text;run.RuntimeNotes=RuntimeBox.Text;run.OverlayObservations=OverlayBox.Text;run.VisualNotes=VisualBox.Text;benchmarks.Save(run);RefreshRuns(run.Id);});
    void Screenshot_Click(object sender,RoutedEventArgs e)=>Guard(()=>{if(selectedRun==null)throw new InvalidOperationException("Select a run first.");var dialog=new OpenFileDialog{Filter="Screenshots|*.png;*.jpg;*.jpeg"};if(dialog.ShowDialog(this)!=true)return;var folder=Path.Combine(benchmarks.Folder(selectedRun.Id),"screenshots");Directory.CreateDirectory(folder);File.Copy(dialog.FileName,Path.Combine(folder,Guid.NewGuid().ToString("N")+Path.GetExtension(dialog.FileName)));StatusText.Text="Screenshot attached to this run.";});
    void ImportFrames_Click(object sender,RoutedEventArgs e)=>Guard(()=>{if(selectedRun==null)throw new InvalidOperationException("Select a FLAT run first.");if(recorder!=null)throw new InvalidOperationException("Stop capture before importing frame data.");var dialog=new OpenFileDialog{Filter="PresentMon CSV|*.csv"};if(dialog.ShowDialog(this)!=true)return;selectedRun.Frames=PresentMonImport.Read(dialog.FileName,selectedRun.Mode);File.Copy(dialog.FileName,Path.Combine(benchmarks.Folder(selectedRun.Id),"presentmon.csv"),true);benchmarks.Save(selectedRun);UpdateRunSummary();});
    static double? Peak(IEnumerable<GpuSample> samples)=>samples.Where(x=>x.MemoryMiB.HasValue).Select(x=>x.MemoryMiB).DefaultIfEmpty(null).Max();
    void UpdateRunSummary()
    {
        if(selectedRun==null)return;var peak=Peak(chartSamples);RunSummary.Text=$"{chartSamples.Count} GPU samples · Peak memory: {(peak.HasValue?peak.Value.ToString("0",CultureInfo.InvariantCulture)+" MiB":"unavailable")}\n{selectedRun.TelemetryStatus}\n{selectedRun.Hardware}";
        if(selectedRun.Frames is FrameSummary f)RunSummary.Text+=$"\nFLAT · {f.AverageFps:0.0} average FPS · P95 {f.P95Ms:0.00} ms · P99 {f.P99Ms:0.00} ms · {f.HitchesOver50Ms} intervals >50 ms\n{f.Source}";
        RunSummary.Text+=$"\n{selectedRun.Markers.Count} checkpoints · {(selectedRun.Ended.HasValue?"Finished":"Unfinished / recording")}";UpdateRunDelta();
    }
    void DrawChart()
    {
        MemoryChart.Children.Clear();var valid=chartSamples.Where(x=>x.MemoryMiB.HasValue).ToList();double width=MemoryChart.ActualWidth,height=MemoryChart.ActualHeight;if(width<20||height<20||valid.Count==0)return;
        var max=Math.Max(1024,valid.Max(x=>x.MemoryMiB!.Value)*1.1);var seconds=Math.Max(1,valid.Max(x=>x.Seconds));
        for(int i=1;i<4;i++){var y=height*i/4;MemoryChart.Children.Add(new Line{X1=0,X2=width,Y1=y,Y2=y,Stroke=new SolidColorBrush(Color.FromRgb(65,44,24)),StrokeThickness=1});}
        var line=new Polyline{Stroke=new SolidColorBrush(Color.FromRgb(255,150,0)),StrokeThickness=2};foreach(var sample in valid)line.Points.Add(new Point(sample.Seconds/seconds*width,height-10-sample.MemoryMiB!.Value/max*(height-20)));MemoryChart.Children.Add(line);
    }
    void Chart_SizeChanged(object sender,SizeChangedEventArgs e)=>DrawChart();
    void UpdateRunDelta()
    {
        if(selectedRun==null||RunCompareBox.SelectedItem is not BenchmarkRun other){RunDelta.Text="Choose a comparison run to see measured differences.";return;}var a=Peak(benchmarks.Samples(other.Id));var b=Peak(chartSamples);RunDelta.Text=$"Compared with {other.ProfileName}\nPeak whole-GPU memory change: {(a.HasValue&&b.HasValue?(b-a)?.ToString("+0;-0;0",CultureInfo.InvariantCulture)+" MiB":"unavailable")}";
        if(other.Frames is FrameSummary f1&&selectedRun.Frames is FrameSummary f2)RunDelta.Text+=$"\nAverage FPS: {f1.AverageFps:0.0} → {f2.AverageFps:0.0}\nP99 presentation interval: {f1.P99Ms:0.00} → {f2.P99Ms:0.00} ms";
        RunDelta.Text+="\nCompare matching scenarios and runtime settings. VR smoothness/SSW still requires recorded headset observations.";
    }
    void RunCompare_Changed(object sender,SelectionChangedEventArgs e)=>Guard(UpdateRunDelta);
    static void OpenFolder(string path)=>Process.Start(new ProcessStartInfo("explorer.exe"){ArgumentList={path},UseShellExecute=true});
    void LibraryFolder_Click(object sender,RoutedEventArgs e)=>Guard(()=>OpenFolder(store.Root));
    void RunFolder_Click(object sender,RoutedEventArgs e)=>Guard(()=>{if(selectedRun==null)throw new InvalidOperationException("Select a run first.");OpenFolder(benchmarks.Folder(selectedRun.Id));});
    void Advanced_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NoRecording();NeedSelection();if(selected!.Historical)throw new InvalidOperationException("Migrate this historical profile before editing.");
        var draft=Edited();var draftNotes=NotesBox.Text;var window=Dialog("Advanced graphics editor — preset draft",1000,740);var grid=new Grid{Margin=new Thickness(22)};window.Content=grid;
        grid.RowDefinitions.Add(new(){Height=GridLength.Auto});grid.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});grid.RowDefinitions.Add(new(){Height=GridLength.Auto});
        var combo=new ComboBox{ItemsSource=draft.Keys.OrderBy(x=>x).ToList()};grid.Children.Add(combo);
        var text=new TextBox{AcceptsReturn=true,AcceptsTab=true,VerticalContentAlignment=VerticalAlignment.Top,FontFamily=new FontFamily("Consolas"),FontSize=13,HorizontalScrollBarVisibility=ScrollBarVisibility.Auto,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetRow(text,1);grid.Children.Add(text);
        string? previous=null;string original="";void CommitBuffer(){if(previous!=null&&text.Text!=original)draft[previous]=System.Text.Encoding.UTF8.GetBytes(text.Text);}
        combo.SelectionChanged+=(_,_)=>{CommitBuffer();previous=combo.SelectedItem as string;if(previous!=null){original=System.Text.Encoding.UTF8.GetString(draft[previous]);text.Text=original;}};
        combo.SelectedItem=GraphicsModel.ActiveFile(draft);var panel=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};Grid.SetRow(panel,2);grid.Children.Add(panel);
        panel.Children.Add(new TextBlock{Text="XML is validated. Live files stay unchanged until Apply.",VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,20,8),Foreground=Brushes.LightSlateGray});var save=new Button{Content="Use these changes",Style=(Style)FindResource("Primary")};panel.Children.Add(save);
        save.Click+=(_,_)=>{try{CommitBuffer();draft.Validate();_=GraphicsModel.ActiveFile(draft);window.DialogResult=true;AcceptDraft(draft);NotesBox.Text=draftNotes;RefreshDirty();UpdateInventory();}catch(Exception ex){MessageBox.Show(window,ex.Message,"Cannot use draft");}};window.ShowDialog();
    });
}
