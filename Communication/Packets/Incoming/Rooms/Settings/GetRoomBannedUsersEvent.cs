using Plus.Communication.Packets.Outgoing.Rooms.Settings;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Settings;

internal class GetRoomBannedUsersEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        var instance = habbo.CurrentRoom;
        if (instance == null || !instance.CheckRights(session, true))
            return Task.CompletedTask;
        if (instance.GetBans().BannedUsers().Count > 0)
            session.Send(new GetRoomBannedUsersComposer(instance));
        return Task.CompletedTask;
    }
}
