using IC = DDoor.ItemChanger;
using Json = Newtonsoft.Json;

namespace DDoor.Randomizer.Multiworld;

internal record class RemoteItemRef(int Index) : IC.Item
{
    [Json.JsonIgnore]
    public string DisplayName => MainThread.ItemNameByIndex(Index);

    [Json.JsonIgnore]
    public string Icon => "MWItem"; // TODO: hahahahahahahahaha

    public void Trigger()
    {
        MWConnection.SendItem(MainThread.ItemByIndex(Index));
    }
}
