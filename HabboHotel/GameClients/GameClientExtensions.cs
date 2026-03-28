using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Communication.Packets.Outgoing.Rooms.Chat;

namespace Plus.HabboHotel.GameClients;

public static class GameClientExtensions
{
    public static void SendWhisper(this GameClient client, string message, int colour = 0)
    {
        var habbo = client.GetHabboOrNull();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return;

        var user = room.GetRoomUserManager().GetRoomUserByHabbo(habbo.Username);
        if (user == null)
            return;

        client.Send(new WhisperComposer(user.VirtualId, message, 0, colour == 0 ? user.LastBubble : colour));
    }

    public static void SendNotification(this GameClient client, string message) => client.Send(new BroadcastMessageAlertComposer(message));
}
