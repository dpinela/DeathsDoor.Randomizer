using CG = System.Collections.Generic;
using RC = RandomizerCore;

namespace DDoor.Randomizer;

internal class HelperLogUpdateEntry : RC.Logic.UpdateEntry
{
    private RC.Logic.ILogicDef location;
    private CG.HashSet<string> reachableLocations;

    public HelperLogUpdateEntry(RC.Logic.ILogicDef location, CG.HashSet<string> reachableLocations)
    {
        this.location = location;
        this.reachableLocations = reachableLocations;
    }

    public override bool CanGet(RC.Logic.ProgressionManager pm) => location.CanGet(pm);
    
    public override CG.IEnumerable<RC.Logic.Term> GetTerms() => location.GetTerms();

    public override void OnAdd(RC.Logic.ProgressionManager pm)
    {
        reachableLocations.Add(location.Name.Replace("_", " "));
    }
}