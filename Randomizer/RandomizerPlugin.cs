using Bep = BepInEx;
using AGM = DDoor.AlternativeGameModes;
using RC = RandomizerCore;
using IC = DDoor.ItemChanger;

namespace DDoor.Randomizer;

[Bep.BepInPlugin("deathsdoor.randomizer", "Randomizer", "1.0.0.0")]
[Bep.BepInDependency("deathsdoor.itemchanger", "1.1")]
internal class RandomizerPlugin : Bep.BaseUnityPlugin
{
    public void Start()
    {
        AGM.AlternativeGameModes.Add("START RANDO", () =>
        {
            try
            {
                Logger.LogInfo("Rando Requested");
                var lm = LogicLoader.Load();
                var ctx = new DDRandoContext(lm);
                var stage0 = new RC.Randomization.RandomizationStage
                {
                    groups = new RC.Randomization.RandomizationGroup[]
                    {
                        new()
                        {
                            Items = HODItems(lm),
                            Locations = HODLocations(lm),
                            Label = "Main Group",
                            Strategy = new RC.Randomization.DefaultGroupPlacementStrategy(1)
                        }
                    },
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
                    new System.Random(),
                    ctx,
                    new[] { stage0 },
                    monitor
                );
                var placements = rando.Run()[0][0];
                var data = IC.SaveData.Open();
                foreach (var p in placements)
                {
                    data.Place(
                        item: p.Item.Name.Replace("_", " "),
                        location: p.Location.Name.Replace("_", " ")
                    );
                }
            }
            catch (System.Exception err)
            {
                Logger.LogError($"Randomization failed: {err}");
            }
        });
    }

    private RC.IRandoItem[] HODItems(RC.Logic.LogicManager lm)
    {
        RC.RandoItem Item(string name)
        {
            return new() { item = lm.GetItemStrict(name) };
        }
        return new RC.IRandoItem[]
        {
            Item("Fire"),
            Item("Bomb"),
            Item("Hookshot"),
            Item("Camp_of_the_Free_Crows_Door"),
            Item("Betty's_Lair_Door"),
            Item("Stranded_Sailor_Door"),
            Item("Castle_Lockstone_Door"),
            Item("Old_Watchtowers_Door"),
            Item("Mushroom_Dungeon_Door"),
            Item("Throne_of_the_Frog_King_Door"),
            Item("Overgrown_Ruins_Door"),
            Item("Flooded_Fortress_Door"),
            Item("Lost_Cemetery_Door"),
            Item("Grove_of_Spirits_Door"),
            Item("The_Urn_Witch's_Laboratory_Door"),
            Item("Ceramic_Manor_Door"),
            Item("Inner_Furnace_Door"),
            Item("Estate_of_the_Urn_Witch_Door"),
            Item("Bomb_Exit_Lever"),
            Item("Red_Ancient_Tablet_of_Knowledge"),
            Item("Yellow_Ancient_Tablet_of_Knowledge"),
            Item("Green_Ancient_Tablet_of_Knowledge"),
            Item("Cyan_Ancient_Tablet_of_Knowledge"),
            Item("Blue_Ancient_Tablet_of_Knowledge"),
            Item("Purple_Ancient_Tablet_of_Knowledge"),
            Item("Pink_Ancient_Tablet_of_Knowledge"),
            Item("Ink-Covered_Teddy_Bear"),
            Item("Fire")
        };
    }

    private RC.IRandoLocation[] HODLocations(RC.Logic.LogicManager lm)
    {
        RC.RandoLocation Loc(string name)
        {
            return new() { logic = lm.GetLogicDefStrict(name) };
        }
        return new RC.IRandoLocation[]
        {
            Loc("Bomb_Avarice"),
            Loc("Hookshot_Avarice"),
            Loc("Camp_of_the_Free_Crows_Door"),
            Loc("Betty's_Lair_Door"),
            Loc("Stranded_Sailor_Door"),
            Loc("Castle_Lockstone_Door"),
            Loc("Old_Watchtowers_Door"),
            Loc("Mushroom_Dungeon_Door"),
            Loc("Throne_of_the_Frog_King_Door"),
            Loc("Overgrown_Ruins_Door"),
            Loc("Flooded_Fortress_Door"),
            Loc("Lost_Cemetery_Door"),
            Loc("Grove_of_Spirits_Door"),
            Loc("The_Urn_Witch's_Laboratory_Door"),
            Loc("Ceramic_Manor_Door"),
            Loc("Inner_Furnace_Door"),
            Loc("Estate_of_the_Urn_Witch_Door"),
            Loc("Bomb_Exit_Lever"),
            Loc("Discarded_Umbrella"),
            Loc("Surveillance_Device"),
            Loc("Soul_Orb-Bomb_Secret"),
            Loc("Soul_Orb-Bomb_Return"),
            Loc("Ancient_Door_Scale_Model"),
            Loc("Soul_Orb-Fire_Secret"),
            Loc("Soul_Orb-Hookshot_Return"),
            Loc("Soul_Orb-Hookshot_Secret"),
            Loc("Modern_Door_Scale_Model"),
            Loc("Makeshift_Soul_Key")
        };
    }
}
