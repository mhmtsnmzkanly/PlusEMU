using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class StateChangesBox : IWiredItem, IWiredExecutable, IWiredActorItemExecutable
{
    public StateChangesBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerStateChanges;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
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
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        return ((IWiredActorItemExecutable)this).Execute((WiredActorItemExecutionContext)context);
    }

    bool IWiredActorItemExecutable.Execute(WiredActorItemExecutionContext context)
    {
        var player = context.Actor;
        var item = context.Item;
        if (player == null || item == null)
            return false;
        if (!SetItems.ContainsKey(item.Id))
            return false;
        return Instance.GetWired().ExecuteTriggerStack(this, player);
    }
}
