using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class KickUserBox : IWiredItem, IWiredCycle, IWiredActorExecutable
{
    private const int KickDelay = 3;
    private readonly Queue<Habbo> _toKick;

    public KickUserBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        TickCount = WiredCycleScheduler.GetTickCountForDelay(KickDelay);
        _toKick = new();
        if (SetItems.Count > 0)
            SetItems.Clear();
    }

    public int TickCount { get; set; }
    public int Delay { get; set; }

    public bool OnCycle()
    {
        if (Instance == null)
            return false;
        if (_toKick.Count == 0)
        {
            TickCount = KickDelay;
            return true;
        }
        lock (_toKick)
        {
            while (_toKick.Count > 0)
            {
                var player = _toKick.Dequeue();
                if (player == null || !player.IsInRoom(Instance) || !player.TryGetClient(out var playerClient))
                    continue;
                _ = Instance.GetRoomService().KickFromRoom(playerClient);
            }
        }
        TickCount = KickDelay;
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectKickUser;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        if (SetItems.Count > 0)
            SetItems.Clear();
        _ = packet.ReadInt();
        var message = packet.ReadString();
        StringData = message;
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        var player = context.Actor;
        if (player == null)
            return false;
        if (TickCount <= 0)
            TickCount = KickDelay;
        if (!_toKick.Contains(player))
        {
            var user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
            if (user == null || !player.TryGetClient(out var playerClient))
                return false;
            if ((player.Permissions?.HasRight("mod_tool") ?? false) || Instance.OwnerId == player.Id)
            {
                playerClient.Send(new WhisperComposer(user.VirtualId, "Wired Kick Exception: Unkickable Player", 0, 0));
                return false;
            }
            _toKick.Enqueue(player);
            playerClient.Send(new WhisperComposer(user.VirtualId, StringData, 0, 0));
        }
        return true;
    }
}
