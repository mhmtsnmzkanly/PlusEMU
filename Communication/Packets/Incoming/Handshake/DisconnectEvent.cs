using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Handshake;

internal class DisconnectEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.Disconnect("Client requested disconnect during handshake.");
        return Task.CompletedTask;
    }
}
