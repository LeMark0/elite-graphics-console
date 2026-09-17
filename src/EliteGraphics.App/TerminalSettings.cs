using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using EliteGraphics.Core;

namespace EliteGraphics.App;

public sealed record TerminalChoice(string Value, string Label)
{
    public override string ToString()=>Label;
}

public sealed class TerminalSetting : INotifyPropertyChanged
{
    public required SettingEntry Entry { get; init; }
    public required string Category { get; init; }
    public required string Saved { get; init; }
    public required string SavedDisplay { get; init; }
    public string? Texture { get; init; }
    public bool Editable { get; init; }
    public bool ReadOnly => !Editable;
    public IReadOnlyList<TerminalChoice> Choices { get; init; } = [];
    public bool HasChoices => Editable && Choices.Count > 0;
    public Visibility ArrowVisibility => HasChoices ? Visibility.Visible : Visibility.Hidden;
    public Visibility ChoiceVisibility => Choices.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility TextVisibility => Choices.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    public string Title => Entry.Setting + (Entry.Value != Saved || Pending ? " *" : "");
    string? input;
    public string Input { get => input ?? Entry.Value; set { input=value; Changed(nameof(Input)); Changed(nameof(Title)); OnInput?.Invoke(this); } }
    public bool Pending => Input != Entry.Value;
    string error="";
    public string Error { get => error; set { error=value; Changed(nameof(Error)); } }
    public Action<TerminalSetting>? OnInput { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    void Changed(string name) => PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(name));
}

public partial class MainWindow
{
    List<TerminalSetting> terminalRows=[];
    bool inlineCommitting;
    bool InlinePending => terminalRows.Any(r=>r.Pending);
    static string Category(SettingEntry row)
    {
        var key=(row.Setting+" "+row.Path).ToLowerInvariant();
        if(key.Contains("hud")||key.Contains("guicolour")||key.Contains("matrix"))return "HUD";
        if(key.Contains("planet")||key.Contains("galaxybackground")||key.Contains("galaxy background")||key.Contains("environment quality"))return "PLANETS / GALAXY";
        if(key.Contains("terrain")||key.Contains("gpu scheduler")||key.Contains("loddistance"))return "TERRAIN";
        if(new[]{"hmd","stereo","supersampling","anti-alias","upscal","cas intensity","ipd"}.Any(key.Contains))return "VR / RENDERING";
        if(new[]{"shadow","occlusion","bloom","blur","effect","volumetric","material","reflection","depth of field"}.Any(key.Contains))return "LIGHTING / EFFECTS";
        if(row.Source=="DisplaySettings.xml"||key.Contains("fov")||key.Contains("gamma"))return "DISPLAY";
        return "ADVANCED / OTHER";
    }
    void BuildTerminalRows(FileSet files)
    {
        var saved=selectedFiles==null?[]:SettingsInventory.Build(selectedFiles,selectedDefinitions,true,true);
        terminalRows=inventory.Select(entry=>{
            var original=saved.FirstOrDefault(r=>r.Source==entry.Source&&r.Path==entry.Path)??entry;
            return new TerminalSetting{Entry=entry,Category=Category(entry),Saved=original.Value,SavedDisplay=original.DisplayValue,
                Editable=entry.CanEdit&&selected?.Historical==false,Choices=SettingEditor.Choices(entry,selectedDefinitions).Select(c=>new TerminalChoice(c.Value,c.Label)).ToArray(),OnInput=InlineInputChanged};
        }).ToList();
        if(selectedDefinitions.Length>0 && selectedFiles!=null)
            foreach(var (feature,label) in new[]{("Planets","Planet texture size"),("GalaxyBackground","Galaxy background texture size")})
            {
                var texture=GraphicsModel.Texture(files,selectedDefinitions,feature);var original=GraphicsModel.Texture(selectedFiles,selectedDefinitions,feature);
                var choices=new[]{512,1024,2048,2560,4096}.Select(v=>v.ToString(CultureInfo.InvariantCulture)).Append(texture.Value).Distinct().Select(v=>new TerminalChoice(v,v+" px")).ToArray();
                terminalRows.Add(new TerminalSetting{Entry=new SettingEntry(label,texture.Value,texture.Source,feature,"Effective texture at the selected environment tier. Edits create or update only that tier's override; dormant tiers are retained."){DisplayValue=texture.Value+" px"},Texture=feature,Category="PLANETS / GALAXY",Saved=original.Value,SavedDisplay=original.Value+" px",Editable=selected?.Historical==false,Choices=choices,OnInput=InlineInputChanged});
            }
    }
    void ShowTerminalRows()
    {
        if(SettingsGrid==null||CategoryBox==null)return;
        var key=(SettingsGrid.SelectedItem as TerminalSetting)?.Entry;
        var query=SettingsSearch.Text.Trim();var category=(CategoryBox.SelectedItem as ComboBoxItem)?.Content as string;
        var order=new[]{"VR / RENDERING","PLANETS / GALAXY","TERRAIN","LIGHTING / EFFECTS","DISPLAY","HUD","ADVANCED / OTHER"};
        var rows=terminalRows.Where(r=>(CategoryBox.SelectedIndex<=0||r.Category==category)&&(query.Length==0||string.Join(" ",r.Entry.Setting,r.Entry.DisplayValue,r.Entry.Value,r.Entry.Source,r.Entry.Path).Contains(query,StringComparison.OrdinalIgnoreCase))).OrderBy(r=>Array.IndexOf(order,r.Category)).ThenBy(r=>r.Texture==null?1:0).ThenBy(r=>r.Entry.Setting).ToList();
        var view=new ListCollectionView(rows);view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(TerminalSetting.Category)));SettingsGrid.ItemsSource=view;
        SettingsGrid.SelectedItem=rows.FirstOrDefault(r=>key!=null&&r.Entry.Source==key.Source&&r.Entry.Path==key.Path)??rows.FirstOrDefault();
        InventorySummary.Text=$"{rows.Count} / {terminalRows.Count} values · Saved-file values; running-game output is not measured.";
    }
    void InlineInputChanged(TerminalSetting row)
    {
        row.Error="";RefreshDirty();ShowSettingDetail(row);
    }
    void Category_Changed(object sender,SelectionChangedEventArgs e){if(!InlinePending)ShowTerminalRows();}
    void Setting_Selected(object sender,SelectionChangedEventArgs e){if(SettingsGrid.SelectedItem is TerminalSetting row)ShowSettingDetail(row);}
    void ShowSettingDetail(TerminalSetting row)
    {
        if(DetailTitle==null)return;
        DetailTitle.Text=row.Entry.Setting.ToUpperInvariant();DetailNote.Text=row.Entry.Note+(row.Choices.Count==0?"\n\nEdit raw values: multipliers use 1.0; percentage fields use fractions (0.7 = 70%).":"");
        DetailSaved.Text=row.SavedDisplay;DetailDraft.Text=row.Pending?row.Input:row.Entry.DisplayValue;
        DetailSource.Text=row.Entry.Source;DetailPath.Text=row.Entry.Path+"\n"+row.Entry.Value;
        ResetSettingButton.IsEnabled=row.Editable&&(row.Pending||row.Entry.Value!=row.Saved);
    }
    FileSet ChangeInline(TerminalSetting row,string value)
    {
        var files=Edited();
        if(row.Texture==null)return SettingEditor.Change(files,row.Entry,value);
        if(!int.TryParse(value,out var size))throw new ArgumentException("Choose a texture size.");
        GraphicsModel.SetTexture(files,selectedDefinitions,row.Texture,size);return files;
    }
    bool CommitInline(TerminalSetting row)
    {
        if(!terminalRows.Contains(row)||!row.Pending)return true;
        try
        {
            NoRecording();NeedSelection();if(!row.Editable)throw new InvalidOperationException("This value is reference-only.");
            if(terminalRows.Any(r=>r!=row&&r.Pending))throw new InvalidOperationException("Finish or cancel the other pending edit first.");
            inlineCommitting=true;var updated=ChangeInline(row,row.Input);row.Error="";
            // Clear pending presentation rows before reloading the shared draft.
            terminalRows=[];AcceptDraft(updated);return true;
        }
        catch(Exception ex){row.Error=ex.Message;RefreshDirty();return false;}
        finally{inlineCommitting=false;RefreshDirty();}
    }
    void Inline_Focus(object sender,KeyboardFocusChangedEventArgs e){if((sender as FrameworkElement)?.DataContext is TerminalSetting row){SettingsGrid.SelectedItem=row;ShowSettingDetail(row);}}
    void Inline_LostFocus(object sender,KeyboardFocusChangedEventArgs e){if(!loading&&!inlineCommitting&&(sender as FrameworkElement)?.DataContext is TerminalSetting row)CommitInline(row);}
    void Inline_KeyDown(object sender,KeyEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is not TerminalSetting row)return;
        if(e.Key==Key.Enter){CommitInline(row);e.Handled=true;}
        if(e.Key==Key.Escape){row.Input=row.Entry.Value;row.Error="";e.Handled=true;RefreshDirty();}
    }
    void InlineChoice_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(loading||inlineCommitting||sender is not ComboBox box||!box.IsKeyboardFocusWithin||box.DataContext is not TerminalSetting row||box.SelectedValue is not string value||value==row.Input)return;
        row.Input=value;CommitInline(row);e.Handled=true;
    }
    void InlineChoice_Wheel(object sender,MouseWheelEventArgs e){e.Handled=true;SettingsGrid.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice,e.Timestamp,e.Delta){RoutedEvent=Mouse.MouseWheelEvent,Source=SettingsGrid});}
    void StepSetting_Click(object sender,RoutedEventArgs e)
    {
        if(sender is not Button button||button.DataContext is not TerminalSetting row||!row.HasChoices)return;
        SettingsGrid.SelectedItem=row;var index=row.Choices.ToList().FindIndex(c=>c.Value==row.Input);var step=button.Tag?.ToString()=="-1"?-1:1;
        row.Input=row.Choices[Math.Clamp(index+step,0,row.Choices.Count-1)].Value;CommitInline(row);
    }
    void ResetSetting_Click(object sender,RoutedEventArgs e){if(SettingsGrid.SelectedItem is TerminalSetting row){row.Input=row.Saved;CommitInline(row);}}
    void TerminalNotes_Changed(object sender,TextChangedEventArgs e){if(NotesBox!=null&&!loading)NotesBox.Text=TerminalNotes.Text;}
    void SettingsGrid_SizeChanged(object sender,SizeChangedEventArgs e)
    {
        if(SettingsGrid.Columns.Count>0)SettingsGrid.Columns[0].Width=new DataGridLength(Math.Max(160,e.NewSize.Width-278));
    }
}
