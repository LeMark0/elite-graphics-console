using System.Text;
using System.Runtime.InteropServices;

namespace EliteGraphics.Core;

public static class GraphicsTarget
{
    [DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
    static extern uint GetLongPathName(string shortPath,StringBuilder longPath,uint capacity);
    public static string CheckedPath(string path)
    {
        var full=Path.GetFullPath(path);
        if(!OperatingSystem.IsWindows() || full.Length<3 || !char.IsAsciiLetter(full[0]) || full[1]!=':' || full[2]!='\\')
            throw new InvalidDataException("Graphics transactions require a local Windows drive path.");
        if(full[3..].Split('\\','/').Any(x=>x.EndsWith(' ')||x.EndsWith('.')||x.Contains(':')))
            throw new InvalidDataException("Ambiguous Windows path is not supported.");
        for(var cursor=full;!string.IsNullOrEmpty(cursor);cursor=Path.GetDirectoryName(cursor))
        {
            try { if((File.GetAttributes(cursor)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("Linked paths are not supported for graphics transactions: "+cursor); }
            catch(FileNotFoundException) { }
            catch(DirectoryNotFoundException) { }
        }
        if(Directory.Exists(full)||File.Exists(full))
        {
            var expanded=new StringBuilder(32768);var length=GetLongPathName(full,expanded,(uint)expanded.Capacity);
            if(length==0||length>=expanded.Capacity)throw new IOException("Cannot resolve the canonical transaction path.");
            full=expanded.ToString();
        }
        return Path.TrimEndingDirectorySeparator(full);
    }

    public static string ValidateDirectory(string path)
    {
        var full=CheckedPath(path);
        var suffix=Path.Combine("Frontier Developments","Elite Dangerous","Options","Graphics");
        if(!Directory.Exists(full)||!full.EndsWith("\\"+suffix,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Select the Elite user folder ending in Frontier Developments\\Elite Dangerous\\Options\\Graphics. Arbitrary directories cannot be used for Apply or Restore.");
        return full;
    }

    public static void ValidateSettings(FileSet files)
    {
        files.Validate();
        if(XmlIO.Read(files["Settings.xml"]).Root?.Name!="GraphicsOptions")throw new InvalidDataException("Target Settings.xml is not an Elite GraphicsOptions document.");
        _=GraphicsModel.ActiveFile(files);
    }
}

/// <summary>Persistent lock file: never unlink it, even after a completed transaction.</summary>
internal sealed class TargetTransaction : IDisposable
{
    readonly FileStream stream;
    readonly string owner;
    public TargetTransaction(string target,string library)
    {
        owner=Path.GetFullPath(library).TrimEnd('\\').ToUpperInvariant();
        var path=GraphicsTarget.CheckedPath(Path.Combine(target,".egc-transaction.lock"));
        try { stream=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); }
        catch(IOException ex) { throw new IOException("Another graphics transaction is using this target. Wait for it to finish, then retry.",ex); }
        try
        {
            if(stream.Length>32768)throw new InvalidDataException("Invalid target transaction marker.");
            var bytes=new byte[(int)stream.Length];stream.ReadExactly(bytes);
            var pending=Encoding.UTF8.GetString(bytes);
            if(pending.Length>0&&pending!=owner)throw new InvalidOperationException("This target has an unfinished transaction in another library. Open the original library and restore/recover it before applying here. Transaction folder: "+pending);
        }
        catch { stream.Dispose();throw; }
    }
    public void Begin()=>Write(owner);
    public void Complete()=>Write("");
    void Write(string value){stream.Position=0;var bytes=Encoding.UTF8.GetBytes(value);stream.Write(bytes);stream.SetLength(bytes.Length);stream.Flush(true);}
    public void Dispose()=>stream.Dispose();
}
