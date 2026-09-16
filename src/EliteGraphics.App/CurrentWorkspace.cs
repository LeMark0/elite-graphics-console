using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EliteGraphics.Core;

namespace EliteGraphics.App;
public partial class MainWindow
{
    bool currentWorkspace;
    void NeedSavedPreset(){NeedSelection();if(currentWorkspace)throw new InvalidOperationException("Save the current workspace as a preset first.");}
    void LoadCurrentState()
    {
        NoRecording();if(GameRunning())throw new InvalidOperationException("Close Elite normally before loading its saved settings.");
        var files=FileSet.Read(settings.Paths.Graphics);files.Validate();var defs=settings.Paths.ReadDefinitions();var build=settings.Paths.Build();
        if(GameRunning()||files.Fingerprint()!=FileSet.Read(settings.Paths.Graphics).Fingerprint()||!defs.SequenceEqual(settings.Paths.ReadDefinitions())||build!=settings.Paths.Build())throw new InvalidOperationException("Settings changed while reading. Try loading again.");
        if(!ConfirmLeave())return;
        loading=true;ProfilesList.SelectedItem=null;loading=false;
        LoadProfile(new ProfileRevision{Name="Current game settings",Mode=GraphicsModel.General(files,"StereoscopicMode")=="0"?"FLAT":"VR",GameBuild=build,Hashes=files.Hashes()},files,defs);
        MainTabs.SelectedItem=ConfigureTab;
    }
    void SaveCurrent_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NoRecording();NeedSelection();if(!currentWorkspace)return;
        var name=Prompt("Save as preset","Preset name","Current game settings");if(name==null)return;
        FinishSave(store.Fork(name,(string)ModeBox.SelectedItem,Edited(),selectedDefinitions,selected!.GameBuild,NotesBox.Text));
    });
    void DeletePreset_Click(object sender,RoutedEventArgs e)=>Guard(()=>
    {
        NoRecording();NeedSavedPreset();var count=store.History(selected!.Id).Count;
        if(MessageBox.Show(this,$"Delete preset ‘{selected.Name}’ from the library?\n\n{count} revision(s) will be hidden. Revision files remain in the local library for recovery and benchmark references. Other presets and the settings applied to the game stay unchanged."+(IsDirty?"\n\nUnsaved edits will be discarded.":""),"Delete preset",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
        store.Delete(selected.Id);currentWorkspace=false;draftFiles=null;selected=null;selectedFiles=null;editorKey=EditorKey();
        RefreshLibrary();
        if(selected==null){MainTabs.IsEnabled=false;DirtyStatus.Text="";ProfileTitle.Text="No preset selected";ProfileSubtitle.Text="Load current game settings or create a preset with +.";UpdatePresetButton.IsEnabled=ForkPresetButton.IsEnabled=DeletePresetButton.IsEnabled=DiscardButton.IsEnabled=false;SaveCurrentButton.Visibility=Visibility.Collapsed;UpdateApplyHeader();UpdateInventory();}
    });
    void AcceptDraft(FileSet files)
    {
        files.Validate();_=GraphicsModel.ActiveFile(files);
        draftFiles=files;LoadDraftControls(files);RefreshDirty();UpdateInventory();
    }
    void EditSetting_Click(object sender,RoutedEventArgs e)
    {
        if((sender as FrameworkElement)?.DataContext is SettingEntry row)Guard(()=>EditSetting(row));
    }
    void Setting_DoubleClick(object sender,MouseButtonEventArgs e)
    {
        if((e.OriginalSource as FrameworkElement)?.DataContext is SettingEntry row)Guard(()=>EditSetting(row));
    }
    sealed record ValueChoice(string Value,string Label){public override string ToString()=>Label;}
    void EditSetting(SettingEntry row)
    {
        NoRecording();NeedSelection();if(!SettingEditor.CanEdit(row))throw new InvalidOperationException("Installed definitions and schema version metadata are reference-only. Edit a saved setting or XML override instead.");
        if(selected!.Historical)throw new InvalidOperationException("Migrate this historical preset before editing.");
        var files=Edited();var window=Dialog("Edit "+row.Setting,660,390);var panel=new StackPanel{Margin=new Thickness(24)};window.Content=panel;
        panel.Children.Add(new TextBlock{Text=row.Setting,FontSize=20});panel.Children.Add(new TextBlock{Text=row.Source+"\n"+row.Path,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,12)});
        var choices=SettingEditor.Choices(row,selectedDefinitions).Select(p=>new ValueChoice(p.Value,p.Label+" ("+p.Value+")")).ToList();
        var input=new TextBox{Text=row.Value};var combo=new ComboBox{ItemsSource=choices,DisplayMemberPath="Label",SelectedItem=choices.FirstOrDefault(p=>p.Value==row.Value)};
        panel.Children.Add(choices.Count>0?combo:input);
        panel.Children.Add(new TextBlock{Text="Current: "+row.DisplayValue+"\n"+(choices.Count>0?"Choose a value. Changes are kept in the draft.":"Enter the raw XML value shown above. Multipliers use 1.0; percentage fields use fractions (0.7 = 70%). Changes are kept in the draft."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,16)});
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(buttons);buttons.Children.Add(new Button{Content="Cancel",IsCancel=true});
        var use=new Button{Content="Use value",IsDefault=true,Style=(Style)FindResource("Primary")};buttons.Children.Add(use);
        use.Click+=(_,_)=>{try{var value=choices.Count>0?(combo.SelectedItem as ValueChoice)?.Value??throw new ArgumentException("Choose a value."):input.Text;var updated=SettingEditor.Change(files,row,value);_=GraphicsModel.ActiveFile(updated);window.DialogResult=true;AcceptDraft(updated);}catch(Exception ex){MessageBox.Show(window,ex.Message,"Cannot use value");}};
        window.ShowDialog();
    }
}
