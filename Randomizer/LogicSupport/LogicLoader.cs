namespace DDoor.Randomizer;

using Collections = System.Collections.Generic;
using IO = System.IO;
using RC = RandomizerCore;
using LFT = RandomizerCore.Logic.LogicFileType;

internal static class LogicLoader
{
    public const string JeffersonStateTerm = "NO_JEFFERSON";
    public const string PotsTerm = "POTS";

    public static RC.Logic.LogicManager Load()
    {
        var lmb = new RC.Logic.LogicManagerBuilder();
        var uniqueItems = new Collections.HashSet<string>();
        foreach (var pool in Pool.All)
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

        return new(lmb);
    }
}
