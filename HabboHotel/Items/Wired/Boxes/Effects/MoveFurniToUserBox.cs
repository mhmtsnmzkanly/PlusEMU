using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Engine;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class MoveFurniToUserBox : IWiredItem, IWiredCycle, IWiredEmptyExecutable
{
    private int _delay;
    private long _next;
    private bool _requested;

    public MoveFurniToUserBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        TickCount = Delay;
        _requested = false;
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
        if (Instance == null || !_requested || _next == 0)
            return false;
        if (WiredCycleScheduler.IsReady(_requested, _next))
        {
            foreach (var item in SetItems.Values.ToList())
            {
                if (item == null)
                    continue;
                if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                    continue;
                if (Instance.GetWired().OtherBoxHasItem(this, item.Id))
                {
                    SetItems.TryRemove(item.Id, out _);
                    continue;
                }
                var point = Instance.GetGameMap().GetChaseMovement(item);
                Instance.GetWired().OnUserFurniCollision(Instance, item);
                if (!WiredFloorMoveHelper.TryMoveFloorItem(Instance, item, point, out _))
                    _next = 0;
            }
            _next = 0;
            return true;
        }
        return false;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.EffectMoveFurniToNearestUser;

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
            if (selectedItem != null && !Instance.GetWired().OtherBoxHasItem(this, selectedItem.Id))
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        var delay = packet.ReadInt();
        Delay = delay;
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context)
    {
        if (SetItems.Count == 0)
            return false;
        if (WiredCycleScheduler.Schedule(ref _next, ref _requested, Delay))
        {
            TickCount = Delay;
        }
        return true;
    }
}
