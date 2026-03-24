using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class UnbanUserFromRoomEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        var instance = habbo.CurrentRoom;
        if (instance == null || !instance.CheckRights(session, true))
            return Task.CompletedTask;
        var userId = packet.ReadInt();
        var roomId = packet.ReadInt();
        if (instance.GetBans().IsBanned(userId))
        {
            instance.GetBans().Unban(userId);
            session.Send(new UnbanUserFromRoomComposer(roomId, userId));
        }
        return Task.CompletedTask;
    }
}
