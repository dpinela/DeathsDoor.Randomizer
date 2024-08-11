using BepConfig = BepInEx.Configuration;
using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

internal class Settings
{
    private BepConfig.ConfigEntry<string> _Seed;
    private BepConfig.ConfigEntry<StartLightState> _StartLightState;
    private BepConfig.ConfigEntry<StartWeapon> _StartWeapon;
    private CG.Dictionary<string, BepConfig.ConfigEntry<bool>> _Pools = new();
    private CG.Dictionary<string, BepConfig.ConfigEntry<bool>> _Skips = new();
    private BepConfig.ConfigEntry<int> _DupeSeeds;
    private BepConfig.ConfigEntry<int> _DupeVitalityShards;
    private BepConfig.ConfigEntry<int> _DupeMagicShards;
    private BepConfig.ConfigEntry<bool> _IncludeBelltowerKey;
    private BepConfig.ConfigEntry<int> _GreenTabletDoorCost;

    private const string SeedGroup = "Seed";
    private const string StartGroup = "Start";
    private const string PoolsGroup = "Pools";
    private const string SkipsGroup = "Skips";
    private const string DupesGroup = "Duplicates";
    private const string LongLocationsGroup = "Long Locations";

    public Settings(BepConfig.ConfigFile config)
    {
        _Seed = config.Bind(SeedGroup, "Seed", "", "Randomization seed (leave blank for a random one)");
        _StartLightState = config.Bind(StartGroup, "LightState", StartLightState.Day, "Whether to start at day or at night");
        foreach (var k in Pool.Predefined.Keys)
        {
            _Pools[k] = config.Bind(PoolsGroup, k, true);
        }
        foreach (var addon in LogicLoader.LogicAddons())
        {
            _Skips[addon] = config.Bind(SkipsGroup, addon, false);
        }
        _StartWeapon = config.Bind(StartGroup, "Weapon", StartWeapon.Sword, "Which weapon to give at the start of the game");
        _DupeSeeds = config.Bind(DupesGroup, "Extra Life Seeds", 0, "Add extra life seeds to the game");
        _DupeVitalityShards = config.Bind(DupesGroup, "Extra Vitality Shards", 0, "Add extra vitality shards to the game");
        _DupeMagicShards = config.Bind(DupesGroup, "Extra Magic Shards", 0, "Add extra magic shards to the game");
        _IncludeBelltowerKey = config.Bind(LongLocationsGroup, "Include Rusty Belltower Key", true, "When Shiny Things are randomized, include the Rusty Belltower Key location in the pool (item is always in the pool)");
        _GreenTabletDoorCost = config.Bind(LongLocationsGroup, "Green Tablet Door Cost", 50, new BepConfig.ConfigDescription(
            "Number of planted pots required to open the tablet door in Estate",
            new BepConfig.AcceptableValueRange<int>(0, 50)));
    }

    public GenerationSettings GetGS()
    {
        var seed = _Seed.Value;
        if (string.IsNullOrWhiteSpace(seed))
        {
            seed = System.DateTime.Now.Ticks.ToString();
        }
        var gs = new GenerationSettings()
        {
            StartLightState = _StartLightState.Value,
            StartWeapon = _StartWeapon.Value,
            Seed = seed,
            DupeSeeds = _DupeSeeds.Value,
            DupeVitalityShards = _DupeVitalityShards.Value,
            DupeMagicShards = _DupeMagicShards.Value,
            IncludeBelltowerKey = _IncludeBelltowerKey.Value,
            GreenTabletDoorCost = _GreenTabletDoorCost.Value,
        };
        foreach (var (k, entry) in _Pools)
        {
            gs.Pools[k] = entry.Value;
        }
        foreach (var (k, entry) in _Skips)
        {
            gs.Skips[k] = entry.Value;
        }
        return gs;
    }
}
