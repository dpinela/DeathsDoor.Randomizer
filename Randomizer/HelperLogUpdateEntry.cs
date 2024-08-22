using CG = System.Collections.Generic;
using RC = RandomizerCore;

namespace DDoor.Randomizer;

internal class HelperLogUpdateEntry : RC.Logic.UpdateEntry
{
    private RC.Logic.ILogicDef location;
    private System.Action<string> addToReachable;

    public HelperLogUpdateEntry(RC.Logic.ILogicDef location, System.Action<string> addToReachable)
    {
        this.location = location;
        this.addToReachable = addToReachable;
    }

    public override bool CanGet(RC.Logic.ProgressionManager pm) => location.CanGet(pm);
    
    public override CG.IEnumerable<RC.Logic.Term> GetTerms() => location.GetTerms();

    public override void OnAdd(RC.Logic.ProgressionManager pm)
    {
        addToReachable(location.Name.Replace("_", " "));
    }
}