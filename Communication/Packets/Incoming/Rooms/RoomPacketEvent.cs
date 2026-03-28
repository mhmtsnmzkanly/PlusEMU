using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Rooms;
public abstract class RoomPacketEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.TryGetCurrentRoom(out var room))
            return Task.CompletedTask;

        return Parse(room, session, packet);
    }

    public abstract Task Parse(Room room, GameClient session, IIncomingPacket packet);
}
