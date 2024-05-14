namespace DDoor.Randomizer;

using CG = System.Collections.Generic;
using RC = RandomizerCore;
using static System.Linq.Enumerable;

internal class DDRandoContext : RC.RandoContext
{
    private const string initDoor = "lvl_hallofdoors[bus_overridespawn]";

    public GenerationSettings gs;

    public CG.Dictionary<string, string> Preplacements = new();
    public CG.Dictionary<string, string> VanillaPlacements = new();

    // RandomizerCore.Json specifically relies on this constructor existing.
    // Used only for deserialization purposes; gs will be set during that process.
    public DDRandoContext(RC.Logic.LogicManager lm) : base(lm)
    {
        gs = null!;
    }

    public DDRandoContext(RC.Logic.LogicManager lm, GenerationSettings gs) : base(lm)
    {
        this.gs = gs;
        var things = new CG.List<RC.ILogicItem>();

        var weapon = lm.GetItemStrict(GenerationSettings.StartWeaponItem(gs.StartWeapon));
        
        things.Add(weapon);
        var lightStateTerm = lm.GetTermStrict(gs.StartLightState switch
        {
            StartLightState.Day => "Daytime",
            StartLightState.Night => "Nighttime",
            _ => throw new System.InvalidOperationException("BUG: StartLightState should not be Random at this point")
        });
        things.Add(new RC.LogicItems.BoolItem("Start Light State", lightStateTerm));
        things.Add(new RC.RandoTransition(lm.GetTransitionStrict(initDoor)));
        InitialProgression = new CompositeItem("Start Progression", things.ToArray());
    }

    public override CG.IEnumerable<RC.GeneralizedPlacement> EnumerateExistingPlacements()
    {
        foreach (var t in vanillaTransitions)
        {
            var to = LM.GetTransitionStrict($"{t.FromScene}[{t.Door}]");
            var from = LM.GetTransitionStrict($"{t.ToScene}[{t.Door}]");
            yield return new RC.GeneralizedPlacement(to, from);
            yield return new RC.GeneralizedPlacement(from, to);
        }

        foreach (var (locName, itemName) in Preplacements.Concat(VanillaPlacements))
        {
            var loc = LM.GetLogicDefStrict(locName.Replace(" ", "_"));
            var item = LM.GetItemStrict(itemName.Replace(" ", "_"));
            yield return new RC.GeneralizedPlacement(item, loc);
        }

        var potItem = LM.GetItemStrict(LogicLoader.PotsTerm);
        foreach (var (name, logicDef) in LM.LogicLookup)
        {
            if (name.StartsWith("Pot-"))
            {
                yield return new RC.GeneralizedPlacement(potItem, logicDef);
            }
        }
    }

    private static readonly CG.List<Transition> vanillaTransitions = new()
    {
        new("lvl_hallofdoors", "sdoor_covenant", "lvlConnect_Fortress_Mountaintops"),
        new("lvl_hallofdoors", "sdoor_betty", "boss_betty"),
        new("lvl_hallofdoors", "sdoor_sailor", "lvl_SailorMountain"),
        new("lvl_hallofdoors", "sdoor_fortress", "lvl_frozenfortress"),
        new("lvl_hallofdoors", "sdoor_mountaintops", "lvl_mountaintops"),
        new("lvl_hallofdoors", "sdoor_forest_dung", "lvl_Forest"),
        new("lvl_hallofdoors", "sdoor_frogboss", "boss_frog"),
        new("lvl_hallofdoors", "sdoor_forest", "lvl_Forest"),
        new("lvl_hallofdoors", "sdoor_swamp", "lvl_Swamp"),
        new("lvl_hallofdoors", "sdoor_graveyard", "lvl_Graveyard"),
        new("lvl_hallofdoors", "sdoor_tutorial", "lvl_Tutorial"),
        new("lvl_hallofdoors", "sdoor_grandmaboss", "boss_grandma"),
        new("lvl_hallofdoors", "sdoor_mansion", "lvl_GrandmaMansion"),
        new("lvl_hallofdoors", "sdoor_basementromp", "lvl_GrandmaBasement"),
        new("lvl_hallofdoors", "sdoor_gardens", "lvl_GrandmaGardens"),
        new("lvl_hallofdoors", "hod_anc_forest", "lvl_Forest"),
        new("lvl_hallofdoors", "hod_anc_fortress", "lvl_frozenfortress"),
        new("lvl_hallofdoors", "hod_anc_mansion", "lvl_GrandmaMansion"),
        new("lvl_Tutorial", "tdoor_gy", "lvl_Graveyard"),

        new("lvl_Graveyard", "d_graveyardtocrypt", "lvlConnect_Graveyard_Gardens"),
        new("lvlConnect_Graveyard_Gardens", "d_crypttogardens", "lvl_GrandmaGardens"),
        new("lvl_GrandmaGardens", "d_gardenstomansion", "lvl_GrandmaMansion"),
        new("lvl_GrandmaMansion", "d_mansiontobasement", "lvlConnect_Mansion_Basement"),
        new("lvlConnect_Mansion_Basement", "d_basementtoromp", "lvl_GrandmaBasement"),
        new("lvl_GrandmaBasement", "d_basementtoboss", "boss_grandma"),

        new("lvl_Graveyard", "d_graveyardtosailorcaves", "lvlconnect_graveyard_sailor"),
        new("lvlconnect_graveyard_sailor", "d_connecttosailor", "lvl_SailorMountain"),
        new("lvl_SailorMountain", "d_sailortofortress", "lvl_frozenfortress"),
        new("lvl_frozenfortress", "d_fortresstoroof", "lvlConnect_Fortress_Mountaintops"),
        new("lvlConnect_Fortress_Mountaintops", "d_CrowCavestoMountaintops", "lvl_mountaintops"),
        new("lvl_mountaintops", "d_mountaintopstobetty", "boss_betty"),

        new("lvl_Graveyard", "forest_buggy", "lvl_Forest"),
        new("lvl_Forest", "d_swamp_enter", "lvl_Swamp"),
        new("lvl_Swamp", "d_frog_boss", "boss_frog")
    };

    private record class Transition(string FromScene, string Door, string ToScene);
}