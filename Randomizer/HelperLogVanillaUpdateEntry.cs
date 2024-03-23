using CG = System.Collections.Generic;
using RC = RandomizerCore;

namespace DDoor.Randomizer;

internal class HelperLogVanillaUpdateEntry : RC.Logic.UpdateEntry
{
    private RC.Logic.ILogicDef location;
    private RC.ILogicItem item;

    public HelperLogVanillaUpdateEntry(RC.Logic.ILogicDef location, RC.ILogicItem item)
    {
        this.location = location;
        this.item = item;
    }

    public override bool CanGet(RC.Logic.ProgressionManager pm) => location.CanGet(pm);
    
    public override CG.IEnumerable<RC.Logic.Term> GetTerms() => location.GetTerms();

    public override void OnAdd(RC.Logic.ProgressionManager pm)
    {
        pm.Add(item, location);
    }
}