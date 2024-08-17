using CG = System.Collections.Generic;
using RC = RandomizerCore;
using static System.Linq.Enumerable;

namespace DDoor.Randomizer;

internal class HelperLogCostedUpdateEntry : RC.Logic.UpdateEntry
{
    private RC.Logic.ILogicDef location;
    private RC.Logic.LogicCost cost;
    private CG.HashSet<string> reachableLocations;

    public HelperLogCostedUpdateEntry(RC.Logic.ILogicDef location, RC.Logic.LogicCost cost, CG.HashSet<string> reachableLocations)
    {
        this.location = location;
        this.cost = cost;
        this.reachableLocations = reachableLocations;
    }

    public override bool CanGet(RC.Logic.ProgressionManager pm) => location.CanGet(pm) && cost.CanGet(pm);
    
    public override CG.IEnumerable<RC.Logic.Term> GetTerms()
    {
        return location.GetTerms().Concat(cost.GetTerms()).Distinct();
    }

    public override void OnAdd(RC.Logic.ProgressionManager pm)
    {
        reachableLocations.Add(location.Name.Replace("_", " "));
    }
}