using UE = UnityEngine;
using Bep = BepInEx;
using AGM = DDoor.AlternativeGameModes;
using RC = RandomizerCore;
using IC = DDoor.ItemChanger;
using CG = System.Collections.Generic;
using IO = System.IO;
using static System.Linq.Enumerable;

namespace DDoor.Randomizer;

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.0.0.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.1")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    private RC.Logic.LogicManager? lm;

    public void Start()
    {
        AGM.AlternativeGameModes.Add("START RANDO", () =>
        {
            try
            {
                Logger.LogInfo("Rando Requested");
                lm = LogicLoader.Load();
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
                GameSave.currentSave.SetKeyState(isRandoKey, true);
                // Disable the first Grey Crow cutscene so that its invisible
                // hitbox isn't blocking the bridge when entering Cemetery from
                // anything other than the vanilla route.
                GameSave.currentSave.SetKeyState("crow_cut1", true, true);
                WriteHelperLog(GenerateHelperLog(new()));
            }
            catch (System.Exception err)
            {
                Logger.LogError($"Randomization failed: {err}");
            }
        });

        IC.SaveData.OnTrackerLogUpdate += tlog =>
        {
            if (tlog != null && GameSave.GetSaveData().IsKeyUnlocked(isRandoKey))
            {
                WriteHelperLog(GenerateHelperLog(tlog));
            }
        };
    }

    private const string isRandoKey = "Randomizer-is_rando";

    private CG.HashSet<string> GenerateHelperLog(CG.List<IC.TrackerLogEntry> tlog)
    {
        lm ??= LogicLoader.Load();
        var ctx = new DDRandoContext(lm);
        var pm = new RC.Logic.ProgressionManager(lm, ctx);
        pm.mu.AddWaypoints(lm.Waypoints);
        pm.mu.AddTransitions(lm.TransitionLookup.Values);
        var save = IC.SaveData.Open();
        var reachableLocations = new CG.HashSet<string>();
        foreach (var loc in save.NamedPlacements.Keys)
        {
            if (lm.LogicLookup.TryGetValue(loc.Replace(" ", "_"), out var logic))
            {
                pm.mu.AddEntry(new HelperLogUpdateEntry(logic, reachableLocations));
            }
        }
        foreach (var gp in ctx.EnumerateExistingPlacements())
        {
            pm.mu.AddEntry(new HelperLogVanillaUpdateEntry(gp.Location, gp.Item));
        }
        pm.mu.StartUpdating();
        
        foreach (var entry in tlog)
        {
            if (!save.NamedPlacements.TryGetValue(entry.LocationName, out var item))
            {
                Logger.LogError($"Helper log generation: unnamed item at {entry.LocationName}");
                continue;
            }
            var randoItem = lm.GetItemStrict(item.Replace(" ", "_"));
            // no location-dependent items exist in this rando,
            // so we don't need to use the Add(ILogicItem, ILogicDef) overload.
            pm.Add(randoItem);
        }

        foreach (var entry in tlog)
        {
            reachableLocations.Remove(entry.LocationName);
        }

        return reachableLocations;
    }

    private static void WriteHelperLog(CG.IEnumerable<string> uncheckedReachableLocations)
    {
        var sortedLocations = new CG.List<string>(uncheckedReachableLocations);
        sortedLocations.Sort();
        var fileLocation = IO.Path.Combine(UE.Application.persistentDataPath, "SAVEDATA", "Randomizer Helper Log.txt");
        using var helperLog = IO.File.Create(fileLocation);
        using var writer = new IO.StreamWriter(helperLog);
        writer.WriteLine("UNCHECKED REACHABLE LOCATIONS:\n");
        foreach (var loc in sortedLocations)
        {
            writer.WriteLine(loc);
        }
    }

    private CG.List<PoolLocation> RandomizableLocations(RC.Logic.LogicManager lm)
    {
        var pls = new CG.List<PoolLocation>();
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

    private RC.IRandoItem[] VanillaItems(CG.List<PoolLocation> locs, RC.Logic.LogicManager lm)
    {
        return locs.Select(loc => loc.VanillaItem.Replace(" ", "_"))
            .Select(name => new RC.RandoItem() { item = lm.GetItemStrict(name) })
            .ToArray();
    }

    private RC.IRandoLocation[] MakeLocations(CG.List<PoolLocation> locs, RC.Logic.LogicManager lm)
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
