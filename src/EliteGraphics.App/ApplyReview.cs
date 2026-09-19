using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EliteGraphics.Core;

namespace EliteGraphics.App;
public partial class MainWindow
{
    bool ReviewApply(IReadOnlyList<SettingDiff> changes)
    {
        var window=CreateApplyReview(changes);return window.ShowDialog()==true;
    }
    Window CreateApplyReview(IReadOnlyList<SettingDiff> changes)
        =>CreateChangeReview(changes,"REVIEW / APPLY PRESET",selected?.DisplayName??"Preset","CURRENT GAME","SELECTED PRESET","Apply these files","A verified rollback snapshot is saved before replacement. Restart Elite to use the applied settings.");

    bool ReviewSave(FileSet before,FileSet after,string name,string mode,string notes,string baseline,bool update=false,byte[]? definitions=null)
    {
        var changes=ChangeReview.Between(before,after).ToList();
        foreach(var (field,oldValue,newValue) in new[]{("Name",update?selected?.Name??"":"— new preset",name),("Mode",update?selected?.Mode??"":"",mode),("Notes",update?selected?.Notes??"":"",notes)})
            if(oldValue!=newValue)changes.Add(new("Preset metadata",field,oldValue,newValue));
        return CreateChangeReview(changes,"REVIEW / SAVE PRESET",name,baseline,"TO SAVE",update?"Update preset":"Save preset","Saves to your preset library only. Game settings are unchanged. Cancel returns to your draft.",definitions).ShowDialog()==true;
    }

    Window CreateChangeReview(IReadOnlyList<SettingDiff> changes,string heading,string name,string beforeLabel,string afterLabel,string action,string explanation,byte[]? definitions=null)
    {
        var window=Dialog("Review changes",1040,650);window.Name="ChangeReviewWindow";var panel=new DockPanel{Margin=new Thickness(24)};window.Content=panel;
        var header=new StackPanel{Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
        header.Children.Add(new TextBlock{Text=heading,Foreground=(System.Windows.Media.Brush)FindResource("Amber"),FontSize=20});
        header.Children.Add(new TextBlock{Text=name,Margin=new Thickness(0,8,0,8)});
        var fileChanges=changes.Count(c=>c.File!="Preset metadata");
        header.Children.Add(new TextBlock{Text=(fileChanges==0?"No graphics file changes. ":$"{fileChanges} file/setting changes across {changes.Where(c=>c.File!="Preset metadata").Select(c=>c.File).Distinct().Count()} file(s). ")+explanation,Style=(Style)FindResource("Muted")});
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(actions,Dock.Bottom);panel.Children.Add(actions);
        actions.Children.Add(new Button{Content="Cancel",IsCancel=true,IsDefault=true});var applyButton=new Button{Name="ConfirmChanges",Content=action,Style=(Style)FindResource("Primary")};applyButton.Click+=(_,_)=>window.DialogResult=true;actions.Children.Add(applyButton);
        string Display(SettingDiff change,string value)
        {
            if(change.File=="Preset metadata")return value;
            var defs=definitions??selectedDefinitions;
            var field=SettingReference.Field(change.Path);var readable=SettingsInventory.FormatValue(field,value,defs.Length>0?XmlIO.Read(defs).Root:null,change.File.EndsWith(".fxcfg")||change.File is "Settings.xml" or "DisplaySettings.xml");
            return readable==value?value:readable+"\nRaw: "+value;
        }
        var table=new DataGrid{IsReadOnly=true,ItemsSource=changes.Select(c=>new{Setting=SettingsInventory.Label(SettingReference.Field(c.Path))+"\n"+c.File+"\n"+c.Path,Current=Display(c,c.Before),Preset=Display(c,c.After)}),CanUserReorderColumns=false};panel.Children.Add(table);
        foreach(var (label,field,width) in new[]{("SETTING / FILE","Setting",2d),(beforeLabel,"Current",1d),(afterLabel,"Preset",1d)})table.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(field),Width=new DataGridLength(width,DataGridLengthUnitType.Star),ElementStyle=(Style)FindResource("CellText")});
        return window;
    }
}
