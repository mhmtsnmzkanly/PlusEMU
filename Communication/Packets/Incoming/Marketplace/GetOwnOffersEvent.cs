using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetOwnOffersEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        if (habbo == null)
            return Task.CompletedTask;

        session.Send(new MarketPlaceOwnOffersComposer(habbo.Id));
        return Task.CompletedTask;
    }
}
