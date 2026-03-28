using Plus.Communication.Packets.Outgoing.Handshake;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

public class InfoRetrieveEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        if (session.GetHabbo() is not { } habbo)
            return Task.CompletedTask;

        session.Send(new UserObjectComposer(habbo));
        session.Send(new UserPerksComposer());
        return Task.CompletedTask;
    }
}
