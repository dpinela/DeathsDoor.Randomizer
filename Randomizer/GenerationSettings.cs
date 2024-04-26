using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

public class GenerationSettings
{
    public StartLightState StartLightState = StartLightState.Day;
    public StartWeapon StartWeapon = StartWeapon.Sword;
    public CG.Dictionary<string, bool> Pools = new();
    public int DupeSeeds = 0;
    public int Seed = 777_777_777;

    public void Derandomize(System.Random rng)
    {
        if (StartLightState == StartLightState.Random)
        {
            var i = rng.Next((int)StartLightState.Random);
            StartLightState = (StartLightState)i;
        }
        if (StartWeapon == StartWeapon.Random)
        {
            var i = rng.Next((int)StartWeapon.Random);
            StartWeapon = (StartWeapon)i;
        }
    }

    private const string startLightKey = "Randomizer-start_light";
    private const string startWeaponKey = "Randomizer-start_weapon";
    private const string poolKeyPrefix = "Randomizer-pool_";

    public void SaveTo(GameSave save)
    {
        save.SetCountKey(startLightKey, (int)StartLightState);
        save.SetCountKey(startWeaponKey, (int)StartWeapon);
        foreach (var (k, on) in Pools)
        {
            save.SetKeyState(poolKeyPrefix + k, on);
        }
    }

    public void LoadFrom(GameSave save)
    {
        StartLightState = (StartLightState)save.GetCountKey(startLightKey);
        StartWeapon = (StartWeapon)save.GetCountKey(startWeaponKey);
        foreach (var k in Pool.Predefined.Keys)
        {
            Pools[k] = save.IsKeyUnlocked(poolKeyPrefix + k);
        }
    }

    public static string StartWeaponItem(StartWeapon w) => w switch
    {
        StartWeapon.Sword => "Reaper's_Sword",
        StartWeapon.Umbrella => "Discarded_Umbrella",
        StartWeapon.Hammer => "Thunder_Hammer",
        StartWeapon.Daggers => "Rogue_Daggers",
        StartWeapon.Greatsword => "Reaper's_Greatsword",
        _ => throw new System.InvalidOperationException("BUG: StartWeapon should not be Random at this point")
    };
}

public enum StartLightState
{
    Day,
    Night,
    Random
}

public enum StartWeapon
{
    Sword,
    Umbrella,
    Daggers,
    Hammer,
    Greatsword,
    Random
}
