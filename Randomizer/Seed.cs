using RC = RandomizerCore;
using CG = System.Collections.Generic;

namespace DDoor.Randomizer;

internal record class Seed(
    CG.List<CG.List<RC.RandoPlacement>[]> Placements,
    DDRandoContext Context
) {}