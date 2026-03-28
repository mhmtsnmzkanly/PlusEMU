using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Items.Wired;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class ToggleFurniBox : IWiredItem, IWiredCycle
{
    private int _delay;

    private long _next;
    private bool _requested;

    public ToggleFurniBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public int TickCount { get; set; }

    public int Delay
    {
        get => _delay;
        set
        {
            _delay = value;
            TickCount = WiredCycleScheduler.GetTickCountForDelay(value);
        }
    }

    public bool OnCycle()
    {
        if (SetItems.Count == 0 || !_requested)
            return false;
        if (WiredCycleScheduler.IsReady(_requested, _next))
        {
            foreach (var item in SetItems.Values.ToList())
            {
                if (item == null)
                    continue;
                if (!Instance.GetRoomItemHandler().GetFloor.Contains(item))
                {
                    SetItems.TryRemove(item.Id, out _);
                    continue;
                }
                item.Interactor.OnWiredTrigger(item);
            }
            WiredCycleScheduler.Reset(ref _next, ref _requested);
            TickCount = Delay;
        }
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.EffectToggleFurniState;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        SetItems.Clear();
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadString();
        var furniCount = packet.ReadInt();
        for (var i = 0; i < furniCount; i++)
        {
            var selectedItem = Instance.GetRoomItemHandler().GetItem(packet.ReadUInt());
            if (selectedItem != null)
                SetItems.TryAdd(selectedItem.Id, selectedItem);
        }
        var delay = packet.ReadInt();
        Delay = delay;
    }

    public bool Execute(params object[] @params)
    {
        if (WiredCycleScheduler.Schedule(ref _next, ref _requested, Delay))
            TickCount = Delay;
        return true;
    }
}
