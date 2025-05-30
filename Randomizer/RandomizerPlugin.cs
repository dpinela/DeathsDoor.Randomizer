using HL = HarmonyLib;
using UE = UnityEngine;
using Bep = BepInEx;
using AGM = DDoor.AlternativeGameModes;
using RC = RandomizerCore;
using IC = DDoor.ItemChanger;
using CG = System.Collections.Generic;
using IO = System.IO;
using Crypto = System.Security.Cryptography;
using Text = System.Text;
using static System.Linq.Enumerable;

namespace DDoor.Randomizer;

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.4.1.0")]
[Bep.BepInDependency("deathsdoor.alternativegamemodes", "1.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.5")]
[Bep.BepInDependency("deathsdoor.magicui", "1.8")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    private Settings? modSettings;
    // MW-SPECIFIC CODE ALERT
    private Multiworld.MainThread? mwManager;

    public void Start()
    {
        try
        {
            Instance = this;

            AGM.AlternativeGameModes.Add("START RANDO", StartRando);

            IC.SaveData.OnTrackerLogUpdate += tlog =>
            {
                if (tlog != null && GameSave.GetSaveData().IsKeyUnlocked(isRandoKey))
                {
                    WriteHelperLog(GenerateHelperLog(tlog));
                }
            };

            IC.ItemIcons.AddPath(IO.Path.GetDirectoryName(typeof(RandomizerPlugin).Assembly.Location));
            
            modSettings = new(Config);

            // MW-SPECIFIC CODE ALERT
            new Multiworld.MWSettings(Config);
            mwManager = gameObject.AddComponent<Multiworld.MainThread>();
            
            new HL.Harmony("deathsdoor.randomizer").PatchAll();
            InitStatus = 1;
        }
        catch (System.Exception err)
        {
            InitStatus = 2;
            throw err;
        }
    }

    public int InitStatus { get; internal set; } = 0;

    internal static RandomizerPlugin? Instance = null;

    internal static void LogInfo(string msg)
    {
        if (Instance != null)
        {
            Instance.Logger.LogInfo(msg);
        }
    }

    internal static void LogError(string msg)
    {
        if (Instance != null)
        {
            Instance.Logger.LogError(msg);
        }
    }

    private void StartRando()
    {
        try
        {
            // MW-SPECIFIC CODE ALERT
            var seed = mwManager!.BaseSeed ?? GenerateRando();
            var ctx = seed.Context;
            var gs = ctx.gs;
            var placementGroups = seed.Placements;

            IO.File.WriteAllText(IO.Path.Combine(UE.Application.persistentDataPath, "SAVEDATA", "Randomizer Settings.json"), gs.ToJSON());
            // Store the context for later use by the helper log.
            ctx.Save();

            PlacementsHash.Save(placementGroups);
            var data = IC.SaveData.Open();
            foreach (var g in placementGroups[0])
            {
                foreach (var p in g)
                {
                    var locName = p.Location.Name.Replace("_", " ");
                    // MW-SPECIFIC CODE ALERT
                    if (mwManager!.ItemReplacements.TryGetValue(p.Location.Name, out var it))
                    {
                        if (it is string name)
                        {
                            data.Place(item: name.Replace("_", " "), location: locName);
                        }
                        else if (it is IC.Item custom)
                        {
                            data.Place(item: custom, location: locName);
                        }
                        else
                        {
                            LogError($"unknown item {it}");
                        }
                    }
                    else
                    {
                        data.Place(
                            item: p.Item.Name.Replace("_", " "),
                            location: locName
                        );
                    }
                }
            }
            foreach (var (loc, item) in ctx.Preplacements)
            {
                data.Place(item: item, location: loc);
            }

            if (gs.StartLightState == StartLightState.Night)
            {
                // SaveData.populateDataStructure will copy this flag
                // into the savefile later.
                LightNight.nightTime = true;
            }
            var startWeaponId = gs.StartWeapon switch
            {
                StartWeapon.Sword => "sword",
                StartWeapon.Daggers => "daggers",
                StartWeapon.Umbrella => "umbrella",
                StartWeapon.Greatsword => "sword_heavy",
                StartWeapon.Hammer => "hammer",
                _ => throw new System.InvalidOperationException("BUG: StartWeapon should not be Random at this point")
            };

            data.GreenTabletDoorCost = gs.GreenTabletDoorCost;
            
            data.StartingWeapon = startWeaponId;
            // Actually equip the chosen weapon.
            // (IC does not do this for you)
            GameSave.currentSave.weaponId = startWeaponId;

            GameSave.currentSave.SetKeyState(isRandoKey, true);
            // Disable the first Grey Crow cutscene so that its invisible
            // hitbox isn't blocking the bridge when entering Cemetery from
            // anything other than the vanilla route.
            // Write the savefile immediately because otherwise this change
            // does nothing.
            GameSave.currentSave.SetKeyState("crow_cut1", true, true);

            WriteHelperLog(GenerateHelperLog(new()));

            // MW-SPECIFIC CODE ALERT
            var mw = mwManager!.PreparedSaveData;
            if (mw != null)
            {
                Multiworld.MWConnection.Join(mw.ServerAddr, mw.PlayerId, mw.RandoId, mw.RemoteNicknames[mw.PlayerId]);
                Multiworld.SaveData.Current = mw;
                mwManager!.ClearPreparedData();
            }
        }
        catch (System.Exception err)
        {
            Logger.LogError($"Randomization failed: {err}");
        }
    }

    internal static GenerationSettings GetGenerationSettings() => Instance!.modSettings!.GetGS();

    internal Seed GenerateRando()
    {
        var gs = GetGenerationSettings();
        var rng = new System.Random(Hash31(gs.Seed));
        gs.Derandomize(rng);

        var pools = new CG.List<PoolBuilder>();
        var pb = new PoolBuilder("Main Group");
        AddBuiltinPools(pb, gs);
        AddSwordToPool(pb, gs);
        AddDupeSeeds(pb, gs);
        AddDupeShards(pb, gs);
        ExcludeBelltowerKey(pb, gs);
        var bounds = BalancePool(pb);
        pools.Add(pb);
        if (gs.DupeSeeds > 0 && !gs.Pools["Seeds"])
        {
            var seedPB = new PoolBuilder("Seeds Group");
            seedPB.AddItem("Life Seed", 50 + gs.DupeSeeds);
            foreach (var loc in Pool.Predefined["Seeds"].Content)
            {
                seedPB.AddLocation(loc.Name);
            }
            bounds["Life Seed"] = BalancePool(seedPB)["Life Seed"];
            pools.Add(seedPB);
        }
        if (gs.DupeVitalityShards > 0 && !gs.Pools["Shrines"])
        {
            var shrinePB = new PoolBuilder("Vitality Shards Group");
            shrinePB.AddItem("Vitality Shard", 8 + gs.DupeVitalityShards);
            foreach (var loc in Pool.Predefined["Shrines"].Content)
            {
                if (loc.VanillaItem == "Vitality Shard")
                {
                    shrinePB.AddLocation(loc.Name);
                }
            }
            bounds["Vitality Shard"] = BalancePool(shrinePB)["Vitality Shard"];
            pools.Add(shrinePB);
        }
        if (gs.DupeMagicShards > 0 && !gs.Pools["Shrines"])
        {
            var shrinePB = new PoolBuilder("Magic Shards Group");
            shrinePB.AddItem("Magic Shard", 8 + gs.DupeMagicShards);
            foreach (var loc in Pool.Predefined["Shrines"].Content)
            {
                if (loc.VanillaItem == "Magic Shard")
                {
                    shrinePB.AddLocation(loc.Name);
                }
            }
            bounds["Magic Shard"] = BalancePool(shrinePB)["Magic Shard"];
            pools.Add(shrinePB);
        }

        var lmb = LogicLoader.Load(gs.Skips);
        LogicLoader.DefineConsolidatedItems(lmb, bounds);
        var lm = new RC.Logic.LogicManager(lmb);

        var ctx = new DDRandoContext(lm, gs);
        AddVanillaSword(ctx, gs);
        RemoveVanillaBelltowerKey(ctx, gs);
        AddVanillaPools(ctx, pb);

        var stage0 = new RC.Randomization.RandomizationStage
        {
            groups = pools.Select(p => p.MakeGroup(lm, gs.GreenTabletDoorCost)).ToArray(),
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
            rng,
            ctx,
            new[] { stage0 },
            monitor
        );
        return new(rando.Run(), ctx);
    }

    private static int Hash31(string s)
    {
        var encoded = new Text.UTF8Encoding().GetBytes(s);
        using var sha = Crypto.SHA256.Create();
        var h = sha.ComputeHash(encoded);
        var u = (uint)h[0] | ((uint)h[1] << 8) | ((uint)h[2] << 16) | (((uint)h[3] & 0x7f) << 24);
        return (int)u;
    }

    private const string isRandoKey = "Randomizer-is_rando";

    private CG.HashSet<string> GenerateHelperLog(CG.List<IC.TrackerLogEntry> tlog)
    {
        var ctx = DDRandoContext.current!;
        var save = IC.SaveData.Open();
        var reachableLocations = new CG.HashSet<string>();
        var allLocations = new CG.HashSet<string>();
        allLocations.UnionWith(save.NamedPlacements.Keys);
        allLocations.UnionWith(save.UnnamedPlacements.Keys);
        var pm = NewPM(ctx, (loc) => reachableLocations.Add(loc), allLocations);
        var lm = ctx.LM;
        
        foreach (var entry in tlog)
        {
            // Preplacements are already accounted for in the vanilla update
            // entries we created above.
            if (!entry.LocationIsVirtual && ctx.Preplacements.ContainsKey(entry.LocationName))
            {
                continue;
            }
            if (entry.ItemName == "")
            {
                // anonymous item; can't possibly have an effect on logic
                continue;
            }
            var randoItem = lm.GetItemStrict(entry.ItemName.Replace(" ", "_"));
            // no location-dependent items exist in this rando,
            // so we don't need to use the Add(ILogicItem, ILogicDef) overload.
            pm.Add(randoItem);
        }

        foreach (var entry in tlog)
        {
            if (!entry.LocationIsVirtual)
            {
                reachableLocations.Remove(entry.LocationName);
            }
        }

        return reachableLocations;
    }

    internal static CG.IEnumerable<(string, string)> GenerateSpoilerLog(CG.IEnumerable<RC.RandoPlacement> flatPlacements)
    {
        var ctx = DDRandoContext.current!;
        var reachableLocations = new CG.List<string>();
        var locations = flatPlacements.Select(x => x.Location.Name);
        var pm = NewPM(ctx, reachableLocations.Add, locations);
        var placementMap = new CG.Dictionary<string, CG.List<string>>();
        foreach (var p in flatPlacements)
        {
            if (!placementMap.TryGetValue(p.Location.Name, out var items))
            {
                items = new();
                placementMap[p.Location.Name] = items;
            }
            items.Add(p.Item.Name);
        }

        for (var i = 0; i < reachableLocations.Count; i++)
        {
            var uloc = reachableLocations[i].Replace(" ", "_");
            if (!placementMap.TryGetValue(uloc, out var items))
            {
                continue;
            }
            foreach (var item in items)
            {
                yield return (item, uloc);
                // This extends the reachableLocations list, and thus the loop,
                // if the item is a progression item.
                pm.Add(ctx.LM.GetItemStrict(item));
            }
        }
    }

    private static RC.Logic.ProgressionManager NewPM(DDRandoContext ctx, System.Action<string> addToReachable, CG.IEnumerable<string> randomizedLocations)
    {
        var lm = ctx.LM;
        var pm = new RC.Logic.ProgressionManager(lm, ctx);
        pm.mu.AddWaypoints(lm.Waypoints);
        pm.mu.AddTransitions(lm.TransitionLookup.Values);
        foreach (var loc in randomizedLocations)
        {
            // this underscore nonsense is going to cause problems;
            // save data doesn't have them but RC output does
            if (lm.LogicLookup.TryGetValue(loc.Replace(" ", "_"), out var logic))
            {
                // Locations with costs should only appear in the helper/spoiler log
                // once those costs can actually be paid.
                if (loc == "Green Ancient Tablet of Knowledge")
                {
                    pm.mu.AddEntry(new HelperLogCostedUpdateEntry(
                        logic,
                        new PlantedPotCost(lm, ctx.gs.GreenTabletDoorCost),
                        addToReachable
                    ));
                }
                else
                {
                    pm.mu.AddEntry(new HelperLogUpdateEntry(logic, addToReachable));
                }
            }
        }
        foreach (var gp in ctx.EnumerateExistingPlacements())
        {
            pm.mu.AddEntry(new HelperLogVanillaUpdateEntry(gp.Location, gp.Item));
        }
        pm.mu.StartUpdating();
        return pm;
    }

    private static void WriteHelperLog(CG.IEnumerable<string> uncheckedReachableLocations)
    {
        var locationsByArea = uncheckedReachableLocations
            .GroupBy(name => IC.Predefined.TryGetLocation(name, out var loc) ? loc.Area : IC.Area.Unknown)
            .OrderBy(g => g.Key);
        
        var fileLocation = IO.Path.Combine(UE.Application.persistentDataPath, "SAVEDATA", "Randomizer Helper Log.txt");
        using var helperLog = IO.File.Create(fileLocation);
        using var writer = new IO.StreamWriter(helperLog);

        writer.WriteLine("UNCHECKED REACHABLE LOCATIONS:");
        foreach (var group in locationsByArea)
        {
            writer.WriteLine("\n" + IC.AreaName.Of(group.Key) + ":\n");
            foreach (var loc in group.OrderBy(x => x))
            {
                writer.WriteLine(loc);
            }
        }
    }

    private static void AddVanillaSword(DDRandoContext ctx, GenerationSettings gs)
    {
        if (gs.StartWeapon != StartWeapon.Sword && !gs.Pools["Weapons"])
        {
            var loc = GenerationSettings.StartWeaponItem(gs.StartWeapon).Replace("_", " ");
            ctx.Preplacements[loc] = "Reaper's Sword";
        }
    }

    private static void AddVanillaPools(DDRandoContext ctx, PoolBuilder pb)
    {
        foreach (var pool in Pool.Predefined.Values)
        {
            foreach (var pe in pool.Content)
            {
                if (!(pb.ContainsLocation(pe.Name) || ctx.Preplacements.ContainsKey(pe.Name)) && pe.VanillaItem != null)
                {
                    ctx.VanillaPlacements[pe.Name] = pe.VanillaItem;
                }
            }
        }
    }

    private static void AddBuiltinPools(PoolBuilder pb, GenerationSettings gs)
    {
        foreach (var (name, pool) in Pool.Predefined)
        {
            if (gs.Pools[name])
            {
                foreach (var pe in pool.Content)
                {
                    pb.AddLocation(pe.Name);
                    if (pe.VanillaItem != null)
                    {
                        pb.AddItem(pe.VanillaItem);
                    }
                }
            }
        }
    }

    private static void AddSwordToPool(PoolBuilder pb, GenerationSettings gs)
    {
        if (gs.StartWeapon == StartWeapon.Sword)
        {
            return;
        }
        var weaponItem = gs.StartWeapon switch
        {
            StartWeapon.Umbrella => "Discarded Umbrella",
            StartWeapon.Hammer => "Thunder Hammer",
            StartWeapon.Daggers => "Rogue Daggers",
            StartWeapon.Greatsword => "Reaper's Greatsword",
            _ => throw new System.InvalidOperationException($"invalid non-default weapon: {gs.StartWeapon}")
        };
        var n = pb.RemoveItemAll(weaponItem);
        pb.AddItem("Reaper's Sword", n);
    }

    private static void AddDupeSeeds(PoolBuilder pb, GenerationSettings gs)
    {
        if (gs.Pools["Seeds"])
        {
            pb.AddItem("Life Seed", gs.DupeSeeds);
        }
    }

    private static void AddDupeShards(PoolBuilder pb, GenerationSettings gs)
    {
        if (gs.Pools["Shrines"])
        {
            pb.AddItem("Vitality Shard", gs.DupeVitalityShards);
            pb.AddItem("Magic Shard", gs.DupeMagicShards);
        }
    }
    
    private const string belltowerKeyLoc = "Rusty Belltower Key";

    private static void ExcludeBelltowerKey(PoolBuilder pb, GenerationSettings gs)
    {
        if (gs.Pools["Shiny Things"] && !gs.IncludeBelltowerKey)
        {
            pb.RemoveLocation(belltowerKeyLoc);
        }
    }

    private static void RemoveVanillaBelltowerKey(DDRandoContext ctx, GenerationSettings gs)
    {
        if (gs.Pools["Shiny Things"] && !gs.IncludeBelltowerKey)
        {
            // If the key location does not have an item placed, the belltower
            // gate will expect the vanilla key to be obtained, and will not
            // open with the one given by rando.
            // This is a shortcoming of how IC implements drop items.
            // Place a dummy item at that location to work around this.
            ctx.Preplacements[belltowerKeyLoc] = "100 Souls";
        }
    }

    private static readonly string[] compressibleItems = new string[]
    {
        "Life Seed",
        "Vitality Shard",
        "Magic Shard"
    };

    private CG.Dictionary<string, (int, int)> BalancePool(PoolBuilder pb)
    {
        var totalItems = pb.Items.Values.Sum();
        var totalLocations = pb.Locations.Values.Sum();
        if (totalItems == totalLocations)
        {
            return new(0);
        }
        if (totalItems > totalLocations)
        {
            var excessItems = totalItems - totalLocations;
            var compressibleItemCounts = compressibleItems.Select(pb.CountItem).ToArray();
            var totalCompressibleItems = compressibleItemCounts.Sum();
            if (excessItems > totalCompressibleItems - compressibleItems.Length)
            {
                throw new System.InvalidOperationException($"not enough compressible items in pool to make room for extra items; have {totalCompressibleItems}, need at least {excessItems + compressibleItems.Length}");
            }

            // Split the excess items (the amount we need to shrink the item pool by)
            // proportionally among the compressible item kinds.
            var excessItemsByKind = compressibleItemCounts.Select(n => excessItems * n / totalCompressibleItems).ToArray();
            var rest = excessItems - excessItemsByKind.Sum();
            for (var i = 0; i < rest; i++)
            {
                excessItemsByKind[i]++;
            }

            var bounds = new CG.Dictionary<string, (int, int)>();

            // Within each compressible item kind, consolidate away as many items as
            // there are excess items in that kind.
            for (var i = 0; i < compressibleItems.Length; i++)
            {
                if (excessItemsByKind[i] == 0)
                {
                    continue;
                }
                var remainingItems = compressibleItemCounts[i] - excessItemsByKind[i];
                Logger.LogInfo($"removing {excessItemsByKind[i]} and keeping {remainingItems} of {compressibleItems[i]}");
                var q = excessItemsByKind[i] / remainingItems;
                var r = excessItemsByKind[i] % remainingItems;
                pb.RemoveItemAll(compressibleItems[i]);
                pb.AddItem($"{compressibleItems[i]} x{1 + q}", remainingItems - r);
                pb.AddItem($"{compressibleItems[i]} x{2 + q}", r);
                bounds[compressibleItems[i]] = (1 + q, r == 0 ? 1 + q : 2 + q);
            }

            return bounds;
        }
        else
        {
            pb.AddItem("100 Souls", totalLocations - totalItems);
            return new(0);
        }
    }
}
