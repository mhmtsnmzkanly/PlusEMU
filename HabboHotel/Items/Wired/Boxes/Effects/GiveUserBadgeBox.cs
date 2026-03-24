using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class GiveUserBadgeBox : IWiredItem
{
    public GiveUserBadgeBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }

    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.EffectGiveUserBadge;

    public ConcurrentDictionary<uint, Item> SetItems { get; set; }

    public string StringData { get; set; } = string.Empty;

    public bool BoolData { get; set; }

    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var badge = packet.ReadString();
        StringData = badge;
    }

    public bool Execute(params object[] @params)
    {
        if (@params == null || @params.Length == 0)
            return false;
        var owner = PlusEnvironment.GetHabboById(Item.UserId);
        if (owner == null || !owner.Permissions.HasRight("room_item_wired_rewards"))
            return false;
        var player = (Habbo)@params[0];
        var playerClient = player?.Client;
        var currentRoom = player?.CurrentRoom;
        if (player == null || playerClient == null || currentRoom == null)
            return false;
        var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        if (string.IsNullOrEmpty(StringData))
            return false;
        if (player.Inventory.Badges.HasBadge(StringData))
            playerClient.Send(new WhisperComposer(user.VirtualId, "Oops, it appears you have already recieved this badge!", 0, user.LastBubble));
        else
        {
            //player.Inventory.Badges.GiveBadge(StringData, true, player.GetClient());
            // TODO @80O: Inject BadgeManager
            playerClient.SendNotification("You have recieved a badge!");
        }
        return true;
    }
}
