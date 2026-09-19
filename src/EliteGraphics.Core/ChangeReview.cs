namespace EliteGraphics.Core;

public static class ChangeReview
{
    public static IReadOnlyList<SettingDiff> Between(FileSet before,FileSet after)
    {
        var result=PresetComparison.Build([new("Before",before,[]),new("After",after,[])],true,true)
            .Select(r=>new SettingDiff(r.Source,r.Setting,r.Cells[0].Value,r.Cells[1].Value)).ToList();
        result.AddRange(GraphicsModel.Diff(before,after).Where(d=>d.Path=="(formatting)"));
        return result;
    }
}
