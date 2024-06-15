This is an item randomizer for Death's Door.

For a room/transition randomizer, see [Spr3AD's DD_Randomizer][ddr].

[ddr]: https://github.com/SpR3AD1/DD_Randomizer

# How to use

To use this mod, you will need:

- [BepInEx 5.4.22][bie] or later 5.x versions
- [AlternativeGameModes][AGM]
- [ItemChanger][IC]
- [RandomizerCore][RC-r]
- [RandomizerCore.Json][RCJ]

[bie]: https://docs.bepinex.dev/articles/user_guide/installation/index.html
[AGM]: https://github.com/dpinela/DeathsDoor.AlternativeGameModes/releases
[IC]: https://github.com/dpinela/DeathsDoor.ItemChanger/releases
[RC-r]: https://github.com/homothetyhk/RandomizerCore/releases
[RCJ]: https://github.com/homothetyhk/RandomizerCore.Json/releases

After installing BepInEx, install this mod, ItemChanger and RandomizerCore onto the BepInEx
plugins directory. Then launch the game, select an empty save slot, and press left or right
while the "Start" option is selected until it reads "Start Rando". Select that to start
a randomized game.

# Recommended additions

There are some additional pieces of software to enhance your randomizer experience:

- [RecentItemsDisplay][RID] - a mod that shows a list of recently-acquired items
  on a corner of the screen.
- [DD_rando_tracker][DDRT] - a standalone program that reads the savedata for files that
  use ItemChanger (including plando and randomizer files) and displays a summary of the
  current inventory and recently-acquired items.

[RID]: https://github.com/dpinela/DeathsDoor.RecentItemsDisplay
[DDRT]: https://github.com/SpR3AD1/DD_rando_tracker

# Randomizable pools

- Spells and their upgrades
- Weapons
- Giant Souls
- Vitality and Magic Shrines
- Shiny Things
- Life Seeds
- Soul Orbs
- Levers
- Doors
- Green, Yellow and Pink Keys
- Crow Souls

# Configuration

This randomizer offers the following configuration settings:

- Seed: the main seed used for all randomization. May be any string; if left blank, a random seed is generated
  for each new file.
- Light State: whether to start the game in daytime, nighttime, or to choose randomly between the two.
  (defaults to Day)
- Weapon: which weapon to start the game with (defaults to Sword). Can also be randomized.
- Pools: whether to add each of the above randomizable pools to the overall pool.
  For pools that are not selected, all their locations are left as vanilla and their items will not be found elsewhere.
- Extra Life Seeds: adds additional life seeds to the item pool, to make it easier to plant pots for healing or
  to unlock the Green Ancient Tablet of Knowledge check. Use of this setting will create items bearing multiple
  life seeds if enough are added.
- Extra Vitality Shards: adds additional vitality shards to the pool,
  making it easier to get health upgrades and also allowing maximum health to go over the
  vanilla limit of 6.
- Extra Magic Shards: adds additional magic shards to the pool,
  making it easier to get upgrades and also allowing magic points to go over the
  vanilla limit of 6.
- Include Rusty Belltower Key: allows the Rusty Belltower Key location to be removed
  from the pool if Tablets are randomized.
- Green Tablet Door Cost: changes the number of planted pots required to open the tablet
  door in Estate to any number between 0 and 50.

To edit these settings, we recommend use of the [BepInEx configuration manager][biecfg] mod, which provides an
in-game GUI for mod settings (it also works with a few other Death's Door mods). Alternatively, you may edit the
`config/deathsdoor.randomizer.cfg` file in your BepInEx installation folder directly; launching the game with the
randomizer installed will generate this file if it doesn't already exist.

[biecfg]: https://github.com/BepInEx/BepInEx.ConfigurationManager

# Logic

The logic for this randomizer is in the `Randomizer/Logic` directory, in three
files:

- locations.txt - contains logic for all randomizable locations, plus flower pots.
- waypoints.txt - contains logic for various intermediate points that are not themselves
  randomizable locations.
- transitions.txt - contains logic for all scene transitions.

Logic expressions refer to items and locations using the same names they're given in
[ItemChanger][IC-predef], but with spaces replaced by underscores because of syntax
constraints.

The logic is written under the assumption that the player can always return to the starting
point of the game - the main lobby of Hall of Doors - at any time; ItemChanger includes
the necessary patches to ensure that this remains the case when the randomizer is active.

[IC-predef]: https://github.com/dpinela/DeathsDoor.ItemChanger/blob/main/ItemChanger/Predefined.cs

## Waypoints

Most waypoints in the waypoints.txt file represent points of the game's map that are
logic-equivalent to several other locations - that is, it's possible to walk between those
locations without any equipment other than the starting set. Where possible, the logic is
written so that any constraint on passing from one place to another is captured in a single
place; so the majority of these are found in the waypoints and transitions files, and most
locations just reference some existing waypoint for their logic, except for constraints
which apply exclusively to that location.

Some waypoints - those with the keyword `stateless` before their names - have a different
purpose: they represent some action/goal that has a permanent effect on the game, like
rescuing Grunt from Mushroom Dungeon or lighting the lanterns in Old Watchtowers, which
is not a randomizable location but needs to be tracked for logic purposes.

# Acknowledgments

Thanks to Homothety for the [RandomizerCore][RC] library, which provides the randomization
algorithm for this randomizer (the same one used for the [Hollow Knight randomizer][R4]).

Thanks to MrKoiCarp and TheDancingGrad for naming many of the items and locations in the
game.

Thanks to all the people in the DD discord who tried this mod's precursor, [Plando][],
and helped find bugs, SpR3AD in particular.

[RC]: https://github.com/homothetyhk/RandomizerCore
[R4]: https://github.com/homothetyhk/RandomizerMod
[Plando]: https://github.com/dpinela/DeathsDoor.Plando
