using Plus.Communication.Packets.Outgoing.Rooms.Session;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Rooms.Connection;

internal class GoToFlatEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null || !habbo.InRoom)
            return Task.CompletedTask;
        if (!habbo.EnterRoom(habbo.CurrentRoom))
            session.Send(new CloseConnectionComposer());
        return Task.CompletedTask;
    }
}
