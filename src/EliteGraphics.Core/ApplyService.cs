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
    public ApplyService(string dataRoot,Func<bool> running) { history=GraphicsTarget.CheckedPath(Path.Combine(dataRoot,"transactions"));Directory.CreateDirectory(history);history=GraphicsTarget.CheckedPath(history);gameRunning=running; }
    void Closed(){if(gameRunning())throw new InvalidOperationException("Close Elite Dangerous before applying or restoring graphics.");}
    static bool SamePath(string a,string b)=>GraphicsTarget.CheckedPath(a).Equals(GraphicsTarget.CheckedPath(b),StringComparison.OrdinalIgnoreCase);
    public IReadOnlyList<TransactionRecord> List()=>Directory.GetDirectories(GraphicsTarget.CheckedPath(history)).Select(x=>GraphicsTarget.CheckedPath(Path.Combine(x,"transaction.json"))).Where(File.Exists).Select(JsonIO.Load<TransactionRecord>).OrderByDescending(x=>x.Created).ToList();
    string Folder(string id){if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Invalid transaction ID.");return GraphicsTarget.CheckedPath(Path.Combine(history,id));}
    public void Apply(string target,FileSet desired,string expectedFingerprint,byte[] capturedDefinitions,byte[] installedDefinitions,string capturedBuild,string installedBuild,Action<int>? afterWrite=null)
    {
        Closed();target=GraphicsTarget.ValidateDirectory(target);GraphicsTarget.CheckedPath(history);
        using var transaction=new TargetTransaction(target,history);
        desired.Validate();var current=FileSet.Read(target);GraphicsTarget.ValidateSettings(current);GraphicsTarget.ValidateSettings(desired);
        if(current.Fingerprint()!=expectedFingerprint)throw new InvalidOperationException("Graphics changed after the preview. Refresh, inspect the changes, then retry.");
        if(capturedDefinitions.Length==0||installedDefinitions.Length==0||FileSet.Hash(capturedDefinitions)!=FileSet.Hash(installedDefinitions))throw new InvalidOperationException("Installed graphics definitions differ or are missing. Capture/migrate a revision for the current game build.");
        if(capturedBuild=="Unknown"||installedBuild=="Unknown"||capturedBuild!=installedBuild)throw new InvalidOperationException("Game build differs or is unknown. Capture/migrate for this installation first.");
        if(GraphicsModel.ActiveFile(desired)!=GraphicsModel.ActiveFile(current))throw new InvalidOperationException("Custom schema differs. Migrate the historical profile first; newer files will not be deleted.");
        desired=ManagedGraphicsFiles.ForApply(desired,current);
        if(current.Keys.Where(ManagedGraphicsFiles.CanWrite).Except(desired.Keys,StringComparer.OrdinalIgnoreCase).Any())throw new InvalidOperationException("Current graphics contains managed files absent from this revision. Capture/migrate a new revision, or restore the preceding transaction. Apply will not silently retain extra settings or delete them.");
        if(List().Any(x=>x.State=="Prepared"&&SamePath(x.Target,target)))throw new InvalidOperationException("An unfinished transaction needs recovery before applying.");
        var record=new TransactionRecord{Target=Path.GetFullPath(target),Before=desired.Keys.ToDictionary(x=>x,x=>current.TryGetValue(x,out var b)?FileSet.Hash(b):null),After=desired.Hashes()};
        var folder=Folder(record.Id);Directory.CreateDirectory(GraphicsTarget.CheckedPath(Path.Combine(folder,"before")));Directory.CreateDirectory(GraphicsTarget.CheckedPath(Path.Combine(folder,"stage")));
        foreach(var (name,bytes) in desired){if(current.TryGetValue(name,out var original))File.WriteAllBytes(GraphicsTarget.CheckedPath(Path.Combine(folder,"before",name)),original);File.WriteAllBytes(GraphicsTarget.CheckedPath(Path.Combine(folder,"stage",name)),bytes);}
        foreach(var (name,hash) in record.Before)if(hash!=null&&FileSet.Hash(File.ReadAllBytes(Path.Combine(folder,"before",name)))!=hash)throw new IOException("Rollback backup verification failed before apply.");
        JsonIO.Save(Path.Combine(folder,"transaction.json"),record);transaction.Begin();
        try
        {
            Closed();if(FileSet.Read(target).Fingerprint()!=expectedFingerprint)throw new InvalidOperationException("Graphics changed while staging the apply.");
            int n=0;foreach(var (name,bytes) in desired){Closed();CheckCurrent(target,name,record.Before[name]);Replace(Path.Combine(target,name),bytes);afterWrite?.Invoke(++n);}
            foreach(var (name,hash) in record.After)if(FileSet.Hash(File.ReadAllBytes(Path.Combine(target,name)))!=hash)throw new IOException("Applied file verification failed.");
            record.State="Applied";JsonIO.Save(GraphicsTarget.CheckedPath(Path.Combine(folder,"transaction.json")),record);transaction.Complete();
        }
        catch
        {
            // Leave the durable journal intact if recovery cannot complete, rather than masking it as success.
            Restore(record,target,expectedCurrent:null);transaction.Complete();
            throw;
        }
    }
    static void Replace(string destination,byte[] bytes)
    {
        GraphicsTarget.CheckedPath(destination);
        var temp=destination+".egc-"+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllBytes(temp,bytes);File.Move(temp,destination,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void RestorePrevious(string target,string expectedCurrent)
    {
        Closed();target=GraphicsTarget.ValidateDirectory(target);GraphicsTarget.CheckedPath(history);
        using var transaction=new TargetTransaction(target,history);
        GraphicsTarget.ValidateSettings(FileSet.Read(target));
        if(List().Any(x=>x.State=="Prepared"&&SamePath(x.Target,target)))throw new InvalidOperationException("Recover the unfinished transaction before restoring a previous apply.");
        var record=List().FirstOrDefault(x=>x.State=="Applied"&&SamePath(x.Target,target))??throw new InvalidOperationException("No applied transaction is available to restore.");
        transaction.Begin();Restore(record,target,expectedCurrent);transaction.Complete();
    }
    public void RecoverPending(string target)
    {
        Closed();target=GraphicsTarget.ValidateDirectory(target);GraphicsTarget.CheckedPath(history);
        using var transaction=new TargetTransaction(target,history);
        transaction.Begin();
        foreach(var record in List().Where(x=>x.State=="Prepared"&&SamePath(x.Target,target)))Restore(record,target,null);
        transaction.Complete();
    }
    void Restore(TransactionRecord record,string target,string? expectedCurrent)
    {
        Closed();if(!SamePath(record.Target,target))throw new InvalidOperationException("Rollback target mismatch.");
        var current=FileSet.Read(target);if(expectedCurrent!=null&&current.Fingerprint()!=expectedCurrent)throw new InvalidOperationException("Graphics changed after the restore preview.");
        var folder=Folder(record.Id);var original=new Dictionary<string,byte[]?>();
        foreach(var (name,hash) in record.Before)
        {
            if(!ManagedGraphicsFiles.CanWrite(name)||!record.After.ContainsKey(name))throw new InvalidDataException("Rollback contains an unrecognised graphics file. No files were restored: "+name);
            var now=current.TryGetValue(name,out var b)?FileSet.Hash(b):null;
            if(now!=hash&&now!=record.After[name])throw new InvalidOperationException("A managed file changed outside the app. Recovery is paused to preserve it: "+name);
            var path=GraphicsTarget.CheckedPath(Path.Combine(folder,"before",name));var bytes=hash!=null?File.ReadAllBytes(path):null;
            if(bytes!=null&&FileSet.Hash(bytes)!=hash)throw new InvalidDataException("Rollback backup failed its integrity check.");
            original[name]=bytes;
        }
        record.State="Prepared";JsonIO.Save(GraphicsTarget.CheckedPath(Path.Combine(folder,"transaction.json")),record);
        foreach(var (name,bytes) in original)
        {
            Closed();var destination=GraphicsTarget.CheckedPath(Path.Combine(target,name));CheckCurrent(target,name,current.TryGetValue(name,out var existing)?FileSet.Hash(existing):null);
            if(bytes!=null)Replace(destination,bytes);
            else if(File.Exists(destination))File.Delete(destination); // Only a file created by this exact transaction.
        }
        foreach(var (name,hash) in record.Before){var path=Path.Combine(target,name);if((File.Exists(path)?FileSet.Hash(File.ReadAllBytes(path)):null)!=hash)throw new IOException("Rollback verification failed.");}
        record.State="Restored";JsonIO.Save(GraphicsTarget.CheckedPath(Path.Combine(folder,"transaction.json")),record);
    }
    static void CheckCurrent(string target,string name,string? expected)
    {
        var path=GraphicsTarget.CheckedPath(Path.Combine(target,name));
        if((File.Exists(path)?FileSet.Hash(File.ReadAllBytes(path)):null)!=expected)
            throw new InvalidOperationException("A graphics file changed during the transaction. Recovery may be required: "+name);
    }
}
