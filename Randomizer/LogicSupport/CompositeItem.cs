using RC = RandomizerCore;
using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

internal class CompositeItem : RC.ILogicItem
{
    private readonly RC.ILogicItem[] components;

    public string Name { get; }

    public CompositeItem(string name, params RC.ILogicItem[] items)
    {
        Name = name;
        components = items;
    }

    public void AddTo(RC.Logic.ProgressionManager pm)
    {
        foreach (var it in components)
        {
            it.AddTo(pm);
        }
    }

    public CG.IEnumerable<RC.Logic.Term> GetAffectedTerms()
    {
        var seenIds = new CG.HashSet<int>();
        foreach (var it in components)
        {
            foreach (var t in it.GetAffectedTerms())
            {
                if (seenIds.Contains(t.Id))
                {
                    continue;
                }

                seenIds.Add(t.Id);
                yield return t;
            }
        }
    }
}