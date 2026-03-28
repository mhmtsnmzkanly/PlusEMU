using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class ActorHasHandItemBox : IWiredItem, IWiredExecutable, IWiredActorExecutable
{
    public ActorHasHandItemBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
        StringData = string.Empty;
        ItemsData = string.Empty;
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.ConditionActorHasHandItemBox;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadInt();
        StringData = unknown2.ToString();
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        return ((IWiredActorExecutable)this).Execute((WiredActorExecutionContext)context);
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        if (Instance == null || string.IsNullOrEmpty(StringData))
            return false;
        var player = context.Actor;
        if (player == null)
            return false;
        var user = Instance.GetRoomUserManager().GetRoomUserByHabbo(player.Id);
        if (user == null)
            return false;
        if (user.CarryItemId != int.Parse(StringData))
            return false;
        return true;
    }
}
