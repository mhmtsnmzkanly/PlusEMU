using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;
using Plus.HabboHotel.Rooms.Instance;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class UserSaysBox : IWiredItem, IWiredExecutable, IWiredChatExecutable
{
    public UserSaysBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        StringData = "";
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerUserSays;
    public ConcurrentDictionary<uint, Item> SetItems { get; set; }
    public string StringData { get; set; } = string.Empty;
    public bool BoolData { get; set; }
    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        var unknown = packet.ReadInt();
        var ownerOnly = packet.ReadInt();
        var message = packet.ReadString();
        BoolData = ownerOnly == 1;
        StringData = message;
    }

    bool IWiredExecutable.Execute(WiredExecutionContext context)
    {
        return ((IWiredChatExecutable)this).Execute((WiredChatExecutionContext)context);
    }

    bool IWiredChatExecutable.Execute(WiredChatExecutionContext context)
    {
        var player = context.Actor;
        var message = context.Message ?? string.Empty;
        if (player == null)
            return false;
        var playerClient = player?.Client;
        var currentRoom = player?.CurrentRoom;
        if (player == null || playerClient == null || currentRoom == null || !player.InRoom)
            return false;
        var user = currentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        if (BoolData && Instance.OwnerId != player.Id || player == null || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(StringData))
            return false;
        player.WiredInteraction = true;
        var wired = Instance.GetWired();
        playerClient.Send(new WhisperComposer(user.VirtualId, message, 0, 0));
        return wired.ExecuteTriggerStack(this, player);
    }
}
