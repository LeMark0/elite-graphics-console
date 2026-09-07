using System.IO;
using System.Windows;

namespace EliteGraphics.App;
public partial class App : Application
{
    Mutex? mutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        string? data=null;for(int i=0;i<e.Args.Length-1;i++)if(e.Args[i]=="--data-dir")data=e.Args[i+1];
        data??=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"EliteGraphicsConsole");
        var mutexName="Local\\EliteGraphicsConsole-"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(data).ToLowerInvariant())))[..16];
        mutex=new Mutex(true,mutexName,out bool owns);if(!owns){MessageBox.Show("Elite Graphics Console is already open for this library.");Shutdown();return;}
        try{var window=new MainWindow(data);MainWindow=window;window.Show();}catch(Exception ex){Directory.CreateDirectory(data);File.WriteAllText(Path.Combine(data,"startup-error.log"),ex.ToString());MessageBox.Show(ex.Message+"\n\nDetails saved in "+data,"Startup problem");Shutdown(1);}
    }
    protected override void OnExit(ExitEventArgs e){mutex?.Dispose();base.OnExit(e);}
}
