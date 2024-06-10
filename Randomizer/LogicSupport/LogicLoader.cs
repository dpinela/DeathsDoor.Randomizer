namespace DDoor.Randomizer;

using CG = System.Collections.Generic;
using IO = System.IO;
using RC = RandomizerCore;
using LFT = RandomizerCore.Logic.LogicFileType;

internal static class LogicLoader
{
    public const string JeffersonStateTerm = "NO_JEFFERSON";
    public const string PotsTerm = "POTS";

    public static RC.Logic.LogicManagerBuilder Load()
    {
        var lmb = new RC.Logic.LogicManagerBuilder();
        var uniqueItems = new CG.HashSet<string>();
        foreach (var pool in Pool.Predefined.Values)
        {
            foreach (var loc in pool.Content)
            {
                if (loc.VanillaItem != null)
                {
                    uniqueItems.Add(loc.VanillaItem);
                }
            }
        }
        uniqueItems.Add(PotsTerm);
        uniqueItems.Add("Reaper's_Sword");
        foreach (var it in uniqueItems)
        {
            var termName = it.Replace(" ", "_");
            var term = lmb.GetOrAddTerm(termName);
            lmb.AddItem(new RC.LogicItems.SingleItem(termName, new RC.TermValue(term, 1)));
        }
        lmb.GetOrAddTerm("NIGHTSTART");

        lmb.StateManager.GetOrAddBool(JeffersonStateTerm);
        lmb.StateManager.SetProperty(JeffersonStateTerm, RC.Logic.StateLogic.StateField.DefaultValuePropertyName, true);

        lmb.VariableResolver = new DDVariableResolver();

        var logicDir = IO.Path.GetDirectoryName(typeof(LogicLoader).Assembly.Location);
        var fmt = new MultilineLogicFormat();

        void LoadLogicFile(LFT type, string name)
        {
            var loc = IO.Path.Combine(logicDir, name);
            using var s = IO.File.OpenRead(loc);
            lmb.DeserializeFile(type, fmt, s);
        }

        LoadLogicFile(LFT.Transitions, "transitions.txt");
        LoadLogicFile(LFT.Locations, "locations.txt");
        LoadLogicFile(LFT.Waypoints, "waypoints.txt");

        return lmb;
    }

    public static void DefineConsolidatedItems(RC.Logic.LogicManagerBuilder lmb, CG.Dictionary<string, (int, int)> bounds)
    {
        foreach (var (item, (min, max)) in bounds)
        {
            var termName = item.Replace(" ", "_");
            var term = lmb.GetTerm(termName);
            for (var n = min; n <= max; n++)
            {
                lmb.AddItem(new RC.LogicItems.SingleItem($"{termName}_x{n}", new RC.TermValue(term, n)));
            }
        }
    }
}
