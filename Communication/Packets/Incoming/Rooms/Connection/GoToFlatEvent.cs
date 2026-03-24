using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

internal class GoToFlatEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var currentRoom = habbo?.CurrentRoom;
        if (habbo == null || currentRoom == null || !habbo.InRoom)
            return Task.CompletedTask;
        if (!habbo.EnterRoom(currentRoom))
            session.Send(new CloseConnectionComposer());
        return Task.CompletedTask;
    }
}
