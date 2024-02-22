namespace DDoor.Randomizer;

using Collections = System.Collections.Generic;
using RC = RandomizerCore;

internal class DDRandoContext : RC.RandoContext
{
    private Collections.List<RC.RandoTransition> transitions = new();

    private const string initDoor = "lvl_hallofdoors[bus_overridespawn]";

    public DDRandoContext(RC.Logic.LogicManager lm) : base(lm)
    {
        InitialProgression = new RC.RandoTransition(lm.GetTransitionStrict(initDoor));
        /*foreach (var (name, lt) in lm.TransitionLookup)
        {
            if (name != initDoor)
            {
                transitions.Add(new(lt));
            }
        }*/
    }

    public override Collections.IEnumerable<RC.GeneralizedPlacement> EnumerateExistingPlacements()
    {
        return new RC.GeneralizedPlacement[0];
        /*foreach (var t in transitions)
        {
            yield return new RC.GeneralizedPlacement(t, t);
        }*/
    }

    private static readonly Collections.Dictionary<string, string> vanillaTransitions = new()
    {
        
    };
}