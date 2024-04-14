using BepConfig = BepInEx.Configuration;

namespace DDoor.Randomizer;

internal class Settings
{
    private BepConfig.ConfigEntry<StartLightState> _StartLightState;
    private BepConfig.ConfigEntry<StartWeapon> _StartWeapon;

    private const string StartGroup = "Start";
    private const string PoolsGroup = "Pools";

    public Settings(BepConfig.ConfigFile config)
    {
        _StartLightState = config.Bind(StartGroup, "LightState", StartLightState.Day, "Whether to start at day or at night");
        _StartWeapon = config.Bind(StartGroup, "Weapon", StartWeapon.Sword, "Which weapon to give at the start of the game");
    }

    public GenerationSettings GetGS()
    {
        return new()
        {
            StartLightState = _StartLightState.Value,
            StartWeapon = _StartWeapon.Value
        };
    }
}
