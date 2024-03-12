using RC = RandomizerCore;
using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

internal class PlantedPotCost : RC.Logic.LogicCost
{
    private RC.Logic.Term lifeSeedTerm, potTerm;
    private int potThreshold;

    public PlantedPotCost(RC.Logic.LogicManager lm, int threshold)
    {
        lifeSeedTerm = lm.GetTermStrict("Life_Seed");
        potTerm = lm.GetTermStrict(LogicLoader.PotsTerm);
        potThreshold = threshold;
    }

    public override bool CanGet(RC.Logic.ProgressionManager pm) =>
        pm.Has(lifeSeedTerm, potThreshold) && pm.Has(potTerm, potThreshold);
    
    public override CG.IEnumerable<RC.Logic.Term> GetTerms()
    {
        yield return lifeSeedTerm;
        yield return potTerm;
    }

    public override string ToString()
    {
        return $"(planted pots >= {potThreshold})";
    }
}