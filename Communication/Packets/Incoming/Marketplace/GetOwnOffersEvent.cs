using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetOwnOffersEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public GetOwnOffersEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _marketplaceService.GetOwnOffers(session);
}
