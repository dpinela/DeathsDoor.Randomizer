using BepConfig = BepInEx.Configuration;
using CG = System.Collections.Generic;
using Text = System.Text;
using Crypto = System.Security.Cryptography;

namespace DDoor.Randomizer;

internal class Settings
{
    private BepConfig.ConfigEntry<string> _Seed;
    private BepConfig.ConfigEntry<StartLightState> _StartLightState;
    private BepConfig.ConfigEntry<StartWeapon> _StartWeapon;
    private CG.Dictionary<string, BepConfig.ConfigEntry<bool>> _Pools = new();

    private const string SeedGroup = "Seed";
    private const string StartGroup = "Start";
    private const string PoolsGroup = "Pools";

    public Settings(BepConfig.ConfigFile config)
    {
        _Seed = config.Bind(SeedGroup, "Seed", "", "Randomization seed (leave blank for a random one)");
        _StartLightState = config.Bind(StartGroup, "LightState", StartLightState.Day, "Whether to start at day or at night");
        foreach (var k in Pool.Predefined.Keys)
        {
            _Pools[k] = config.Bind(PoolsGroup, k, true);
        }
        _StartWeapon = config.Bind(StartGroup, "Weapon", StartWeapon.Sword, "Which weapon to give at the start of the game");
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
            Seed = Hash31(seed)
        };
        foreach (var (k, entry) in _Pools)
        {
            gs.Pools[k] = entry.Value;
        }
        return gs;
    }

    private static int Hash31(string s)
    {
        var encoded = new Text.UTF8Encoding().GetBytes(s);
        using var sha = Crypto.SHA256.Create();
        var h = sha.ComputeHash(encoded);
        var u = (uint)h[0] | ((uint)h[1] << 8) | ((uint)h[2] << 16) | (((uint)h[3] & 0x7f) << 24);
        return (int)u;
    }
}