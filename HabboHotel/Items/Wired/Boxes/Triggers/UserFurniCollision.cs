using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class UserFurniCollision : IWiredItem, IWiredExecutable, IWiredActorItemExecutable
{
    public UserFurniCollision(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        StringData = "";
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.TriggerUserFurniCollision;

    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadString();
    }

    bool IWiredActorItemExecutable.Execute(WiredActorItemExecutionContext context)
    {
        var player = context.Actor;
        var item = context.Item;
        if (player == null || item == null)
            return false;
        var wired = Instance.GetWired();
        wired.OnEvent(Item);
        return wired.ExecuteTriggerStack(this, player);
    }
}
