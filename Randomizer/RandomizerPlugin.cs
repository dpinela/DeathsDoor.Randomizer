using Bep = BepInEx;
using AGM = DDoor.AlternativeGameModes;
using RC = RandomizerCore;
using IC = DDoor.ItemChanger;
using Collections = System.Collections.Generic;
using static System.Linq.Enumerable;

namespace DDoor.Randomizer;

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.0.0.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.1")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    public void Start()
    {
        AGM.AlternativeGameModes.Add("START RANDO", () =>
        {
            try
            {
                Logger.LogInfo("Rando Requested");
                var lm = LogicLoader.Load();
                var ctx = new DDRandoContext(lm);
                var randoLocs = RandomizableLocations(lm);
                var stage0 = new RC.Randomization.RandomizationStage
                {
                    groups = new RC.Randomization.RandomizationGroup[]
                    {
                        new()
                        {
                            Items = VanillaItems(randoLocs, lm),
                            Locations = MakeLocations(randoLocs, lm),
                            Label = "Main Group",
                            Strategy = new RC.Randomization.DefaultGroupPlacementStrategy(3)
                        }
                    },
                    strategy = new(),
                    label = "stage0"
                };
                var monitor = new RC.RandoMonitor();
                monitor.OnSendEvent += (etype, msg) =>
                {
                    Logger.LogInfo($"Rando Run: {etype}: {msg}");
                };
                monitor.OnError += (err) =>
                {
                    if (err is RC.Exceptions.UnreachableLocationException ule)
                    {
                        Logger.LogError(ule.GetVerboseMessage());
                        throw new System.Exception("dead");
                    }
                };
                var rando = new RC.Randomization.Randomizer(
                    new System.Random(),
                    ctx,
                    new[] { stage0 },
                    monitor
                );
                var placements = rando.Run()[0][0];
                var data = IC.SaveData.Open();
                foreach (var p in placements)
                {
                    data.Place(
                        item: p.Item.Name.Replace("_", " "),
                        location: p.Location.Name.Replace("_", " ")
                    );
                }
            }
            catch (System.Exception err)
            {
                Logger.LogError($"Randomization failed: {err}");
            }
        });
    }

    private Collections.List<PoolLocation> RandomizableLocations(RC.Logic.LogicManager lm)
    {
        var pls = new Collections.List<PoolLocation>();
        foreach (var pool in Pool.All)
        {
            foreach (var loc in pool.Content)
            {
                if (lm.GetLogicDef(loc.Name.Replace(" ", "_")) != null) {
                    pls.Add(loc);
                }
            }
        }
        return pls;
    }

    private RC.IRandoItem[] VanillaItems(Collections.List<PoolLocation> locs, RC.Logic.LogicManager lm)
    {
        return locs.Select(loc => loc.VanillaItem.Replace(" ", "_"))
            .Select(name => new RC.RandoItem() { item = lm.GetItemStrict(name) })
            .ToArray();
    }

    private RC.IRandoLocation[] MakeLocations(Collections.List<PoolLocation> locs, RC.Logic.LogicManager lm)
    {
        return locs.Select(loc => loc.Name.Replace(" ", "_"))
            .Select(name => {
                var loc = new RC.RandoLocation() { logic = lm.GetLogicDefStrict(name) };
                if (name == "Green_Ancient_Tablet_of_Knowledge")
                {
                    loc.AddCost(new PlantedPotCost(lm, 50));
                }
                return loc;
            })
            .ToArray();
    }
}
