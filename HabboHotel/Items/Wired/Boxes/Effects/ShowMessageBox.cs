using System.Collections.Concurrent;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Instance;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items.Wired.Boxes.Effects;

internal class ShowMessageBox : IWiredItem, IWiredActorExecutable
{
    public ShowMessageBox(Room instance, Item item)
    {
        Instance = instance;
        Item = item;
        SetItems = new();
    }

    public Room Instance { get; set; }

    public Item Item { get; set; }

    public WiredBoxType Type => WiredBoxType.EffectShowMessage;

    public ConcurrentDictionary<uint, Item> SetItems { get; set; }

    public string StringData { get; set; } = string.Empty;

    public bool BoolData { get; set; }

    public string ItemsData { get; set; } = string.Empty;

    public void HandleSave(IIncomingPacket packet)
    {
        _ = packet.ReadInt();
        var message = packet.ReadString();
        StringData = message;
    }

    bool IWiredActorExecutable.Execute(WiredActorExecutionContext context)
    {
        var player = context.Actor;
        if (player == null || string.IsNullOrWhiteSpace(StringData) || !player.TryGetClient(out var playerClient) || !player.TryGetCurrentRoom(out var currentRoom))
            return false;
        if (!currentRoom.GetRoomUserManager().TryGetRoomUserByHabbo(player.Username, out var user) || user == null)
            return false;
        var message = StringData;
        if (StringData.Contains("%USERNAME%"))
            message = message.Replace("%USERNAME%", player.Username);
        if (StringData.Contains("%ROOMNAME%"))
            message = message.Replace("%ROOMNAME%", currentRoom.Name);
        if (StringData.Contains("%USERCOUNT%"))
            message = message.Replace("%USERCOUNT%", currentRoom.UserCount.ToString());
        if (StringData.Contains("%USERSONLINE%"))
            message = message.Replace("%USERSONLINE%", Instance.GetClientManager().Count.ToString());
        playerClient.Send(new WhisperComposer(user.VirtualId, message, 0, 34));
        return true;
    }
}
