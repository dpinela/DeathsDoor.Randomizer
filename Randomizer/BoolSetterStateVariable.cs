using RC = RandomizerCore;
using RCSL = RandomizerCore.Logic.StateLogic;
using CG = System.Collections.Generic;
using Linq = System.Linq;

namespace DDoor.Randomizer;

internal class BoolSetterStateVariable : RCSL.StateModifier
{
    private RCSL.StateBool targetVariable;
    private bool targetValue;

    public BoolSetterStateVariable(string name, RCSL.StateBool sb, bool value)
    {
        targetVariable = sb;
        targetValue = value;
        Name = name;
    }

    public override string Name { get; }

    public override CG.IEnumerable<RC.Logic.Term> GetTerms() =>
        Linq.Enumerable.Empty<RC.Logic.Term>();
    
    public override CG.IEnumerable<RCSL.LazyStateBuilder>? ProvideState(
        object? sender, RC.Logic.ProgressionManager pm) =>
        Linq.Enumerable.Empty<RCSL.LazyStateBuilder>();
    
    public override CG.IEnumerable<RCSL.LazyStateBuilder> ModifyState(
        object? sender, RC.Logic.ProgressionManager pm, RCSL.LazyStateBuilder lsb)
    {
        lsb.SetBool(targetVariable, targetValue);
        yield return lsb;
    }
}