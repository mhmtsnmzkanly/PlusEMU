using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;
using System.Collections.Concurrent;

namespace Plus.HabboHotel.Items.Wired.Boxes.Triggers;

internal class UserSaysCommandBox : IWiredItem, IWiredExecutable, IWiredChatExecutable
{
    public UserSaysCommandBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        StringData = "";
        SetItems = new();
    }

    public Room Instance { get; set; }
    public Item Item { get; set; }
    public WiredBoxType Type => WiredBoxType.TriggerUserSaysCommand;
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
        if (player == null || player.CurrentRoom == null || !player.InRoom)
            return false;
        var client = player.Client;
        if (client == null)
            return false;
        var user = player.CurrentRoom.GetRoomUserManager().GetRoomUserByHabbo(player.Username);
        if (user == null)
            return false;
        if (BoolData && Instance.OwnerId != player.Id || string.IsNullOrWhiteSpace(StringData))
            return false;
        player.WiredInteraction = true;
        var wired = Instance.GetWired();
        client.Send(new WhisperComposer(user.VirtualId, StringData, 0, 0));
        return wired.ExecuteTriggerStack(this, player);
    }
}
