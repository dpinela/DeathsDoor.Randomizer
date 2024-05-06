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

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.1.0.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.3")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    private RC.Logic.LogicManager? lm;
    private Settings? modSettings;

    public void Start()
    {
        AGM.AlternativeGameModes.Add("START RANDO", StartRando);

        IC.SaveData.OnTrackerLogUpdate += tlog =>
        {
            if (tlog != null && GameSave.GetSaveData().IsKeyUnlocked(isRandoKey))
            {
                WriteHelperLog(GenerateHelperLog(tlog));
            }
        };
        
        modSettings = new(Config);
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
            var (minSeeds, maxSeeds) = BalancePool(pb);
            pools.Add(pb);
            if (gs.DupeSeeds > 0 && !gs.Pools["Seeds"])
            {
                var seedPB = new PoolBuilder("Seeds Group");
                seedPB.AddItem("Life Seed", 50 + gs.DupeSeeds);
                foreach (var loc in Pool.Predefined["Seeds"].Content)
                {
                    seedPB.AddLocation(loc.Name);
                }
                (minSeeds, maxSeeds) = BalancePool(seedPB);
                pools.Add(seedPB);
            }

            var lmb = LogicLoader.Load();
            LogicLoader.DefineConsolidatedSeedItems(lmb, minSeeds, maxSeeds);
            lm = new(lmb);

            var ctx = new DDRandoContext(lm, gs);
            AddVanillaSword(ctx, gs);

            var stage0 = new RC.Randomization.RandomizationStage
            {
                groups = pools.Select(p => p.MakeGroup(lm)).ToArray(),
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
            var placementGroups = rando.Run()[0];
            var data = IC.SaveData.Open();
            foreach (var g in placementGroups)
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

            GameSave.currentSave.SetCountKey(minSeedsKey, minSeeds);
            GameSave.currentSave.SetCountKey(maxSeedsKey, maxSeeds);
            gs.SaveTo(GameSave.currentSave);
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
    private const string minSeedsKey = "Randomizer-min_seeds";
    private const string maxSeedsKey = "Randomizer-max_seeds";

    private CG.HashSet<string> GenerateHelperLog(CG.List<IC.TrackerLogEntry> tlog)
    {
        if (lm == null)
        {
            var lmb = LogicLoader.Load();
            var minSeeds = GameSave.currentSave.GetCountKey(minSeedsKey);
            var maxSeeds = GameSave.currentSave.GetCountKey(maxSeedsKey);
            LogicLoader.DefineConsolidatedSeedItems(lmb, minSeeds, maxSeeds);
            lm = new(lmb);
        }
        var gs = new GenerationSettings();
        gs.LoadFrom(GameSave.currentSave);
        var ctx = new DDRandoContext(lm, gs);
        AddVanillaSword(ctx, gs);
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

    private static (int, int) BalancePool(PoolBuilder pb)
    {
        var totalItems = pb.Items.Values.Sum();
        var totalLocations = pb.Locations.Values.Sum();
        if (totalItems == totalLocations)
        {
            return (1, 1);
        }
        if (totalItems > totalLocations)
        {
            var excessItems = totalItems - totalLocations;
            var n = pb.Items.TryGetValue("Life Seed", out var x) ? x : 0;
            if (n <= excessItems)
            {
                throw new System.InvalidOperationException($"not enough life seeds in pool to make room for extra items; have {n}, need at least {excessItems + 1}");
            }
            // Consolidate away as many seeds as there are excess items.
            // The remaining pool of seeds is thus n - excessItems.
            var remainingSeeds = n - excessItems;
            var q = excessItems / remainingSeeds;
            var r = excessItems % remainingSeeds;
            pb.RemoveItemAll("Life Seed");
            pb.AddItem($"Life Seed x{1 + q}", remainingSeeds - r);
            pb.AddItem($"Life Seed x{2 + q}", r);
            return r == 0 ? (1 + q, 1 + q) : (1 + q, 2 + q);
        }
        else
        {
            pb.AddItem("100 Souls", totalLocations - totalItems);
            return (1, 1);
        }
    }
}
