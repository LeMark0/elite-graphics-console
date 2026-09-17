using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EliteGraphics.App;
using EliteGraphics.Core;

internal static class UiTests
{
    [STAThread]
    static int Main()
    {
        var root=Path.Combine(Path.GetTempPath(),"egc-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        var game=Path.Combine(root,"game");var graphics=Path.Combine(root,"graphics");Directory.CreateDirectory(game);Directory.CreateDirectory(graphics);
        var defs=Encoding.UTF8.GetBytes("<GraphicsConfig><Environment><Low><LocalisationName>L</LocalisationName><Item><Feature>Planets</Feature><QualitySetting>0</QualitySetting></Item><Item><Feature>GalaxyBackground</Feature><QualitySetting>0</QualitySetting></Item></Low></Environment><Planets><Low><TextureSize>1024</TextureSize></Low></Planets><GalaxyBackground><Low><TextureSize>1024</TextureSize></Low></GalaxyBackground><Terrain><Low><LocalisationName>L</LocalisationName></Low></Terrain><HBAO><Off><LocalisationName>O</LocalisationName></Off></HBAO><Volumetrics><Low><LocalisationName>L</LocalisationName></Low></Volumetrics></GraphicsConfig>");
        File.WriteAllBytes(Path.Combine(game,"GraphicsConfiguration.xml"),defs);
        var files=new FileSet{
            ["Settings.xml"]=Encoding.UTF8.GetBytes("<GraphicsOptions><PresetName>Custom</PresetName><StereoscopicMode>3</StereoscopicMode></GraphicsOptions>"),
            ["DisplaySettings.xml"]=Encoding.UTF8.GetBytes("<DisplayConfig><ScreenWidth>1920</ScreenWidth><ScreenHeight>1080</ScreenHeight><MaxFramesPerSecond>60</MaxFramesPerSecond><VSync>false</VSync><LimitFrameRate>false</LimitFrameRate></DisplayConfig>"),
            ["Custom.4.4.fxcfg"]=Encoding.UTF8.GetBytes("<Root MajorVersion='4' MinorVersion='4'><EnvironmentQuality>0</EnvironmentQuality><TerrainQuality>0</TerrainQuality><AOQuality>0</AOQuality><VolumetricsQuality>0</VolumetricsQuality><HMDRenderTargetMultiplier>1</HMDRenderTargetMultiplier><SSAAMultiplier>1</SSAAMultiplier><LODDistanceScale>0.7</LODDistanceScale><GpuSchedulerMultiplier>0.7</GpuSchedulerMultiplier><DirectionalShadowQuality>3</DirectionalShadowQuality><SpotShadowQuality>2</SpotShadowQuality><AAMode>1</AAMode><NewUnknown>keep</NewUnknown></Root>"),
            ["GraphicsConfigurationOverride.xml"]=Encoding.UTF8.GetBytes("<GraphicsConfig><GUIColour><Default><MatrixRed>0.4, 0.2, 1</MatrixRed></Default></GUIColour></GraphicsConfig>")};
        foreach(var(file,bytes)in files)File.WriteAllBytes(Path.Combine(graphics,file),bytes);
        JsonIO.Save(Path.Combine(root,"settings.json"),new LocalSettings{Paths=new AppPaths{Game=game,Graphics=graphics,Legacy=""}});
        var store=new ProfileStore(root);var original=store.Fork("VR FXAA + 4096","VR",files,defs,"test","UI fixture");
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};app.Resources=new ResourceDictionary{Source=new Uri("/EliteGraphicsConsole;component/TerminalTheme.xaml",UriKind.Relative)};
        MainWindow? window=null;int failures=0,passes=0;
        void Assert(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);passes++;}
        try
        {
            window=new MainWindow(root);window.Show();Pump();
            object? Call(string name,params object?[] args)=>typeof(MainWindow).GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,args);
            T Field<T>(string name)=>(T)typeof(MainWindow).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!;
            TerminalSetting Row(string name)=>Field<List<TerminalSetting>>("terminalRows").Single(r=>r.Entry.Setting==name);
            bool Commit(TerminalSetting row)=>(bool)Call("CommitInline",row)!;
            var hmd=Row("HMD image quality");hmd.Input="1.1";Assert(Commit(hmd),"Inline decimal commits from integer serialization");
            Assert(GraphicsModel.Quality((FileSet)Call("Edited")!,"HMDRenderTargetMultiplier")=="1.1","Shared draft retains inline multiplier");
            Assert(Field<Button>("UpdatePresetButton").IsEnabled&&!Field<Button>("HeaderApplyButton").IsEnabled,"Dirty preset can update but cannot apply");
            hmd=Row("HMD image quality");hmd.Input="invalid";Assert(!Commit(hmd)&&hmd.Error.Length>0,"Invalid inline edit remains visible");
            Assert(!Field<Button>("UpdatePresetButton").IsEnabled&&!Field<Button>("ForkPresetButton").IsEnabled,"Invalid edit blocks save and fork");
            Call("UpdateInventory");Assert(Row("HMD image quality")==hmd&&hmd.Input=="invalid","Refresh does not discard invalid input");
            var ss=Row("Supersampling");ss.Input="1.2";Assert(!Commit(ss)&&hmd.Input=="invalid","Second edit cannot discard an invalid first edit");
            ss.Input=ss.Entry.Value;hmd.Input=hmd.Entry.Value;Call("RefreshDirty");Assert(Field<Button>("UpdatePresetButton").IsEnabled,"Cancelling pending values restores save availability");
            var aa=Row("Anti-aliasing");aa.Input="0";Assert(Commit(aa)&&Row("Anti-aliasing").Entry.DisplayValue=="Off","Choice changes reach shared draft");
            var planet=Row("Planet texture size");planet.Input="4096";Assert(Commit(planet),"Effective texture edits selected tier");
            var draft=(FileSet)Call("Edited")!;Assert(GraphicsModel.Texture(draft,defs,"Planets").Value=="4096"&&Encoding.UTF8.GetString(draft["GraphicsConfigurationOverride.xml"]).Contains("0.4, 0.2, 1"),"Texture edit preserves HUD");
            Call("UpdatePreset_Click",window,new RoutedEventArgs());Assert(store.Heads().Single().Number==2,"Update saves revision under original identity");
            Assert(Row("HMD image quality").Entry.Value=="1.1"&&Row("Anti-aliasing").Entry.Value=="0","Saved revision reloads edited values");
            Capture(window,Path.Combine(root,"settings.png"),1440,940);
            Capture(window,Path.Combine(root,"settings-small.png"),1120,760);
            var settingsGrid=Field<DataGrid>("SettingsGrid");Assert(settingsGrid.Columns.Sum(c=>c.ActualWidth)<=settingsGrid.ActualWidth,"Both settings columns fit minimum window width");
            var tabs=Field<TabControl>("MainTabs");tabs.SelectedIndex=2;Pump();Capture(window,Path.Combine(root,"compare.png"),1440,940);
            var review=(Window)Call("CreateApplyReview",GraphicsModel.Diff(files,draft))!;review.Show();Pump();Capture(review,Path.Combine(root,"apply-review.png"),1040,650);review.Close();
            Call("LoadCurrentState");Assert(Field<ListBox>("ProfilesList").SelectedItem==null&&Field<Button>("SaveCurrentButton").Visibility==Visibility.Visible,"Load current clears selection and offers save immediately");
            Assert(store.Heads().Count==1&&store.List().Count==2,"Load current creates no revision");
            hmd=Row("HMD image quality");hmd.Input="bad";Commit(hmd);Assert(!Field<Button>("SaveCurrentButton").IsEnabled,"Invalid current-state edit blocks save");
            hmd.Input=hmd.Entry.Value;Call("RefreshDirty");Assert(Field<Button>("SaveCurrentButton").IsEnabled,"Cancelling current edit restores save");
            Assert(FileSet.Read(graphics).Fingerprint()==files.Fingerprint(),"UI editing never writes game files");
            Assert(new TerminalChoice("1","FXAA").ToString()=="FXAA","Selector displays friendly label");
            Call("LoadProfile",original,null,null); // Leave a clean workspace for Close.
        }
        catch(Exception ex){failures++;Console.WriteLine("FAIL "+(ex.InnerException??ex));}
        finally{if(window!=null){typeof(MainWindow).GetField("terminalRows",BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(window,new List<TerminalSetting>());window.Hide();}app.Shutdown();}
        Console.WriteLine($"{passes} checks passed; {failures} failed. Artifacts: {root}");return failures==0?0:1;
    }
    static void Pump()
    {
        var frame=new DispatcherFrame();Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));Dispatcher.PushFrame(frame);
    }
    static void Capture(Window window,string path,int width,int height)
    {
        window.Width=width;window.Height=height;window.UpdateLayout();Pump();
        var surface=window;var bitmap=new RenderTargetBitmap((int)surface.ActualWidth,(int)surface.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(surface);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
    }
}
