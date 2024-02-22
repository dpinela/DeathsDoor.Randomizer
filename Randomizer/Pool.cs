namespace DDoor.Randomizer;

using Collections = System.Collections.Generic;
using static System.Linq.Enumerable;

internal class Pool
{
    public Collections.List<PoolLocation> Content;

    public Pool(params PoolLocation[] locations) {
        Content = new(locations);
    }

    public static Pool Uniform(string item, params string[] locations)
    {
        return new Pool(locations.Select(loc => new PoolLocation(name: loc, vanillaItem: item)).ToArray());
    }

    public static readonly Pool Spells = new(
        new(name: "Fire Avarice", vanillaItem: "Fire"),
        new(name: "Bomb Avarice", vanillaItem: "Bomb"),
        new(name: "Hookshot Avarice", vanillaItem: "Hookshot"),
        new(name: "Fire Silent Servant", vanillaItem: "Fire"),
        new(name: "Bomb Silent Servant", vanillaItem: "Bomb"),
        new(name: "Hookshot Silent Servant", vanillaItem: "Hookshot"),
        new(name: "Arrow Silent Servant", vanillaItem: "Arrow Level 2")
    );

    public static readonly Pool Weapons = new(
        new("Rogue Daggers"),
        new("Discarded Umbrella"),
        new("Reaper's Greatsword"),
        new("Thunder Hammer")
    );

    public static readonly Pool GiantSouls = new(
        new(name: "Betty", vanillaItem: "Giant Soul of Betty"),
        new(name: "Frog King", vanillaItem: "Giant Soul of The Frog King"),
        new(name: "Grandma", vanillaItem: "Giant Soul of The Urn Witch")
    );

    public static readonly Pool Shrines = new(
        new(name: "Heart Shrine-Cemetery Behind Entrance", vanillaItem: "Vitality Shard"),
        new(name: "Magic Shrine-Cemetery After Smough Arena", vanillaItem: "Magic Shard"),
        new(name: "Heart Shrine-Cemetery Winding Bridge End", vanillaItem: "Vitality Shard"),
        new(name: "Heart Shrine-Hookshot Arena", vanillaItem: "Vitality Shard"),
        new(name: "Magic Shrine-Sailor Turncam", vanillaItem: "Magic Shard"),
        new(name: "Magic Shrine-Lockstone Secret West", vanillaItem: "Magic Shard"),
        new(name: "Heart Shrine-Camp Ice Skating", vanillaItem: "Vitality Shard"),
        new(name: "Magic Shrine-Ruins Hookshot Arena", vanillaItem: "Magic Shard"),
        new(name: "Magic Shrine-Ruins Turncam", vanillaItem: "Magic Shard"),
        new(name: "Heart Shrine-Dungeon Water Arena", vanillaItem: "Vitality Shard"),
        new(name: "Heart Shrine-Ruins Sewer", vanillaItem: "Vitality Shard"),
        new(name: "Magic Shrine-Fortress Bow Secret", vanillaItem: "Magic Shard"),
        new(name: "Magic Shrine-Estate Left of Manor", vanillaItem: "Magic Shard"),
        new(name: "Heart Shrine-Garden of Life", vanillaItem: "Vitality Shard"),
        new(name: "Magic Shrine-Manor Bathroom Puzzle", vanillaItem: "Magic Shard"),
        new(name: "Heart Shrine-Furnace Cart Puzzle", vanillaItem: "Vitality Shard")
    );

    public static readonly Pool ShinyThings = new(
        new("Engagement Ring"),
        new("Old Compass"),
        new("Incense"),
        new("Undying Blossom"),
        new("Old Photograph"),
        new("Sludge-Filled Urn"),
        new("Token of Death"),
        new("Rusty Garden Trowel"),
        new("Captain's Log"),
        new("Giant Arrowhead"),
        new("Malformed Seed"),
        new("Corrupted Antler"),
        new("Magical Forest Horn"),
        new("Ancient Crown"),
        new("Grunt's Old Mask"),
        new("Ancient Door Scale Model"),
        new("Modern Door Scale Model"),
        new("Rusty Belltower Key"),
        new("Surveillance Device"),
        new("Shiny Medallion"),
        new("Ink-Covered Teddy Bear"),
        new("Death's Contract"),
        new("Makeshift Soul Key"),
        new("Mysterious Locket")
    );

    public static readonly Pool Seeds = Pool.Uniform(
        "Life Seed",

        "Seed-Cemetery Broken Bridge",
        "Seed-Catacombs Tower",
        "Seed-Cemetery Left of Main Entrance",
        "Seed-Cemetery Near Tablet Gate",
        "Seed-Between Cemetery and Sailor",
        "Seed-Sailor Upper",
        "Seed-Lockstone Upper East",
        "Seed-Lockstone Soul Door",
        "Seed-Lockstone Behind Bars",
        "Seed-Lockstone Entrance West",
        "Seed-Lockstone North",
        "Seed-Camp Ledge With Huts",
        "Seed-Camp Stall",
        "Seed-Camp Rooftops",
        "Seed-Watchtowers Ice Skating",
        "Seed-Watchtowers Tablet Door",
        "Seed-Watchtowers Archer Platform",
        "Seed-Watchtowers Boxes",
        "Seed-Dungeon Fire Puzzle Near Water Arena",
        "Seed-Ruins Lord of Doors Arena",
        "Seed-Ruins Fire Plant Corridor",
        "Seed-Dungeon Water Arena Left Exit",
        "Seed-Ruins Right Middle",
        "Seed-Ruins On Settlement Wall",
        "Seed-Ruins Behind Boxes",
        "Seed-Ruins Down Through Bomb Wall",
        "Seed-Dungeon Above Crow 2",
        "Seed-Dungeon Right Above Key",
        "Seed-Dungeon Avarice Bridge",
        "Seed-Fortress Watchtower",
        "Seed-Fortress Statue",
        "Seed-Fortress Bridge",
        "Seed-Fortress Intro Crate",
        "Seed-Fortress East After Statue",
        "Seed-Estate Family Tomb",
        "Seed-Estate Entrance",
        "Seed-Estate Hedge Gaps",
        "Seed-Garden of Joy",
        "Seed-Manor Boxes",
        "Seed-Manor Near Brazier",
        "Seed-Manor Below Big Pot Arena",
        "Seed-Manor Rafters",
        "Seed-Manor Main Room Upper",
        "Seed-Manor Spinny Pot Room",
        "Seed-Manor Library Shelf",
        "Seed-Cart Puzzle",
        "Seed-Furnace Entrance",
        "Seed-Inner Furnace Horizontal Pistons",
        "Seed-Inner Furnace Conveyor Bridge",
        "Seed-Inner Furnace Conveyor Gauntlet"
    );

    public static readonly Pool SoulOrbs = Pool.Uniform(
        "100 Souls",

        "Soul Orb-Fire Return Upper",
        "Soul Orb-Hookshot Secret",
        "Soul Orb-Bomb Return",
        "Soul Orb-Bomb Secret",
        "Soul Orb-Hookshot Return",
        "Soul Orb-Fire Return Lower",
        "Soul Orb-Fire Secret",
        "Soul Orb-Catacombs Exit",
        "Soul Orb-Cemetery Hookshot Towers",
        "Soul Orb-Cemetery Under Bridge",
        "Soul Orb-Cemetery East Tree",
        "Soul Orb-Cemetery Winding Bridge End",
        "Soul Orb-Catacombs Room 2",
        "Soul Orb-Catacombs Room 1",
        "Soul Orb-Cemetery Gated Sewer",
        "Soul Orb-Catacombs Entrance",
        "Soul Orb-Cemetery Cave",
        "Soul Orb-Sailor Turncam",
        "Soul Orb-North Lockstone Sewer",
        "Soul Orb-Lockstone Hookshot North",
        "Soul Orb-Lockstone Exit",
        "Soul Orb-West Lockstone Sewer",
        "Soul Orb-Camp Rooftops",
        "Soul Orb-Camp Broken Bridge",
        "Soul Orb-Watchtowers Behind Barb Elevator",
        "Soul Orb-Dungeon Vine",
        "Soul Orb-Ruins Stump",
        "Soul Orb-Ruins Outside Left Dungeon Exit",
        "Soul Orb-Dungeon Cobweb",
        "Soul Orb-Ruins Left After Key Door",
        "Soul Orb-Ruins Lower Bomb Wall",
        "Soul Orb-Ruins Lord of Doors Arena Hookshot",
        "Soul Orb-Dungeon Lower Entrance",
        "Soul Orb-Ruins Upper Above Horn",
        "Soul Orb-Ruins Above Three Plants",
        "Soul Orb-Ruins Up Turncam Ladder",
        "Soul Orb-Ruins Above Entrance Gate",
        "Soul Orb-Ruins Upper Bomb Wall",
        "Soul Orb-Dungeon Left Exit Shelf",
        "Soul Orb-Ruins Lower Hookshot",
        "Soul Orb-Fortress Bomb",
        "Soul Orb-Fortress Hidden Sewer",
        "Soul Orb-Fortress Drop",
        "Soul Orb-Estate Access Crypt",
        "Soul Orb-Estate Balcony",
        "Soul Orb-Garden of Love Turncam",
        "Soul Orb-Garden of Life Hookshot Loop",
        "Soul Orb-Garden of Love Bomb Walls",
        "Soul Orb-Garden of Life Bomb Wall",
        "Soul Orb-Estate Broken Boardwalk",
        "Soul Orb-Estate Secret Cave",
        "Soul Orb-Estate Twin Benches",
        "Soul Orb-Estate Sewer Middle",
        "Soul Orb-Estate Sewer End",
        "Soul Orb-Garden of Peace",
        "Soul Orb-Manor Imp Loft",
        "Soul Orb-Manor Library Shelf",
        "Soul Orb-Manor Chandelier",
        "Soul Orb-Furnace Lantern Chain",
        "Soul Orb-Small Room",
        "Soul Orb-Furnace Entrance Sewer"
    );

    public static readonly Pool TruthTablets = new(
        new("Red Ancient Tablet of Knowledge"),
        new("Yellow Ancient Tablet of Knowledge"),
        new("Green Ancient Tablet of Knowledge"),
        new("Cyan Ancient Tablet of Knowledge"),
        new("Blue Ancient Tablet of Knowledge"),
        new("Purple Ancient Tablet of Knowledge"),
        // This is not the actual vanilla situation, but no logic
        // actually depends on tablets so that's fine.
        new(name: "Estate Owl", vanillaItem: "Pink Ancient Tablet of Knowledge"),
        new(name: "Ruins Owl", vanillaItem: "Pink Ancient Tablet of Knowledge"),
        new(name: "Watchtowers Owl", vanillaItem: "Pink Ancient Tablet of Knowledge")
    );

    public static readonly Pool Levers = new(
        new("Bomb Exit Lever"),
        new("Cemetery Sewer Lever"),
        new("Guardian of the Door Access Lever"),
        new("Cemetery Shortcut to East Tree Lever"),
        new("Cemetery East Tree Lever"),
        new("Catacombs Tower Lever"),
        new("Cemetery Exit to Estate Lever"),
        new("Catacombs Exit Lever"),
        new("Hookshot Silent Servant Lever"),
        new("Sailor Turncam Upper Lever"),
        new("Sailor Turncam Lower Lever"),
        new("Sailor Greatsword Gate Lever"),
        new("Lockstone East Start Shortcut Lever"),
        new("Lockstone Entrance Lever"),
        new("Lockstone East Lower Lever"),
        new("Lockstone Upper Shortcut Lever"),
        new("Lockstone Dual Laser Puzzle Lever"),
        new("Lockstone Tracking Beam Puzzle Lever"),
        new("Lockstone Vertical Laser Puzzle Lever"),
        new("Lockstone North Puzzle Lever"),
        new("Lockstone Secret West Lever"),
        new("Lockstone Hookshot Puzzle Lever"),
        new("Lockstone Upper Puzzle Lever"),
        new("Lockstone Upper Dual Laser Puzzle Lever"),
        new("Watchtowers Before Ice Arena Lever"),
        new("Watchtowers After Ice Skating Lever"),
        new("Watchtowers After Boomers Lever"),
        new("Ruins Settlement Gate Lever"),
        new("Ruins Upper Dungeon Entrance Lever"),
        new("Ruins Ladder Left of Lord of Doors Arena Lever"),
        new("Ruins Entrance Ladder Shortcut Lever"),
        new("Ruins Main Gate Lever"),
        new("Dungeon Entrance Right Gate Lever"),
        new("Dungeon Entrance Left Gate Lever"),
        new("Dungeon Above Crow 2 Lever"),
        new("Fortress Bomb Lever"),
        new("Fortress Main Gate Lever"),
        new("Fortress Central Shortcut Lever"),
        new("Fortress Statue Lever"),
        new("Fortress Watchtower Lower Lever"),
        new("Fortress Bridge Ladder"),
        new("Fortress Pre-Main Gate Lever"),
        new("Fortress Watchtower Upper Lever"),
        new("Fortress North West Lever"),
        new("Estate Elevator Left Lever"),
        new("Estate Elevator Right Lever"),
        new("Garden of Love Lever"),
        new("Garden of Life End Lever"),
        new("Garden of Peace Lever"),
        new("Garden of Joy Lever"),
        new("Garden of Love Turncam Lever"),
        new("Garden of Life Lanterns Lever"),
        new("Estate Underground Urn Shed Lever"),
        new("Manor Big Pot Arena Lever"),
        new("Manor Bookshelf Shortcut Lever")
    );

    public static readonly Pool Doors = new(
        new("Grove of Spirits Door"),
        new("Lost Cemetery Door"),
        new("Stranded Sailor Door"),
        new("Castle Lockstone Door"),
        new("Camp of the Free Crows Door"),
        new("Old Watchtowers Door"),
        new("Betty's Lair Door"),
        new("Overgrown Ruins Door"),
        new("Mushroom Dungeon Door"),
        new("Flooded Fortress Door"),
        new("Throne of the Frog King Door"),
        new("Estate of the Urn Witch Door"),
        new("Ceramic Manor Door"),
        new("Inner Furnace Door"),
        new("The Urn Witch's Laboratory Door")
    );

    public static readonly Pool Keys = new(
        new(name: "Key-Cemetery Central", vanillaItem: "Pink Key"),
        new(name: "Key-Cemetery Grey Crow", vanillaItem: "Pink Key"),
        new(name: "Key-Camp of the Free Crows", vanillaItem: "Pink Key"),
        new(name: "Key-Lockstone West", vanillaItem: "Pink Key"),
        new(name: "Key-Lockstone North", vanillaItem: "Pink Key"),
        new(name: "Key-Overgrown Ruins", vanillaItem: "Green Key"),
        new(name: "Key-Dungeon Hall", vanillaItem: "Green Key"),
        new(name: "Key-Dungeon Right", vanillaItem: "Green Key"),
        new(name: "Key-Dungeon Near Water Arena", vanillaItem: "Green Key"),
        new(name: "Key-Manor Under Dining Room", vanillaItem: "Yellow Key"),
        new(name: "Key-Manor After Spinny Pot Room", vanillaItem: "Yellow Key"),
        new(name: "Key-Manor Library", vanillaItem: "Yellow Key")
    );

    public static readonly Collections.List<Pool> All = new()
    {
        Spells,
        Weapons,
        GiantSouls,
        Shrines,
        ShinyThings,
        Seeds,
        SoulOrbs,
        TruthTablets,
        Levers,
        Doors,
        Keys
    };
}

internal class PoolLocation
{
    public string Name;
    public string VanillaItem;

    public PoolLocation(string name, string vanillaItem)
    {
        Name = name;
        VanillaItem = vanillaItem;
    }

    public PoolLocation(string vanillaItem) : this(vanillaItem, vanillaItem) {}
}
