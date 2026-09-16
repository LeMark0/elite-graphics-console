namespace EliteGraphics.Core;

public sealed class ProfileStore
{
    public string Root { get; }
    public string Profiles => Path.Combine(Root,"profiles");
    public ProfileStore(string root) { Root = Path.GetFullPath(root); Directory.CreateDirectory(Profiles); }
    string Folder(string id) { if(!Guid.TryParseExact(id,"N",out _)) throw new InvalidDataException("Invalid profile ID."); return Path.Combine(Profiles,id); }
    public IReadOnlyList<ProfileRevision> List() => Directory.GetDirectories(Profiles).Where(x=>File.Exists(Path.Combine(x,"revision.json"))).Select(x=>JsonIO.Load<ProfileRevision>(Path.Combine(x,"revision.json"))).OrderByDescending(x=>x.Created).ToList();
    public ProfileRevision Get(string id) => JsonIO.Load<ProfileRevision>(Path.Combine(Folder(id),"revision.json"));
    public string Identity(ProfileRevision revision)
    {
        var visited=new HashSet<string>();
        while(string.IsNullOrEmpty(revision.PresetId)&&revision.ParentId!=null)
        {
            if(!visited.Add(revision.Id))throw new InvalidDataException("Cyclic preset history.");
            var parent=Get(revision.ParentId);
            if(parent.Name!=revision.Name||parent.Mode!=revision.Mode)break;
            revision=parent;
        }
        return string.IsNullOrEmpty(revision.PresetId)?revision.Id:revision.PresetId;
    }
    public IReadOnlyList<ProfileRevision> Heads()=>List().GroupBy(Identity).Select(g=>g.OrderByDescending(p=>p.Created).ThenByDescending(p=>p.Number).First()).ToList();
    public IReadOnlyList<ProfileRevision> History(string id){var key=Identity(Get(id));return List().Where(p=>Identity(p)==key).OrderByDescending(p=>p.Created).ToList();}
    public ProfileRevision Update(string id,string mode,FileSet files,byte[] definitions,string build,string notes)
    {
        var original=Get(id);
        if(original.Protected)throw new InvalidOperationException("Protected baselines can only be forked.");
        var identity=Identity(original);
        if(Heads().Single(p=>Identity(p)==identity).Id!=id)throw new InvalidOperationException("A newer revision exists. Select the latest preset before updating, or fork these changes.");
        return Save(original.Name,mode,files,definitions,build,notes,id,false,false,identity);
    }
    public ProfileRevision Fork(string name,string mode,FileSet files,byte[] definitions,string build,string notes,string? parent=null,bool historical=false)
        =>Save(name,mode,files,definitions,build,notes,parent,false,historical,Guid.NewGuid().ToString("N"));
    public FileSet Files(string id)
    {
        var meta=Get(id); var data=FileSet.Read(Path.Combine(Folder(id),"Graphics")); data.Validate();
        if(data.Count!=meta.Hashes.Count || meta.Hashes.Any(x=>!data.TryGetValue(x.Key,out var b)||FileSet.Hash(b)!=x.Value)) throw new InvalidDataException("Revision integrity check failed. Restore its original files before using it.");
        return data;
    }
    public byte[] Definitions(string id)
    {
        var meta=Get(id);var path=Path.Combine(Folder(id),"Definitions.xml");var bytes=File.Exists(path)?File.ReadAllBytes(path):[];
        if(bytes.Length>0 && FileSet.Hash(bytes)!=meta.DefinitionHash) throw new InvalidDataException("Definition snapshot was modified.");
        if(bytes.Length==0 && meta.DefinitionHash.Length>0) throw new InvalidDataException("Definition snapshot missing.");
        return bytes;
    }
    public ProfileRevision Save(string name, string mode, FileSet data, byte[] definitions, string build, string notes="", string? parentId=null, bool protect=false, bool historical=false,string? presetId=null)
    {
        if(string.IsNullOrWhiteSpace(name)||name.Length>100) throw new ArgumentException("Enter a profile name of 1–100 characters.");
        if(mode is not ("VR" or "FLAT")) throw new ArgumentException("Choose VR or FLAT.");
        data.Validate(); if(definitions.Length>0) _=XmlIO.Read(definitions);
        if(parentId!=null) _=Get(parentId);
        var parent=parentId==null?null:Get(parentId);
        presetId??=parent!=null&&parent.Name==name.Trim()&&parent.Mode==mode?Identity(parent):Guid.NewGuid().ToString("N");
        if(!Guid.TryParseExact(presetId,"N",out _))throw new InvalidDataException("Invalid preset identity.");
        var number=List().Where(x=>Identity(x)==presetId).Select(x=>x.Number).DefaultIfEmpty(0).Max()+1;
        var revision=new ProfileRevision { PresetId=presetId,Name=name.Trim(),Mode=mode,Number=number,ParentId=parentId,Protected=protect,Historical=historical,Status=historical?"Historical / requires migration":"Untested",Notes=notes,GameBuild=build,DefinitionHash=definitions.Length>0?FileSet.Hash(definitions):"",Hashes=data.Hashes() };
        var path=Folder(revision.Id);Directory.CreateDirectory(Path.Combine(path,"Graphics"));
        foreach(var (filename,bytes) in data) File.WriteAllBytes(Path.Combine(path,"Graphics",filename),bytes);
        if(definitions.Length>0) File.WriteAllBytes(Path.Combine(path,"Definitions.xml"),definitions);
        JsonIO.Save(Path.Combine(path,"revision.json"),revision);
        return revision;
    }
}
