namespace DDoor.Randomizer.Multiworld;

internal class RemoteItem
{
    public string Name;
    public int PlayerId;
    public RemoteItemState State = RemoteItemState.Uncollected;

    public RemoteItem(string name, int pid)
    {
        Name = name;
        PlayerId = pid;
    }
}

internal enum RemoteItemState
{
    Uncollected,
    Collected,
    Confirmed
}
