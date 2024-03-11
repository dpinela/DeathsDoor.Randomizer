// Targeting an older framework requires us to provide our own version of this.
namespace System.Diagnostics.CodeAnalysis;

internal class MaybeNullWhenAttribute : System.Attribute
{
    public bool ReturnValue { get; }

    public MaybeNullWhenAttribute(bool val)
    {
        ReturnValue = val;
    }
}