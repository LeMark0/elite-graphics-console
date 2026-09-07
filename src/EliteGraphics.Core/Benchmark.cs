using System.Diagnostics;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;

namespace EliteGraphics.Core;

public sealed record GpuSample(double Seconds,double? MemoryMiB,double? Utilisation,double? Temperature,double? ClockMHz,double? PowerWatts);
public sealed record FrameSummary(int Count,double AverageFps,double P95Ms,double P99Ms,int HitchesOver50Ms,string Source);
public sealed record BenchmarkRun
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string RevisionId { get; init; } = "";
    public string ProfileName { get; init; } = "";
    public string Mode { get; init; } = "VR";
    public string Scenario { get; set; } = "Orbital approach";
    public string GameBuild { get; init; } = "Unknown";
    public Dictionary<string,string> ActualHashes { get; init; } = new();
    public DateTimeOffset Started { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? Ended { get; set; }
    public bool GameRunningAtStart { get; init; }
    public string RuntimeNotes { get; set; } = "";
    public string OverlayObservations { get; set; } = "";
    public string VisualNotes { get; set; } = "";
    public string TelemetryStatus { get; set; } = "Starting";
    public string Hardware { get; set; } = "Unknown";
    public FrameSummary? Frames { get; set; }
    public List<string> Markers { get; init; } = [];
    public string DisplayName => $"{Started.ToLocalTime():dd MMM HH:mm} · {ProfileName} · {Scenario}";
    public override string ToString() => DisplayName;
}

public sealed class BenchmarkStore
{
    public string Root { get; }
    public BenchmarkStore(string dataRoot) {Root=Path.Combine(dataRoot,"benchmarks");Directory.CreateDirectory(Root);}
    public string Folder(string id){if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Invalid run ID.");return Path.Combine(Root,id);}
    public void Save(BenchmarkRun run)=>JsonIO.Save(Path.Combine(Folder(run.Id),"run.json"),run);
    public IReadOnlyList<BenchmarkRun> List()=>Directory.GetDirectories(Root).Where(x=>File.Exists(Path.Combine(x,"run.json"))).Select(x=>JsonIO.Load<BenchmarkRun>(Path.Combine(x,"run.json"))).OrderByDescending(x=>x.Started).ToList();
    public List<GpuSample> Samples(string id)
    {
        var path=Path.Combine(Folder(id),"gpu.csv");if(!File.Exists(path))return [];
        // A live capture keeps its writer open. Readers must explicitly permit that writer.
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);
        using var reader=new StreamReader(stream);reader.ReadLine();var result=new List<GpuSample>();
        while(reader.ReadLine() is string line){var x=line.Split(',');if(x.Length==6&&GpuRecorder.Number(x[0]) is double seconds)result.Add(new GpuSample(seconds,GpuRecorder.Number(x[1]),GpuRecorder.Number(x[2]),GpuRecorder.Number(x[3]),GpuRecorder.Number(x[4]),GpuRecorder.Number(x[5])));}
        return result;
    }
}

public sealed class GpuRecorder : IDisposable
{
    readonly Stopwatch watch=Stopwatch.StartNew();readonly object gate=new();readonly BenchmarkStore store;readonly StreamWriter writer;
    Process? process;bool disposed;
    public BenchmarkRun Run {get;}
    public event Action<GpuSample>? Sample;
    public GpuRecorder(BenchmarkStore store,BenchmarkRun run)
    {
        this.store=store;Run=run;store.Save(run);
        writer=new StreamWriter(Path.Combine(store.Folder(run.Id),"gpu.csv")){AutoFlush=true};writer.WriteLine("seconds,gpu_memory_mib,gpu_utilisation_percent,gpu_temperature_c,gpu_clock_mhz,gpu_power_w");
    }
    public void Start()
    {
        try
        {
            process=new Process{StartInfo=new ProcessStartInfo("nvidia-smi","--query-gpu=memory.used,utilization.gpu,temperature.gpu,clocks.gr,power.draw --format=csv,noheader,nounits -i 0 -l 1"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true},EnableRaisingEvents=true};
            process.OutputDataReceived+=(_,e)=>{if(e.Data==null)return;var fields=e.Data.Split(',');if(fields.Length!=5)return;GpuSample sample;lock(gate){if(disposed)return;sample=new(watch.Elapsed.TotalSeconds,Number(fields[0]),Number(fields[1]),Number(fields[2]),Number(fields[3]),Number(fields[4]));writer.WriteLine(string.Join(',',new double?[]{sample.Seconds,sample.MemoryMiB,sample.Utilisation,sample.Temperature,sample.ClockMHz,sample.PowerWatts}.Select(x=>x?.ToString("0.###",CultureInfo.InvariantCulture)??"")));}Sample?.Invoke(sample);};
            process.ErrorDataReceived+=(_,e)=>{if(!string.IsNullOrWhiteSpace(e.Data)){lock(gate){if(!disposed){Run.TelemetryStatus="NVIDIA telemetry reported an error; unavailable values stay blank.";store.Save(Run);}}}};
            process.Start();process.BeginOutputReadLine();process.BeginErrorReadLine();Run.TelemetryStatus="Recording whole-GPU samples at 1 Hz; not per-frame VR data.";store.Save(Run);
        }
        catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException){Run.TelemetryStatus="NVIDIA telemetry unavailable: "+ex.Message;store.Save(Run);}
    }
    public static double? Number(string value)=>double.TryParse(value.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out var n)&&double.IsFinite(n)?n:null;
    public void Marker(string label){lock(gate){Run.Markers.Add($"{watch.Elapsed.TotalSeconds:0.0}s · {label}");store.Save(Run);}}
    public void Dispose()
    {
        lock(gate){if(disposed)return;disposed=true;}
        if(process!=null){try{if(!process.HasExited)process.Kill();process.WaitForExit(2000);}catch(InvalidOperationException){}process.Dispose();}
        lock(gate){writer.Dispose();Run.Ended=DateTimeOffset.UtcNow;if(Run.TelemetryStatus.StartsWith("Recording"))Run.TelemetryStatus="Complete · 1 Hz whole-GPU telemetry";store.Save(Run);}
    }
}

public static class PresentMonImport
{
    public static FrameSummary Read(string path,string mode)
    {
        if(mode!="FLAT")throw new InvalidOperationException("Desktop presentation data is only accepted for FLAT runs; it cannot establish VR headset frame delivery.");
        using var parser=new TextFieldParser(path){TextFieldType=FieldType.Delimited,HasFieldsEnclosedInQuotes=true};parser.SetDelimiters(",");
        var header=parser.ReadFields()??throw new InvalidDataException("Empty CSV.");
        int Col(string name)=>Array.FindIndex(header,x=>x.Trim().Equals(name,StringComparison.OrdinalIgnoreCase));
        int time=Col("MsBetweenPresents");if(time<0)time=Col("FrameTime");if(time<0)throw new InvalidDataException("Expected PresentMon MsBetweenPresents or FrameTime (milliseconds).");
        int app=Col("Application"),swap=Col("SwapChainAddress");if(app<0)app=Col("ProcessName");if(app<0)throw new InvalidDataException("CSV must identify Application or ProcessName so Elite's frames can be isolated.");
        var frames=new List<double>();var chains=new HashSet<string>();
        while(!parser.EndOfData){var row=parser.ReadFields();if(row==null||row.Length<=Math.Max(time,app))continue;if(!Path.GetFileName(row[app]).Equals("EliteDangerous64.exe",StringComparison.OrdinalIgnoreCase))continue;var n=GpuRecorder.Number(row[time]);if(n is >0){frames.Add(n.Value);if(swap>=0&&row.Length>swap)chains.Add(row[swap]);}}
        if(chains.Count>1)throw new InvalidDataException("Multiple Elite swapchains found. Export a single relevant swapchain before importing.");
        if(frames.Count<2)throw new InvalidDataException("Not enough valid Elite frames in this capture.");
        frames.Sort();double P(double percentile)=>frames[(int)Math.Ceiling(frames.Count*percentile)-1];
        return new(frames.Count,1000/frames.Average(),P(.95),P(.99),frames.Count(x=>x>50),"PresentMon app presentation intervals; nearest-rank percentiles; hitches >50 ms");
    }
}
