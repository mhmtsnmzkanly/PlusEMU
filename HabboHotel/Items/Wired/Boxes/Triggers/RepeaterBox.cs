using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class RepeaterBox : IWiredItem, IWiredCycle, IWiredExecutable, IWiredEmptyExecutable
{
    private int _delay;

    public RepeaterBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public int Delay
    {
        get => _delay;
        set
        {
            _delay = value;
            TickCount = value;
        }
    }

    public int TickCount { get; set; }

    public bool OnCycle()
    {
        var wired = Instance.GetWired();
        if (!wired.ExecuteRepeaterConditions(this))
            return false;

        if (!wired.ExecuteTriggerEffectsForRoomUsers(this))
            return false;

        TickCount = Delay;
        return true;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerRepeat;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var delay = packet.ReadInt();
        Delay = delay;
        TickCount = delay;
    }

    bool IWiredEmptyExecutable.Execute(WiredEmptyExecutionContext context) => true;
}
