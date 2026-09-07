namespace EliteGraphics.Core;

public sealed record TransactionRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Target { get; init; } = "";
    public string State { get; set; } = "Prepared";
    public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;
    public Dictionary<string,string?> Before { get; init; } = new();
    public Dictionary<string,string> After { get; init; } = new();
}

/// <summary>Recoverable per-file replacement; never mirrors/deletes unrelated destination files.</summary>
public sealed class ApplyService
{
    readonly string history;
    readonly Func<bool> gameRunning;
    public ApplyService(string dataRoot,Func<bool> running) { history=Path.Combine(dataRoot,"transactions");Directory.CreateDirectory(history);gameRunning=running; }
    void Closed(){if(gameRunning())throw new InvalidOperationException("Close Elite Dangerous before applying or restoring graphics.");}
    static bool SamePath(string a,string b)=>Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);
    public IReadOnlyList<TransactionRecord> List()=>Directory.GetDirectories(history).Where(x=>File.Exists(Path.Combine(x,"transaction.json"))).Select(x=>JsonIO.Load<TransactionRecord>(Path.Combine(x,"transaction.json"))).OrderByDescending(x=>x.Created).ToList();
    string Folder(string id){if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Invalid transaction ID.");return Path.Combine(history,id);}
    public void Apply(string target,FileSet desired,string expectedFingerprint,byte[] capturedDefinitions,byte[] installedDefinitions,string capturedBuild,string installedBuild,Action<int>? afterWrite=null)
    {
        Closed();desired.Validate();var current=FileSet.Read(target);
        if(current.Fingerprint()!=expectedFingerprint)throw new InvalidOperationException("Graphics changed after the preview. Refresh, inspect the changes, then retry.");
        if(capturedDefinitions.Length==0||installedDefinitions.Length==0||FileSet.Hash(capturedDefinitions)!=FileSet.Hash(installedDefinitions))throw new InvalidOperationException("Installed graphics definitions differ or are missing. Capture/migrate a revision for the current game build.");
        if(capturedBuild=="Unknown"||installedBuild=="Unknown"||capturedBuild!=installedBuild)throw new InvalidOperationException("Game build differs or is unknown. Capture/migrate for this installation first.");
        if(GraphicsModel.ActiveFile(desired)!=GraphicsModel.ActiveFile(current))throw new InvalidOperationException("Custom schema differs. Migrate the historical profile first; newer files will not be deleted.");
        if(current.Keys.Except(desired.Keys,StringComparer.OrdinalIgnoreCase).Any())throw new InvalidOperationException("Current graphics contains files absent from this revision. Capture/migrate a new revision, or restore the preceding transaction. Apply will not silently retain extra settings or delete them.");
        if(List().Any(x=>x.State=="Prepared"&&SamePath(x.Target,target)))throw new InvalidOperationException("An unfinished transaction needs recovery before applying.");
        var record=new TransactionRecord{Target=Path.GetFullPath(target),Before=desired.Keys.ToDictionary(x=>x,x=>current.TryGetValue(x,out var b)?FileSet.Hash(b):null),After=desired.Hashes()};
        var folder=Folder(record.Id);Directory.CreateDirectory(Path.Combine(folder,"before"));Directory.CreateDirectory(Path.Combine(folder,"stage"));
        foreach(var (name,bytes) in desired){if(current.TryGetValue(name,out var original))File.WriteAllBytes(Path.Combine(folder,"before",name),original);File.WriteAllBytes(Path.Combine(folder,"stage",name),bytes);}
        foreach(var (name,hash) in record.Before)if(hash!=null&&FileSet.Hash(File.ReadAllBytes(Path.Combine(folder,"before",name)))!=hash)throw new IOException("Rollback backup verification failed before apply.");
        JsonIO.Save(Path.Combine(folder,"transaction.json"),record);
        try
        {
            Closed();if(FileSet.Read(target).Fingerprint()!=expectedFingerprint)throw new InvalidOperationException("Graphics changed while staging the apply.");
            int n=0;foreach(var (name,bytes) in desired){Closed();Replace(Path.Combine(target,name),bytes);afterWrite?.Invoke(++n);}
            foreach(var (name,hash) in record.After)if(FileSet.Hash(File.ReadAllBytes(Path.Combine(target,name)))!=hash)throw new IOException("Applied file verification failed.");
            record.State="Applied";JsonIO.Save(Path.Combine(folder,"transaction.json"),record);
        }
        catch
        {
            // Leave the durable journal intact if recovery cannot complete, rather than masking it as success.
            Restore(record,target,expectedCurrent:null);
            throw;
        }
    }
    static void Replace(string destination,byte[] bytes)
    {
        var temp=destination+".egc-"+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,bytes);File.Move(temp,destination,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void RestorePrevious(string target,string expectedCurrent)
    {
        var record=List().FirstOrDefault(x=>x.State=="Applied"&&SamePath(x.Target,target))??throw new InvalidOperationException("No applied transaction is available to restore.");
        Restore(record,target,expectedCurrent);
    }
    public void RecoverPending(string target)
    {
        foreach(var record in List().Where(x=>x.State=="Prepared"&&SamePath(x.Target,target)))Restore(record,target,null);
    }
    void Restore(TransactionRecord record,string target,string? expectedCurrent)
    {
        Closed();if(!SamePath(record.Target,target))throw new InvalidOperationException("Rollback target mismatch.");
        var current=FileSet.Read(target);if(expectedCurrent!=null&&current.Fingerprint()!=expectedCurrent)throw new InvalidOperationException("Graphics changed after the restore preview.");
        var folder=Folder(record.Id);var original=new Dictionary<string,byte[]?>();
        foreach(var (name,hash) in record.Before)
        {
            if(!FileSet.Allowed(name)||!record.After.ContainsKey(name))throw new InvalidDataException("Invalid rollback manifest.");
            var now=current.TryGetValue(name,out var b)?FileSet.Hash(b):null;
            if(now!=hash&&now!=record.After[name])throw new InvalidOperationException("A managed file changed outside the app. Recovery is paused to preserve it: "+name);
            var path=Path.Combine(folder,"before",name);var bytes=hash!=null?File.ReadAllBytes(path):null;
            if(bytes!=null&&FileSet.Hash(bytes)!=hash)throw new InvalidDataException("Rollback backup failed its integrity check.");
            original[name]=bytes;
        }
        foreach(var (name,bytes) in original)
        {
            Closed();var destination=Path.Combine(target,name);
            if(bytes!=null)Replace(destination,bytes);
            else if(File.Exists(destination))File.Delete(destination); // Only a file created by this exact transaction.
        }
        foreach(var (name,hash) in record.Before){var path=Path.Combine(target,name);if((File.Exists(path)?FileSet.Hash(File.ReadAllBytes(path)):null)!=hash)throw new IOException("Rollback verification failed.");}
        record.State="Restored";JsonIO.Save(Path.Combine(folder,"transaction.json"),record);
    }
}
