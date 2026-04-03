using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Items.Wired;
using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class TeleportUserBox : IWiredItem, IWiredCycle, IWiredActorExecutable
{
    private readonly Queue<Habbo> _queue;
    private int _delay;

    public TeleportUserBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        _queue = new();
        TickCount = WiredCycleScheduler.GetTickCountForDelay(Delay, extraTick: true);
    }

    public int Delay
    {
        get => _delay;
        set
        {
            _delay = value;
            TickCount = WiredCycleScheduler.GetTickCountForDelay(value, extraTick: true);
        }
    }

    public int TickCount { get; set; }

    public bool OnCycle()
    {
        if (_queue.Count == 0 || SetItems.Count == 0)
        {
            _queue.Clear();
            TickCount = Delay;
            return true;
        }
        while (_queue.Count > 0)
        {
            var player = _queue.Dequeue();
            if (player == null || !player.IsInRoom(Instance))
                continue;
            TeleportUser(player);
        }
        TickCount = Delay;
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectTeleportToFurni;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var unknown2 = packet.ReadString();
        if (SetItems.Count > 0)
            SetItems.Clear();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null)
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        Delay = packet.ReadInt();
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        var player = context.Actor;
        if (player == null)
            return false;
        player.Effects?.ApplyEffect(4);
        _queue.Enqueue(player);
        return true;
    }

    private void TeleportUser(Habbo player)
    {
        if (player == null)
            return;
        if (!player.TryGetCurrentRoom(out var room))
            return;
        if (!room.GetRoomUserManager().TryGetRoomUserByHabbo(player.Username, out var user) || user == null)
            return;
        if (player.IsTeleporting || player.IsHopping || player.TeleporterId != 0)
            return;
        if (!WiredSetItemSelector.TryGetRandomFloorItem(Instance, SetItems, out var item))
            return;
        if (room.GetGameMap() == null)
            return;
        room.GetGameMap().TeleportToItem(user, item);
        room.GetRoomUserManager().UpdateUserStatusses();
        player.Effects?.ApplyEffect(0);
    }
}
