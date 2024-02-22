namespace DDoor.Randomizer;

using IO = System.IO;
using Text = System.Text;
using Collections = System.Collections.Generic;
using RC = RandomizerCore;

internal class MultilineLogicFormat : RC.Logic.ILogicFormat
{
    public Collections.IEnumerable<(string, RC.Logic.TermType)> LoadTerms(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }

    public Collections.IEnumerable<RC.Logic.RawWaypointDef> LoadWaypoints(IO.Stream s)
    {
        foreach (var (name, logic) in LoadGenericFile(s))
        {
            yield return new(name, logic, true);
        }
    }

    public Collections.IEnumerable<RC.Logic.RawLogicDef> LoadTransitions(IO.Stream s) =>
        LoadGenericLogic(s);
    
    public Collections.Dictionary<string, string> LoadMacros(IO.Stream s)
    {
        var dict = new Collections.Dictionary<string, string>();
        foreach (var (name, logic) in LoadGenericFile(s))
        {
            dict[name] = logic;
        }
        return dict;
    }

    public Collections.IEnumerable<RC.LogicItems.ILogicItemTemplate> LoadItems(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }
    
    public Collections.IEnumerable<RC.Logic.RawLogicDef> LoadLocations(IO.Stream s) =>
        LoadGenericLogic(s);

    public Collections.IEnumerable<RC.Logic.RawLogicDef> LoadLogicEdits(IO.Stream s) =>
        LoadGenericLogic(s);

    public Collections.Dictionary<string, string> LoadMacroEdits(IO.Stream s) => LoadMacros(s);

    public Collections.IEnumerable<RC.Logic.RawSubstDef> LoadLogicSubstitutions(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }

    public Collections.IEnumerable<RC.LogicItems.ILogicItemTemplate> LoadItemTemplates(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }

    public Collections.IEnumerable<RC.StringItems.StringItemTemplate> LoadItemStrings(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }

    public RC.Logic.StateLogic.RawStateData LoadStateData(IO.Stream s)
    {
        throw new System.InvalidOperationException("MultilineLogicFormat doesn't support this kind of file");
    }

    private Collections.IEnumerable<RC.Logic.RawLogicDef> LoadGenericLogic(IO.Stream s)
    {
        foreach (var (name, logic) in LoadGenericFile(s))
        {
            yield return new(name, logic);
        }
    }

    private Collections.IEnumerable<(string, string)> LoadGenericFile(IO.Stream s)
    {
        using var r = new IO.StreamReader(s);
        string? header = null;
        var contentBuilder = new Text.StringBuilder();
        while (true)
        {
            var line = r.ReadLine();
            if (line == null)
            {
                break;
            }
            var commentMarker = line.IndexOf("//");
            if (commentMarker != -1)
            {
                line = line.Substring(0, commentMarker);
            }
            var content = line.Trim();
            if (content.EndsWith(":"))
            {
                if (header != null)
                {
                    yield return new(header, contentBuilder.ToString());
                }
                header = content.Substring(0, content.Length - 1).Trim();
                contentBuilder.Clear();
            }
            else
            {
                if (contentBuilder.Length > 0)
                {
                    contentBuilder.Append('\n');
                }
                contentBuilder.Append(content);
            }
        }
        if (header != null)
        {
            yield return (header, contentBuilder.ToString());
        }
    }
}