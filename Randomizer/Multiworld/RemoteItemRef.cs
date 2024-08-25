using IC = DDoor.ItemChanger;

namespace DDoor.Randomizer.Multiworld;

internal record class RemoteItemRef(int Index) : IC.Item
{
    public string DisplayName => MainThread.ItemNameByIndex(Index);

    public string Icon => "Arrow"; // TODO: hahahahahahahahaha

    public void Trigger()
    {
        // TODO: later
    }
}