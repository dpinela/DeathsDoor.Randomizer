using CA = System.Diagnostics.CodeAnalysis;
using RC = RandomizerCore;

namespace DDoor.Randomizer;

internal class DDVariableResolver : RC.Logic.VariableResolver
{
    public override bool TryMatch(RC.Logic.LogicManager lm, string term, [CA.MaybeNullWhen(false)] out RC.Logic.LogicVariable lvar)
    {
        if (base.TryMatch(lm, term, out lvar))
        {
            return true;
        }
        switch (term)
        {
            case "$BEGIN_JEFFERSON":
                lvar = new BoolSetterStateVariable(term, lm.StateManager.GetBoolStrict(LogicLoader.JeffersonStateTerm), false);
                return true;
            case "$NO_JEFFERSON":
                lvar = new BoolSetterStateVariable(term, lm.StateManager.GetBoolStrict(LogicLoader.JeffersonStateTerm), true);
                return true;
            default:
                lvar = null;
                return false;
        }
    }
}