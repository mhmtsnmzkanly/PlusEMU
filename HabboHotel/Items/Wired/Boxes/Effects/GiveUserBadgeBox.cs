using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class GiveUserBadgeBox : IWiredItem, IWiredExecutable
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
        return ((IWiredExecutable)this).Execute(new(@params));
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        var player = context.Actor;
        var owner = Instance.GetClientManager().GetClientByUserId(Item.UserId)?.GetHabbo() ?? 
                    Instance.GetUserDataFactory().GetUserDataByIdAsync(Item.UserId).GetAwaiter().GetResult();
        var ownerPermissions = owner?.Permissions;
        if (ownerPermissions == null || !ownerPermissions.HasRight("room_item_wired_rewards"))
            return false;
        var playerClient = player?.Client;
        var currentRoom = player?.CurrentRoom;
        var playerBadges = player?.Inventory?.Badges;
        if (player == null || playerClient == null || currentRoom == null || playerBadges == null)
            return false;
        var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        if (string.IsNullOrEmpty(StringData))
            return false;
        if (playerBadges.HasBadge(StringData))
            playerClient.Send(new WhisperComposer(user.VirtualId, "Oops, it appears you have already recieved this badge!", 0, user.LastBubble));
        else
        {
            var badgeManager = Instance.GetBadgeManager();
            Task.Run(() => badgeManager.GiveBadge(player, StringData));
            playerClient.SendNotification("You have recieved a badge!");
        }
        return true;
    }
}
