#nullable disable

using System;
using Net = System.Net.Sockets;
using Threading = System.Threading;
using Timers = System.Timers;
using Collections = System.Collections.Generic;
using static System.Linq.Enumerable;
using SyncCollections = System.Collections.Concurrent;
using MWLib = MultiWorldLib;
using MWMsg = MultiWorldLib.Messaging;
using MWMsgDef = MultiWorldLib.Messaging.Definitions.Messages;
using UE = UnityEngine;
using RC = RandomizerCore;

namespace DDoor.Randomizer.Multiworld;

internal class MWConnection : IDisposable
{
    private string _serverAddr;
    private Action _onConnectAction;
    private Net.TcpClient _client;
    private Net.NetworkStream _conn;
    private Timers.Timer _pingTimer;
    private int _unansweredPings;
    private MWMsg.MWMessagePacker _packer;
    private Threading.Thread _writeThread, _readThread;
    private SyncCollections.BlockingCollection<Action> _commandQueue;
    private ulong _uid;
    private bool _joinConfirmed;
    private Collections.List<MWMsg.MWMessage> _messagesHeldUntilJoin = new();

    public static MWConnection Current { get; private set; }

    public static void Start()
    {
        Terminate();
        Current = new();
    }

    public static void Join(string serverAddr, int playerId, int randoId, string nickname)
    {
        if (Current == null)
        {
            Current = new();
            Current.Connect(serverAddr, () => Current.Join(playerId, randoId, nickname));
        }
        else if (Current._serverAddr != serverAddr)
        {
            Current.Dispose();
            Current = new();
            Current.Connect(serverAddr, () => Current.Join(playerId, randoId, nickname));
        }
        else
        {
            Current.Join(playerId, randoId, nickname);
        }
    }

    public static void Terminate()
    {
        if (Current != null)
        {
            Current.Disconnect();
            Current.Dispose();
            Current = null;
        }
    }

    internal static void SendItem(RemoteItem item)
    {
        if (item.State != RemoteItemState.Uncollected)
        {
            UE.Debug.Log($"item {item.Name} for player {item.PlayerId} already collected");
            return;
        }
        item.State = RemoteItemState.Collected;
        if (Current == null)
        {
            UE.Debug.Log($"cannot send item {item.Name} to player {item.PlayerId} without connection");
            return;
        }
        Current.SendRemoteItem(item);
    }

    internal static void SendManyItems(Collections.List<RemoteItem> items)
    {
        foreach (var it in items)
        {
            it.State = RemoteItemState.Collected;
        }
        if (Current == null)
        {
            UE.Debug.Log("cannot send bulk items without connection");
            return;
        }
        Current.SendManyRemoteItems(items);
    }

    public static void NotifySaved()
    {
        Current?.DoNotifySaved();
    }

    public MWConnection()
    {
        _commandQueue = new(new SyncCollections.ConcurrentQueue<Action>());
        _writeThread = new(WriteLoop);
        _writeThread.Start();
        _packer = new(new MWLib.Binary.BinaryMWMessageEncoder());
    }

    // The group MUST be called exactly this, or the MW server will
    // not shuffle together our items with the other players'.
    private const string SingularGroup = "Main Item Group";

    private void WriteLoop()
    {
        while (true)
        {
            try
            {
                var cmd = _commandQueue.Take();
                cmd();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception err)
            {
                Log(err.ToString());
            }
        }
    }

    private void ReadLoop()
    {
        while (true)
        {
            try
            {
                var msg = _packer.Unpack(new MWMsg.MWPackedMessage(_conn));
                switch (msg)
                {
                    case MWMsgDef.MWConnectMessage connMsg:
                        _commandQueue.Add(() =>
                        {
                            _uid = connMsg.SenderUid;
                            Log($"MW: Connected to {connMsg.ServerName} as UID {_uid}");
                            _pingTimer = new(PingInterval);
                            _pingTimer.Elapsed += (_, _) => Ping();
                            _pingTimer.AutoReset = true;
                            _pingTimer.Enabled = true;
                            _onConnectAction();
                        });
                        break;
                    case MWMsgDef.MWReadyConfirmMessage rcMsg:
                        _commandQueue.Add(() => Log($"MW: Joined the room with {rcMsg.Ready} players: {string.Join(", ", rcMsg.Names)}"));
                        MainThread.Invoke(mt =>
                        {
                            mt.ShowMWStatus($"Joined to room with {string.Join(", ", rcMsg.Names)}");
                        });
                        break;
                    case MWMsgDef.MWPingMessage:
                        _commandQueue.Add(() =>
                        {
                            _unansweredPings = 0;
                        });
                        break;
                    case MWMsgDef.MWRequestRandoMessage:
                        Log("MW: Received request to generate our rando");
                        MainThread.Invoke(mt =>
                        {
                            var s = RandomizerPlugin.Instance.GenerateRando();
                            mt.BaseSeed = s;
                            // The first group in the one stage corresponds to all randomized
                            // pools; the others, if they exist, are just for non-randomized
                            // pools that have had duplicate items added to them.
                            SendCheckMapping(s.Placements.SelectMany(x => x[0]).ToList());
                        });
                        break;
                    case MWMsgDef.MWResultMessage resultMsg:
                        MainThread.Invoke(mt =>
                        {
                            mt.SaveData = new(
                                _serverAddr,
                                resultMsg.PlayerId,
                                resultMsg.RandoId,
                                resultMsg.Nicknames,
                                new()
                            );

                            if (!resultMsg.Placements.TryGetValue(SingularGroup, out var pairs))
                            {
                                UE.Debug.Log("MW: no placements for group {SingularGroup}");
                                return;
                            }

                            foreach (var (itemName, locName) in pairs)
                            {
                                var (pid, name) = ParseMWItemName(itemName);
                                if (pid == -1 || pid == resultMsg.PlayerId)
                                {
                                    mt.ItemReplacements[locName] = name;
                                }
                                else
                                {
                                    UE.Debug.Log($"MW: Multiworld[{locName}] = {name}");
                                    var item = new RemoteItem(name, pid);
                                    mt.ItemReplacements[locName] = new RemoteItemRef(mt.SaveData.RemoteItems.Count);
                                    mt.SaveData.RemoteItems.Add(item);
                                }
                            }
                            mt.ShowMWStatus($"Ready to join - hash: {resultMsg.GeneratedHash}");
                        });
                        break;
                    case MWMsgDef.MWJoinConfirmMessage:
                        _commandQueue.Add(() =>
                        {
                            _joinConfirmed = true;
                            foreach (var msg in _messagesHeldUntilJoin)
                            {
                                SendPacked(msg);
                            }
                            _messagesHeldUntilJoin = null;
                        });
                        MainThread.Invoke(mt =>
                        {
                            mt.ResendUnconfirmedItems();
                        });
                        Log("MW: joined");
                        break;
                    case MWMsgDef.MWDataSendConfirmMessage dsConfirmMsg:
                        if (dsConfirmMsg.Label != MWLib.Consts.MULTIWORLD_ITEM_MESSAGE_LABEL)
                        {
                            Log($"MW: data send confirmation has invalid label {dsConfirmMsg.Label}; content={dsConfirmMsg.Content} to={dsConfirmMsg.To}");
                            break;
                        }
                        MainThread.Invoke(mt =>
                        {
                            if (mt.ConfirmRemoteCheck(dsConfirmMsg.Content, dsConfirmMsg.To))
                            {
                                UE.Debug.Log($"MW: received confirmation of sent item {dsConfirmMsg.Content} for player {dsConfirmMsg.To}");
                            }
                            else
                            {
                                UE.Debug.Log($"MW: received confirmation for unknown item {dsConfirmMsg.Content} for player {dsConfirmMsg.To}");
                            }
                        });
                        break;
                    case MWMsgDef.MWDatasSendConfirmMessage forfeitConfirmMsg:
                        MainThread.Invoke(mt =>
                        {
                            mt.ConfirmEjectMW(forfeitConfirmMsg.DatasCount);
                        });
                        break;
                    case MWMsgDef.MWRequestCharmNotchCostsMessage:
                        MainThread.Invoke(mt =>
                        {
                            var pid = mt.MWPlayerId();
                            _commandQueue.Add(() =>
                            {
                                SendPacked(new MWMsgDef.MWAnnounceCharmNotchCostsMessage()
                                {
                                    SenderUid = _uid,
                                    PlayerID = pid,
                                    Costs = new()
                                });
                            });
                        });
                        break;
                    case MWMsgDef.MWAnnounceCharmNotchCostsMessage notchCostsMsg:
                        Log($"got notch costs for player {notchCostsMsg.PlayerID} but ignoring for now");
                        _commandQueue.Add(() =>
                        {
                            SendPacked(new MWMsgDef.MWConfirmCharmNotchCostsReceivedMessage()
                            {
                                SenderUid = _uid,
                                PlayerID = notchCostsMsg.PlayerID
                            });
                        });
                        break;
                    case MWMsgDef.MWDataReceiveMessage recvMsg:
                        if (recvMsg.Label != MWLib.Consts.MULTIWORLD_ITEM_MESSAGE_LABEL)
                        {
                            Log($"MW: received data with unknown label {recvMsg.Label}");
                            break;
                        }
                        Log($"MW: got {recvMsg.Content} from {recvMsg.From}");
                        MainThread.Invoke(mt =>
                        {
                            var ok = mt.ReceiveRemoteItem(recvMsg.Content, recvMsg.From);
                            if (!ok)
                            {
                                Log($"MW: received unknown item {recvMsg.Content}");
                            }
                        });
                        _commandQueue.Add(() =>
                        {
                            SendPacked(new MWMsgDef.MWDataReceiveConfirmMessage()
                            {
                                SenderUid = _uid,
                                Label = recvMsg.Label,
                                Data = recvMsg.Content,
                                From = recvMsg.From
                            });
                        });
                        break;
                    case MWMsgDef.MWDatasReceiveMessage forfeitMsg:
                        MainThread.Invoke(mt =>
                        {
                            UE.Debug.Log($"MW: receiving {forfeitMsg.Datas.Count} forfeited items");
                            foreach (var (label, content) in forfeitMsg.Datas)
                            {
                                if (label != MWLib.Consts.MULTIWORLD_ITEM_MESSAGE_LABEL)
                                {
                                    UE.Debug.Log($"MW: received data with unknown label {label}");
                                    continue;
                                }
                                var ok = mt.ReceiveRemoteItem(content, forfeitMsg.From);
                                if (!ok)
                                {
                                    Log($"MW: received unknown item {content}");
                                }
                            }
                        });
                        _commandQueue.Add(() =>
                        {
                            SendPacked(new MWMsgDef.MWDatasReceiveConfirmMessage()
                            {
                                SenderUid = _uid,
                                Count = forfeitMsg.Datas.Count,
                                From = forfeitMsg.From
                            });
                        });
                        break;
                    default:
                        Log($"MW: got a {msg.GetType().Name}");
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception err)
            {
                Log(err.ToString());
            }
        }
    }

    private void ReadMessages(Action<MWMsg.MWMessage> handler)
    {
        while (true)
        {
            try
            {
                var msg = _packer.Unpack(new MWMsg.MWPackedMessage(_conn));
                _commandQueue.Add(() => handler(msg));
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception err)
            {
                Log(err.ToString());
            }
        }
    }

    private static (int, string) ParseMWItemName(string name)
    {
        if (!name.StartsWith("MW(")) return (-1, name);
        var i = name.IndexOf(")_");
        if (i == -1) return (-1, name);
        if (int.TryParse(name.Substring(3, i - 3), out var n)) return (n, name.Substring(i + 2));
        return (-1, name);
    }

    private static int ParseLocalCheckName(string name)
    {
        if (!name.EndsWith(")")) return -1;
        var i = name.LastIndexOf('(');
        if (i == -1) return -1;
        if (int.TryParse(name.Substring(i + 1, name.Length - i - 2), out var n)) return n;
        return -1;
    }

    public void Connect(string serverAddr, string nickname, string roomName)
    {
        Connect(serverAddr, () => Ready(nickname, roomName));
    }

    public void Connect(string serverAddr, Action onConnect)
    {
        _commandQueue.Add(() =>
        {
            _serverAddr = serverAddr;
            _onConnectAction = onConnect;
            var i = _serverAddr.IndexOf(':');
            if (i != -1 && int.TryParse(_serverAddr.Substring(i + 1), out var port))
            {
                _client = new(_serverAddr.Substring(0, i), port);
            }
            else
            {
                _client = new(_serverAddr, MWLib.Consts.DEFAULT_PORT);
            }
            _conn = _client.GetStream();
            SendPacked(new MWMsgDef.MWConnectMessage());
            _readThread = new(ReadLoop);
            _readThread.Start();
        });
    }

    private void Ping()
    {
        _commandQueue.Add(() =>
        {
            _unansweredPings++;
            if (_unansweredPings == ReconnectThreshold)
            {
                MainThread.Invoke(mt => mt.ReconnectMW());
            }
            else
            {
                SendPacked(new MWMsgDef.MWPingMessage() { SenderUid = _uid });
            }
        });
    }

    private void Disconnect()
    {
        _commandQueue.Add(() =>
        {
            SendPacked(new MWDisconnectMessage() { SenderUid = _uid });
        });
    }

    private void DoNotifySaved()
    {
        _commandQueue.Add(() => SendPacked(new MWMsgDef.MWSaveMessage() { SenderUid = _uid }));
    }

    private void Ready(string nickname, string roomName)
    {
        SendPacked(new MWMsgDef.MWReadyMessage()
        {
            SenderUid = _uid,
            Room = roomName,
            Nickname = nickname,
            ReadyMode = MWMsgDef.Mode.MultiWorld,
            ReadyMetadata = new (string, string)[0]
        });
    }

    private void Join(int playerId, int randoId, string nickname)
    {
        _commandQueue.Add(() =>
        {
            if (playerId == -1)
            {
                Log("MW: trying to join too early");
                return;
            }
            _joinConfirmed = false;
            SendPacked(new MWMsgDef.MWJoinMessage()
            {
                SenderUid = _uid,
                DisplayName = nickname,
                PlayerId = playerId,
                RandoId = randoId,
                Mode = MWMsgDef.Mode.MultiWorld
            });
        });
    }

    internal void StartRandomization()
    {
        _commandQueue.Add(() =>
        {
            SendPacked(new MWMsgDef.MWInitiateGameMessage()
            {
                SenderUid = _uid,
                Settings = "{\"RandomizationAlgorithm\": \"Default\"}"
            });
        });
    }

    internal void SendRemoteItem(RemoteItem item)
    {
        var pid = item.PlayerId;
        var name = item.Name;

        _commandQueue.Add(() =>
        {
            var msg = new MWMsgDef.MWDataSendMessage()
            {
                SenderUid = _uid,
                Label = MWLib.Consts.MULTIWORLD_ITEM_MESSAGE_LABEL,
                Content = name,
                To = pid
            };
            if (_joinConfirmed)
            {
                SendPacked(msg);
            }
            else
            {
                _messagesHeldUntilJoin.Add(msg);
            }
        });
    }

    internal void SendManyRemoteItems(Collections.List<RemoteItem> items)
    {
        _commandQueue.Add(() =>
        {
            var datas = items.Select(it => (MWLib.Consts.MULTIWORLD_ITEM_MESSAGE_LABEL, it.Name, it.PlayerId)).ToList();

            var msg = new MWMsgDef.MWDatasSendMessage()
            {
                SenderUid = _uid,
                Datas = datas
            };
            if (_joinConfirmed)
            {
                SendPacked(msg);
            }
            else
            {
                _messagesHeldUntilJoin.Add(msg);
            }
        });
    }

    private void SendCheckMapping(Collections.List<RC.RandoPlacement> placements)
    {
        var slog = RandomizerPlugin.GenerateSpoilerLog(placements);
        var items = new Collections.List<(string, string)>();
        foreach (var p in placements)
        {
            items.Add((p.Item.Name, p.Location.Name));
        }
        var itemArr = items.ToArray();
        var hash = PlacementsHash.UintHash(placements);
        _commandQueue.Add(() =>
        {
            SendPacked(new MWMsgDef.MWRandoGeneratedMessage()
            {
                SenderUid = _uid,
                Items = new Collections.Dictionary<string, (string, string)[]>()
                {
                    {SingularGroup, itemArr}
                },
                Seed = (int)hash
            });
            Log($"MW: sent {items.Count} item placements to server");
        });
    }

    private const double PingInterval = 5000;
    private const int ReconnectThreshold = 5;

    private void SendPacked(MWMsg.MWMessage msg)
    {
        var packed = _packer.Pack(msg);
        _conn.Write(packed.Buffer, 0, (int)packed.Length);
    }

    public void Dispose()
    {
        _commandQueue.Dispose();
        if (_pingTimer != null)
        {
            _pingTimer.Dispose();
        }
        if (_conn != null)
        {
            _conn.Dispose();
            _client.Dispose();
        }
    }

    private static void Log(string s)
    {
        MainThread.Invoke((_) => UE.Debug.Log(s));
    }
}