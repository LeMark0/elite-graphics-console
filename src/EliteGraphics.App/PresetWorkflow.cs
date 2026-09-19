using System.IO;
using System.Windows;
using System.Windows.Controls;
using EliteGraphics.Core;

namespace EliteGraphics.App;

public partial class MainWindow
{
    FileSet? draftFiles;
    string editorKey="";
    IEnumerable<TextBox> EditorTextBoxes()=>new[]{HmdBox,SsBox,LodBox,WorkBox,ShadowBox,SpotBox,WidthBox,HeightBox,FpsBox,NotesBox};
    IEnumerable<ComboBox> EditorCombos()=>new[]{ModeBox,EnvironmentBox,TerrainBox,AoBox,VolumetricBox,PlanetBox,GalaxyBox};
    string EditorKey()=>System.Text.Json.JsonSerializer.Serialize(new { Text=EditorTextBoxes().Select(x=>x.Text).ToArray(), Choices=EditorCombos().Select(x=>x.SelectedIndex).ToArray(), Vsync=VsyncBox.IsChecked, Limit=LimitBox.IsChecked });
    bool IsDirty=>selected!=null&&(InlinePending||EditorKey()!=editorKey||(draftFiles!=null&&draftFiles.Fingerprint()!=selectedFiles!.Fingerprint()));
    void WireEditor()
    {
        foreach(var box in EditorTextBoxes())box.TextChanged+=(_,_)=>RefreshDirty();
        foreach(var box in EditorCombos())box.SelectionChanged+=(_,_)=>RefreshDirty();
        foreach(var box in new[]{VsyncBox,LimitBox}){box.Checked+=(_,_)=>RefreshDirty();box.Unchecked+=(_,_)=>RefreshDirty();}
    }
    void RefreshDirty()
    {
        if(loading||selected==null)return;
        SettingsSearch.IsEnabled=CategoryBox.IsEnabled=InspectOlder.IsEnabled=InspectDefaults.IsEnabled=!InlinePending;
        UpdateApplyHeader();var dirty=IsDirty;string? error=null;
        try{if(!selected.Historical)_=Edited();}catch(Exception ex){error=ex.Message;}
        var latest=!currentWorkspace&&store.Heads().Any(p=>p.Id==selected.Id);
        SaveCurrentButton.Visibility=currentWorkspace?Visibility.Visible:Visibility.Collapsed;UpdatePresetButton.Visibility=ForkPresetButton.Visibility=currentWorkspace?Visibility.Collapsed:Visibility.Visible;DeletePresetButton.IsEnabled=!currentWorkspace;
        UpdatePresetButton.IsEnabled=dirty&&!selected.Protected&&!selected.Historical&&latest&&error==null;
        ForkPresetButton.IsEnabled=error==null;SaveCurrentButton.IsEnabled=error==null;DiscardButton.IsEnabled=dirty;
        UpdatePresetButton.Visibility=!currentWorkspace&&dirty?Visibility.Visible:Visibility.Collapsed;DiscardButton.Visibility=dirty?Visibility.Visible:Visibility.Collapsed;
        ProfileTitle.Text=selected.Name+(dirty?" *":"");
        DirtyStatus.Text=error??(currentWorkspace?(dirty?"Unsaved changes — save as a preset to keep your edits.":""):dirty?(selected.Protected?"Protected baseline — fork to keep your changes.":!latest?"Earlier revision — fork to keep your changes.":"Unsaved changes — update this preset or fork a new one."):"");
        HmdCard.Text=Short(HmdBox.Text)+"×";SsCard.Text=Short(SsBox.Text)+"×";
        PlanetCard.Text=PlanetBox.SelectedItem?.ToString()??"Unknown";GalaxyCard.Text=GalaxyBox.SelectedItem?.ToString()??"Unknown";
    }
    bool ConfirmLeave()=>!IsDirty||CreateUnsavedDialog().ShowDialog()==true;
    void FinishSave(ProfileRevision profile){currentWorkspace=false;draftFiles=null;editorKey=EditorKey();filter="ALL";RefreshLibrary(profile.Id);MainTabs.SelectedItem=ConfigureTab;}
    void UpdatePreset_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();NeedSavedPreset();if(!IsDirty)return;var files=Edited();if(!ReviewSave(selectedFiles!,files,selected!.Name,(string)ModeBox.SelectedItem,NotesBox.Text,"SAVED REVISION",true))return;FinishSave(store.Update(selected!.Id,(string)ModeBox.SelectedItem,files,selectedDefinitions,selected.GameBuild,NotesBox.Text));});
    void ForkPreset_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();NeedSelection();var name=Prompt("Fork this preset","New preset name",selected!.Name+" Copy");if(name==null)return;var files=selected!.Historical?selectedFiles!:Edited();if(!ReviewSave(selectedFiles!,files,name,(string)ModeBox.SelectedItem,NotesBox.Text,"SOURCE PRESET"))return;FinishSave(store.Fork(name,(string)ModeBox.SelectedItem,files,selectedDefinitions,selected.GameBuild,NotesBox.Text,selected.Id,selected.Historical));});
    void DiscardPreset_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NeedSelection();terminalRows=[];TerminalNotes.Text="";if(currentWorkspace){draftFiles=null;LoadDraftControls(selectedFiles!);NotesBox.Text="";editorKey=EditorKey();RefreshDirty();UpdateInventory();}else LoadProfile(selected!);});
    void HiRes_Click(object sender,RoutedEventArgs e)=>Guard(()=>{NoRecording();NeedSelection();var files=Edited();GraphicsModel.SetTexture(files,selectedDefinitions,"Planets",4096);GraphicsModel.SetTexture(files,selectedDefinitions,"GalaxyBackground",4096);AcceptDraft(files);});
    void History_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NeedSavedPreset();var window=Dialog("Preset history",720,460);var panel=new DockPanel{Margin=new Thickness(24)};window.Content=panel;
        var view=new Button{Content="View selected revision",HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(view,Dock.Bottom);panel.Children.Add(view);
        var list=new ListBox{ItemsSource=store.History(selected!.Id),DisplayMemberPath="DisplayName"};panel.Children.Add(list);list.SelectedIndex=0;
        view.Click+=(_,_)=>Guard(()=>{if(list.SelectedItem is not ProfileRevision revision||!ConfirmLeave())return;window.DialogResult=true;LoadProfile(revision);MainTabs.SelectedItem=ConfigureTab;});window.ShowDialog();
    });
    void LoadDraftControls(FileSet files)
    {
        // Load through the same mapping as saved presets, retaining the immutable source.
        var original=selectedFiles;var draft=draftFiles;loading=true;selectedFiles=files;
        string Q(string field)=>GraphicsModel.Quality(files,field);
        HmdBox.Text=Q("HMDRenderTargetMultiplier");SsBox.Text=Q("SSAAMultiplier");LodBox.Text=Q("LODDistanceScale");WorkBox.Text=Q("GpuSchedulerMultiplier");ShadowBox.Text=Q("DirectionalShadowQuality");SpotBox.Text=Q("SpotShadowQuality");
        EnvironmentBox.SelectedIndex=ParseInt(Q("EnvironmentQuality"));TerrainBox.SelectedIndex=ParseInt(Q("TerrainQuality"));AoBox.SelectedIndex=ParseInt(Q("AOQuality"));VolumetricBox.SelectedIndex=ParseInt(Q("VolumetricsQuality"));
        ModeBox.SelectedItem=GraphicsModel.General(files,"StereoscopicMode")=="0"?"FLAT":"VR";
        WidthBox.Text=GraphicsModel.Display(files,"ScreenWidth");HeightBox.Text=GraphicsModel.Display(files,"ScreenHeight");FpsBox.Text=GraphicsModel.Display(files,"MaxFramesPerSecond");VsyncBox.IsChecked=GraphicsModel.Display(files,"VSync")=="true";LimitBox.IsChecked=GraphicsModel.Display(files,"LimitFrameRate")=="true";
        PlanetBox.SelectedItem=ParseInt(GraphicsModel.Texture(files,selectedDefinitions,"Planets").Value);GalaxyBox.SelectedItem=ParseInt(GraphicsModel.Texture(files,selectedDefinitions,"GalaxyBackground").Value);
        selectedFiles=original;draftFiles=draft;editorKey=EditorKey();loading=false;
    }
    void LoadCurrent_Click(object sender,RoutedEventArgs e)=>Guard(LoadCurrentState);
    void NewPreset_Click(object sender,RoutedEventArgs e)=>CreatePreset(false);
    void CreatePreset(bool fromGame)=>Guard(()=>
    {
        NoRecording();var window=Dialog(fromGame?"Load current game settings":"Create a preset",760,550);var panel=new StackPanel{Margin=new Thickness(24)};window.Content=panel;
        panel.Children.Add(new TextBlock{Text="STARTING POINT"});var source=new ComboBox{ItemsSource=new[]{"Current in-game settings (saved files)","Default in-game preset","One of my saved presets"},SelectedIndex=0,IsEnabled=!fromGame};panel.Children.Add(source);
        var folder=Path.Combine(settings.Paths.Game,"OptionDefaults");var templates=Directory.Exists(folder)?Directory.GetFiles(folder,"*.fxcfg").OrderBy(Path.GetFileName).ToArray():Array.Empty<string>();
        var defaults=new ComboBox{ItemsSource=templates.Select(p=>new DefaultChoice(Path.GetFileNameWithoutExtension(p),p)).ToList(),DisplayMemberPath="Name",SelectedIndex=0};panel.Children.Add(defaults);
        var saved=new ComboBox{ItemsSource=store.Heads(),DisplayMemberPath="DisplayName",SelectedIndex=0};panel.Children.Add(saved);
        var help=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,12)};panel.Children.Add(help);
        void Explain(){defaults.Visibility=source.SelectedIndex==1?Visibility.Visible:Visibility.Collapsed;saved.Visibility=source.SelectedIndex==2?Visibility.Visible:Visibility.Collapsed;help.Text=source.SelectedIndex switch{0=>"Close Elite first. Enter a name, then load its saved settings into a new preset. This includes XML overrides and HUD colours. Your existing presets and game settings are preserved.",1=>"Uses the installed game's default quality values. Display/headset mode, HUD colours and newer fields absent from the default are inherited from current saved settings. Other graphics XML overrides are excluded from this new copy. A VR/flat default name does not switch your runtime or headset mode.",_=>"Copies the saved preset exactly, with its captured game definitions. Your source preset is preserved."};}
        source.SelectionChanged+=(_,_)=>Explain();Explain();panel.Children.Add(new TextBlock{Text="NEW PRESET NAME"});var name=new TextBox{Text=fromGame?"Current game settings":"New preset"};panel.Children.Add(name);
        var create=new Button{Content=fromGame?"Load as new preset":"Create preset",HorizontalAlignment=HorizontalAlignment.Right,Style=(Style)FindResource("Primary")};panel.Children.Add(create);
        create.Click+=(_,_)=>{try{
            if(string.IsNullOrWhiteSpace(name.Text))throw new ArgumentException("Enter a preset name.");
            FileSet files;FileSet baselineFiles;byte[] defs;string build;string? parent=null;bool historical=false;string mode;
            if(source.SelectedIndex==2){var p=saved.SelectedItem as ProfileRevision??throw new InvalidOperationException("Choose a saved preset.");files=store.Files(p.Id);baselineFiles=files;defs=store.Definitions(p.Id);build=p.GameBuild;parent=p.Id;historical=p.Historical;mode=p.Mode;}
            else{
                if(GameRunning())throw new InvalidOperationException("Close Elite normally before reading its saved settings.");
                var current=FileSet.Read(settings.Paths.Graphics);defs=settings.Paths.ReadDefinitions();build=settings.Paths.Build();files=current;baselineFiles=current;
                if(source.SelectedIndex==1){var template=defaults.SelectedItem as DefaultChoice??throw new InvalidOperationException("No installed default presets found.");files=PresetDefaults.Create(current,File.ReadAllBytes(template.Path));}
                mode=GraphicsModel.General(files,"StereoscopicMode")=="0"?"FLAT":"VR";
                if(GameRunning()||current.Fingerprint()!=FileSet.Read(settings.Paths.Graphics).Fingerprint()||!defs.SequenceEqual(settings.Paths.ReadDefinitions())||build!=settings.Paths.Build())throw new InvalidOperationException("Game settings changed during capture. Try again.");
            }
            if(!ConfirmLeave())return;
            var notes="Created from "+source.SelectedItem+(source.SelectedIndex==1?": "+((DefaultChoice)defaults.SelectedItem).Name+"; display/headset, HUD and unspecified fields inherited.":".");
            if(!ReviewSave(baselineFiles,files,name.Text.Trim(),mode,notes,source.SelectedIndex==2?"SOURCE PRESET":"CAPTURED GAME",definitions:defs))return;
            var result=store.Fork(name.Text.Trim(),mode,files,defs,build,notes,parent,historical);
            window.DialogResult=true;FinishSave(result);
        }catch(Exception ex){MessageBox.Show(window,ex.Message,"Cannot create preset");}};
        window.ShowDialog();
    });
    sealed record DefaultChoice(string Name,string Path){public override string ToString()=>Name;}
}
