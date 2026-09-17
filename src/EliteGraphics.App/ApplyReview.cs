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
    {
        var window=Dialog("Review apply",1040,650);var panel=new DockPanel{Margin=new Thickness(24)};window.Content=panel;
        var header=new StackPanel{Margin=new Thickness(0,0,0,16)};DockPanel.SetDock(header,Dock.Top);panel.Children.Add(header);
        header.Children.Add(new TextBlock{Text="REVIEW / APPLY PRESET",Foreground=(System.Windows.Media.Brush)FindResource("Amber"),FontSize=20});
        header.Children.Add(new TextBlock{Text=selected?.DisplayName,Margin=new Thickness(0,8,0,8)});
        header.Children.Add(new TextBlock{Text="Review the exact saved-file changes below. A verified rollback snapshot is saved before replacement. Restart Elite to use the applied settings.",Style=(Style)FindResource("Muted")});
        var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,16,0,0)};DockPanel.SetDock(actions,Dock.Bottom);panel.Children.Add(actions);
        actions.Children.Add(new Button{Content="Cancel",IsCancel=true});var applyButton=new Button{Content="Apply these files",Style=(Style)FindResource("Primary")};applyButton.Click+=(_,_)=>window.DialogResult=true;actions.Children.Add(applyButton);
        var table=new DataGrid{ItemsSource=changes.Select(c=>new{Setting=c.File+"\n"+c.Path,Current=c.Before,Preset=c.After}),CanUserReorderColumns=false};panel.Children.Add(table);
        foreach(var (label,field,width) in new[]{("SETTING / FILE","Setting",2d),("CURRENT GAME","Current",1d),("SELECTED PRESET","Preset",1d)})table.Columns.Add(new DataGridTextColumn{Header=label,Binding=new Binding(field),Width=new DataGridLength(width,DataGridLengthUnitType.Star),ElementStyle=(Style)FindResource("CellText")});
        return window;
    }
}
