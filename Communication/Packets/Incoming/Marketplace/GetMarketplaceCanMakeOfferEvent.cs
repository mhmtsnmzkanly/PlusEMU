using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetMarketplaceCanMakeOfferEvent : IPacketEvent
{
    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var habbo = session.GetHabbo();
        var errorCode = habbo?.TradingLockExpiry > 0 ? 6 : 1;
        session.Send(new MarketplaceCanMakeOfferResultComposer(errorCode));
        return Task.CompletedTask;
    }
}
