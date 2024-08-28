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
    public SaveData? PreparedSaveData;
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
        var save = SaveData.Current!;
        var item = save.RemoteItems[index];
        var ownerName = save.RemoteNicknames[item.PlayerId];
        var i = item.Name.LastIndexOf("_(");
        var itemName = i == -1 ? item.Name : item.Name.Substring(0, i);
        itemName = itemName.Replace("_", " ");
        return $"{ownerName}'s {itemName}";
    }

    internal static RemoteItem ItemByIndex(int index)
    {
        return SaveData.Current!.RemoteItems[index];
    }

    public void ClearPreparedData()
    {
        BaseSeed = null;
        PreparedSaveData = null;
        ItemReplacements.Clear();
    }

    public void ShowMWStatus(string s)
    {
        UE.Debug.Log(s);
    }

    public void ResendUnconfirmedItems()
    {
        if (SaveData.Current == null)
        {
            return;
        }
        foreach (var ri in SaveData.Current.RemoteItems)
        {
            if (ri.State == RemoteItemState.Collected)
            {
                MWConnection.SendItem(ri);
            }
        }
    }

    public bool ConfirmRemoteCheck(string name, int playerId)
    {
        if (SaveData.Current == null)
        {
            return false;
        }
        foreach (var ri in SaveData.Current.RemoteItems)
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
        if (SaveData.Current == null)
        {
            return;
        }
        var n = SaveData.Current.RemoteItems.Count(ri => ri.State == RemoteItemState.Collected);
        if (numConfirmedItems != n)
        {
            UE.Debug.Log($"MW: eject confirmation failed: confirmed {numConfirmedItems} but had {n} awaiting confirmation");
            return;
        }
        foreach (var ri in SaveData.Current.RemoteItems)
        {
            if (ri.State == RemoteItemState.Collected)
            {
                ri.State = RemoteItemState.Confirmed;
            }
        }
        UE.Debug.Log($"MW: ejected");
    }

    public int MWPlayerId() => SaveData.Current == null ? -1 : SaveData.Current.PlayerId;

    public void DisconnectMW()
    {
        BaseSeed = null;
        PreparedSaveData = null;
        ItemReplacements.Clear();
        ShowMWStatus("");
    }

    public void ReconnectMW()
    {
        var mw = SaveData.Current;
        if (mw != null)
        {
            MWConnection.Terminate();
            MWConnection.Join(mw.ServerAddr, mw.PlayerId, mw.RandoId, mw.RemoteNicknames[mw.PlayerId]);
        }
    }

    public void EjectMW()
    {
        if (SaveData.Current == null)
        {
            return;
        }
        var othersItems = SaveData.Current.RemoteItems.Where(ri => ri.State == RemoteItemState.Uncollected).ToList();
        MWConnection.SendManyItems(othersItems);
    }

    public bool ReceiveRemoteItem(string itemName, string from)
    {
        itemName = itemName.Replace("_", " ");
        if (!IC.Predefined.TryGetItem(itemName, out var it))
        {
            return false;
        }
        var displayName = it.DisplayName;
        var icon = it.Icon;
        // Since this item isn't at a predefined location and has instead appeared
        // out of thin air, IC does not add it to the tracker log for us.
        IC.SaveData.Open().AddToTrackerLog(new()
        {
            LocationIsVirtual = true,
            LocationName = from,
            ItemName = itemName,
            ItemDisplayName = displayName,
            ItemIcon = icon,
            GameTime = GameTimeTracker.instance.GetTime()
        });
        IC.CornerPopup.Show(IC.ItemIcons.Get(icon), $"{displayName}\nfrom {from}");
        it.Trigger();
        return true;
    }
}
