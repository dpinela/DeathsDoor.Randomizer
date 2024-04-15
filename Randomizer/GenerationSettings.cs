using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

public class GenerationSettings
{
    public StartLightState StartLightState = StartLightState.Day;
    public StartWeapon StartWeapon = StartWeapon.Sword;
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

    public void SaveTo(GameSave save)
    {
        save.SetCountKey(startLightKey, (int)StartLightState);
        save.SetCountKey(startWeaponKey, (int)StartWeapon);
    }

    public void LoadFrom(GameSave save)
    {
        StartLightState = (StartLightState)save.GetCountKey(startLightKey);
        StartWeapon = (StartWeapon)save.GetCountKey(startWeaponKey);
    }
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
