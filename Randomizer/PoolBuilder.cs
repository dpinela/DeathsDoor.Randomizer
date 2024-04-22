using CG = System.Collections.Generic;
using RC = RandomizerCore;

namespace DDoor.Randomizer;

public class PoolBuilder
{
    private CG.Dictionary<string, int> items = new();
    private CG.Dictionary<string, int> locations = new();

    public CG.IReadOnlyDictionary<string, int> Items => items;
    public CG.IReadOnlyDictionary<string, int> Locations => locations;

    public void AddItem(string item, int howMany = 1)
    {
        if (!items.TryGetValue(item, out var n))
        {
            n = 0;
        }
        items[item] = n + howMany;
    }

    public int RemoveItem(string item)
    {
        if (items.TryGetValue(item, out var n))
        {
            items.Remove(item);
            return n;
        }
        return 0;
    }

    public void AddLocation(string location, int howMany = 1)
    {
        if (!locations.TryGetValue(location, out var n))
        {
            n = 0;
        }
        locations[location] = n + howMany;
    }

    internal RC.IRandoItem[] MakeItems(RC.Logic.LogicManager lm)
    {
        var list = new CG.List<RC.IRandoItem>();
        foreach (var (name, n) in items)
        {
            for (var i = 0; i < n; i++)
            {
                list.Add(new RC.RandoItem() { item = lm.GetItemStrict(name.Replace(" ", "_")) });
            }
        }
        list.Sort((a, b) => a.Name.CompareTo(b.Name));
        return list.ToArray();
    }

    internal RC.IRandoLocation[] MakeLocations(RC.Logic.LogicManager lm)
    {
        var list = new CG.List<RC.IRandoLocation>();
        foreach (var (name, n) in locations)
        {
            for (var i = 0; i < n; i++)
            {
                var loc = new RC.RandoLocation() { logic = lm.GetLogicDefStrict(name.Replace(" ", "_")) };
                if (name == "Green Ancient Tablet of Knowledge")
                {
                    loc.AddCost(new PlantedPotCost(lm, 50));
                }
                list.Add(loc);
            }
        }
        list.Sort((a, b) => a.Name.CompareTo(b.Name));
        return list.ToArray();
    }
}
