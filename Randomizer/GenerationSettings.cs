using CG = System.Collections.Generic;
using IO = System.IO;
using Json = Newtonsoft.Json;

namespace DDoor.Randomizer;

public class GenerationSettings
{
    public StartLightState StartLightState = StartLightState.Day;
    public StartWeapon StartWeapon = StartWeapon.Sword;
    public CG.Dictionary<string, bool> Pools = new();
    public int DupeSeeds = 0;
    public int DupeVitalityShards = 0;
    public int DupeMagicShards = 0;
    public bool IncludeBelltowerKey;
    public string Seed = "";

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

    public string ToJSON()
    {
        // We are relying on IC's bundled copy of Newtonsoft.JSON for this.
        // Fine for now, but beware of expanding it.
        return Json.JsonConvert.SerializeObject(this, Json.Formatting.Indented, new Json.Converters.StringEnumConverter());
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
