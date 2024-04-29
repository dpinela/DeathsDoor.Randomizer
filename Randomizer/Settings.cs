using BepConfig = BepInEx.Configuration;
using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

internal class Settings
{
    private BepConfig.ConfigEntry<string> _Seed;
    private BepConfig.ConfigEntry<StartLightState> _StartLightState;
    private BepConfig.ConfigEntry<StartWeapon> _StartWeapon;
    private CG.Dictionary<string, BepConfig.ConfigEntry<bool>> _Pools = new();
    private BepConfig.ConfigEntry<int> _DupeSeeds;

    private const string SeedGroup = "Seed";
    private const string StartGroup = "Start";
    private const string PoolsGroup = "Pools";
    private const string DupesGroup = "Duplicates";

    public Settings(BepConfig.ConfigFile config)
    {
        _Seed = config.Bind(SeedGroup, "Seed", "", "Randomization seed (leave blank for a random one)");
        _StartLightState = config.Bind(StartGroup, "LightState", StartLightState.Day, "Whether to start at day or at night");
        foreach (var k in Pool.Predefined.Keys)
        {
            _Pools[k] = config.Bind(PoolsGroup, k, true);
        }
        _StartWeapon = config.Bind(StartGroup, "Weapon", StartWeapon.Sword, "Which weapon to give at the start of the game");
        _DupeSeeds = config.Bind(DupesGroup, "Extra Life Seeds", 0, "Add extra life seeds to the game");
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
        };
        foreach (var (k, entry) in _Pools)
        {
            gs.Pools[k] = entry.Value;
        }
        return gs;
    }
}
