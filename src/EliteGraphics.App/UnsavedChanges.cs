using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EliteGraphics.App;
public partial class MainWindow
{
    bool CanUpdateBeforeLeaving()=>!currentWorkspace&&selected is {Protected:false,Historical:false}&&store.Heads().Any(p=>p.Id==selected.Id);

    void SaveBeforeLeaving(string name)
    {
        NoRecording();NeedSelection();
        var files=Edited();
        if(string.IsNullOrWhiteSpace(name))throw new ArgumentException("Enter a name for the new preset.");
        var saved=CanUpdateBeforeLeaving()
            ?store.Update(selected!.Id,(string)ModeBox.SelectedItem,files,selectedDefinitions,selected.GameBuild,NotesBox.Text)
            :store.Fork(name.Trim(),(string)ModeBox.SelectedItem,files,selectedDefinitions,selected!.GameBuild,NotesBox.Text,currentWorkspace?null:selected.Id,selected.Historical);
        FinishSave(saved);
    }

    Window CreateUnsavedDialog()
    {
        var update=CanUpdateBeforeLeaving();
        var window=Dialog("Unsaved changes",780,400);window.SizeToContent=SizeToContent.Height;window.MinHeight=0;
        var panel=new StackPanel{Margin=new Thickness(24)};window.Content=panel;
        panel.Children.Add(new TextBlock{Text="YOU HAVE UNSAVED CHANGES",Foreground=(Brush)FindResource("Amber"),FontSize=20,Margin=new Thickness(0,0,0,14)});
        panel.Children.Add(new TextBlock{Text=currentWorkspace?"You edited the loaded current game settings. These edits have not been saved as a preset.":$"You edited ‘{selected?.Name}’. These changes have not been saved to the preset.",Margin=new Thickness(0,0,0,10)});
        panel.Children.Add(new TextBlock{Text="Choose what to do before continuing. Discard removes only these unsaved app edits. Your game settings stay unchanged.",Style=(Style)FindResource("Muted"),Margin=new Thickness(0,0,0,18)});
        var name=new TextBox{Name="UnsavedPresetName",Text=currentWorkspace?"Current game settings":selected?.Name+" Copy"};
        if(!update){panel.Children.Add(new TextBlock{Text="NEW PRESET NAME",Foreground=(Brush)FindResource("Amber"),FontSize=11});panel.Children.Add(name);}
        var error=new TextBlock{Name="UnsavedSaveError",Foreground=new SolidColorBrush(Color.FromRgb(255,102,85)),Margin=new Thickness(0,0,0,12)};panel.Children.Add(error);
        var actions=new WrapPanel{HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(actions);
        var keep=new Button{Name="KeepEditing",Content="Keep editing",IsCancel=true,IsDefault=true};actions.Children.Add(keep);
        var discard=new Button{Name="DiscardAndContinue",Content="Discard changes and continue"};discard.Click+=(_,_)=>window.DialogResult=true;actions.Children.Add(discard);
        var save=new Button{Name="SaveAndContinue",Content=update?"Update preset and continue":currentWorkspace?"Save as preset and continue":"Fork preset and continue",Style=(Style)FindResource("Primary")};actions.Children.Add(save);
        try{NoRecording();_=Edited();}catch(Exception ex){save.IsEnabled=false;error.Text="To save, choose Keep editing and resolve this first: "+ex.Message;}
        save.Click+=(_,_)=>{try{SaveBeforeLeaving(update?selected!.Name:name.Text);window.DialogResult=true;}catch(Exception ex){error.Text="Could not save: "+ex.Message;}};
        window.ContentRendered+=(_,_)=>keep.Focus();
        return window;
    }
}
