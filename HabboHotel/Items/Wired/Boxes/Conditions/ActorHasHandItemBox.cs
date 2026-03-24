using System.Collections.Concurrent;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Conditions;

internal class ActorHasHandItemBox : IWiredItem
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
    public string StringData { get; set; }
    public bool BoolData { get; set; }
    public string ItemsData { get; set; }

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var unknown2 = packet.ReadInt();
        StringData = unknown2.ToString();
    }

    public bool Execute(params object[] @params)
    {
        if (@params.Length == 0 || Instance == null || string.IsNullOrEmpty(StringData))
            return false;
        var player = (Habbo)@params[0];
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
