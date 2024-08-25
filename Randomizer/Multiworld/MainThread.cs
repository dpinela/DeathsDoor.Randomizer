using UE = UnityEngine;
using IC = DDoor.ItemChanger;
using CC = System.Collections.Concurrent;
using CG = System.Collections.Generic;
using static System.Linq.Enumerable;

namespace DDoor.Randomizer.Multiworld;

internal class MainThread : UE.MonoBehaviour
{
    private readonly static CC.ConcurrentQueue<System.Action<MainThread>> callbacks = new();

    public Seed? BaseSeed;
    public SaveData? SaveData;
    // values are either strings or RemoteItemRefs
    public CG.Dictionary<string, object> ItemReplacements = new();

    internal static MainThread? Instance;

    public void Start()
    {
        Instance = this;
    }

    public static void Invoke(System.Action<MainThread> f)
    {
        callbacks.Enqueue(f);
    }

    public void Update()
    {
        while (callbacks.TryDequeue(out var f))
        {
            f(this);
        }
    }

    internal static string ItemNameByIndex(int index)
    {
        var save = Instance!.SaveData!;
        var item = save.RemoteItems[index];
        var ownerName = save.RemoteNicknames[item.PlayerId];
        var itemName = item.Name.Replace("_", " ");
        return $"{ownerName}'s {itemName}";
    }

    internal static RemoteItem ItemByIndex(int index)
    {
        return Instance!.SaveData!.RemoteItems[index];
    }

    public void ShowMWStatus(string s)
    {
        UE.Debug.Log(s);
    }

    public void ResendUnconfirmedItems()
    {
        if (SaveData == null)
        {
            return;
        }
        foreach (var ri in SaveData.RemoteItems)
        {
            if (ri.State == RemoteItemState.Collected)
            {
                MWConnection.SendItem(ri);
            }
        }
    }

    public bool ConfirmRemoteCheck(string name, int playerId)
    {
        if (SaveData == null)
        {
            return false;
        }
        foreach (var ri in SaveData.RemoteItems)
        {
            if (ri.PlayerId == playerId && ri.Name == name)
            {
                ri.State = RemoteItemState.Confirmed;
                return true;
            }
        }
        return false;
    }

    public void ConfirmEjectMW(int numConfirmedItems)
    {
        if (SaveData == null)
        {
            return;
        }
        var n = SaveData.RemoteItems.Count(ri => ri.State == RemoteItemState.Collected);
        if (numConfirmedItems != n)
        {
            UE.Debug.Log($"MW: eject confirmation failed: confirmed {numConfirmedItems} but had {n} awaiting confirmation");
            return;
        }
        foreach (var ri in SaveData.RemoteItems)
        {
            if (ri.State == RemoteItemState.Collected)
            {
                ri.State = RemoteItemState.Confirmed;
            }
        }
        UE.Debug.Log($"MW: ejected");
    }

    public int MWPlayerId() => SaveData == null ? -1 : SaveData.PlayerId;

    public void DisconnectMW()
    {
        BaseSeed = null;
        SaveData = null;
        ItemReplacements.Clear();
        ShowMWStatus("");
    }

    public void ReconnectMW()
    {
        if (SaveData != null)
        {
            MWConnection.Terminate();
            MWConnection.Join(SaveData.ServerAddr, SaveData.PlayerId, SaveData.RandoId, SaveData.RemoteNicknames[SaveData.PlayerId]);
        }
    }

    public void EjectMW()
    {
        if (SaveData == null)
        {
            return;
        }
        var othersItems = SaveData.RemoteItems.Where(ri => ri.State == RemoteItemState.Uncollected).ToList();
        MWConnection.SendManyItems(othersItems);
    }

    public bool ReceiveRemoteItem(string itemName, string from)
    {
        if (!IC.Predefined.TryGetItem(itemName.Replace("_", " "), out var it))
        {
            return false;
        }
        IC.CornerPopup.Show(IC.ItemIcons.Get(it.Icon), $"{it.DisplayName} from {from}");
        it.Trigger();
        return true;
    }
}
