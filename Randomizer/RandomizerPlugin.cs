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

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.2.2.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.4")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    private RC.Logic.LogicManager? lm;
    private Settings? modSettings;

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
            
            modSettings = new(Config);
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

    private static RandomizerPlugin? Instance = null;

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
            var gs = modSettings!.GetGS();
            IO.File.WriteAllText(IO.Path.Combine(UE.Application.persistentDataPath, "SAVEDATA", "Randomizer Settings.json"), gs.ToJSON());
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
            lm = new(lmb);

            var ctx = new DDRandoContext(lm, gs);
            AddVanillaSword(ctx, gs);
            RemoveVanillaBelltowerKey(ctx, gs);
            AddVanillaPools(ctx, pb);

            // Store the context for later use by the helper log.
            ctx.Save();

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
            var placementGroups = rando.Run();
            PlacementsHash.Save(placementGroups);
            var data = IC.SaveData.Open();
            foreach (var g in placementGroups[0])
            {
                foreach (var p in g)
                {
                    data.Place(
                        item: p.Item.Name.Replace("_", " "),
                        location: p.Location.Name.Replace("_", " ")
                    );
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
            
        }
        catch (System.Exception err)
        {
            Logger.LogError($"Randomization failed: {err}");
        }
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
        var lm = ctx.LM;
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
            // Preplacements are already accounted for in the vanilla update
            // entries we created above.
            if (ctx.Preplacements.ContainsKey(entry.LocationName))
            {
                continue;
            }
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
