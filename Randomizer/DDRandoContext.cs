namespace DDoor.Randomizer;

using Collections = System.Collections.Generic;
using RC = RandomizerCore;

internal class DDRandoContext : RC.RandoContext
{
    private Collections.List<RC.Logic.LogicDef> pots = new();

    private const string initDoor = "lvl_hallofdoors[bus_overridespawn]";

    public DDRandoContext(RC.Logic.LogicManager lm) : base(lm)
    {
        InitialProgression = new RC.RandoTransition(lm.GetTransitionStrict(initDoor));
    }

    public override Collections.IEnumerable<RC.GeneralizedPlacement> EnumerateExistingPlacements()
    {
        foreach (var t in vanillaTransitions)
        {
            var to = LM.GetTransitionStrict($"{t.FromScene}[{t.Door}]");
            var from = LM.GetTransitionStrict($"{t.ToScene}[{t.Door}]");
            yield return new RC.GeneralizedPlacement(to, from);
            yield return new RC.GeneralizedPlacement(from, to);
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

    private static readonly Collections.List<Transition> vanillaTransitions = new()
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

    private static readonly Collections.List<string> potLocations = new()
    {
        "Outside_Catacombs_Exit_Pot",
        "Catacombs_Room_2_Pot",
        "Cemetery_Right_Arena_Pot",
        "Inner_Furnace_Burner_2_Pot",
        "Inner_Furnace_Islands_Pot",
        "Inner_Furnace_Burner_8_Pot",
        "Inner_Furnace_Final_Pot",
        "Lockstone_West_Crow_Pot",
        "Lockstone_West_Keyed_Crow_Pot",
        "Lockstone_Entrance_Pot",
        "Lockstone_East_Crow_Pot",
        "Camp_Castle_Door_Pot",
        "Watchtowers_First_Pot",
        "Watchtowers_Barb_Elevator_Pot",
        "Watchtowers_Lasers_Arena_Pot",
        "Watchtowers_Owl_Pot",
        "Watchtowers_Cave_Entrance_Pot",
        "Ruins_Key_Pot",
        "Ruins_Fire_Plant_Corridor_Pot",
        "Ruins_Right_Arena_Pot",
        "Ruins_Sewer_Pot",
        "Ruins_Left_of_Lord_of_Doors_Arena_Pot",
        "Dungeon_Cobweb_Room_Pot",
        "Dungeon_Water_Arena_Pot",
        "Dungeon_Hall_Pot",
        "Dungeon_Crow_2_Pot",
        "Fortress_Main_Gate_Pot",
        "Fortress_U_Turn_Pot",
        "Fortress_Breakable_Bridges_Pot",
        "Fortress_Bridge_Pot",
        "Fortress_Exit_Pot"
    };
}