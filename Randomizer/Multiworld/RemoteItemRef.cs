using IC = DDoor.ItemChanger;
using Json = Newtonsoft.Json;

namespace DDoor.Randomizer.Multiworld;

internal record class RemoteItemRef(int Index) : IC.Item
{
    [Json.JsonIgnore]
    public string DisplayName => MainThread.ItemNameByIndex(Index);

    [Json.JsonIgnore]
    public string Icon => "Arrow"; // TODO: hahahahahahahahaha

    public void Trigger()
    {
        IC.CornerPopup.Show(this);
        MWConnection.SendItem(MainThread.ItemByIndex(Index));
    }
}
