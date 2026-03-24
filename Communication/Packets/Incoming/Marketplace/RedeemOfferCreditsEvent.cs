using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class RedeemOfferCreditsEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public RedeemOfferCreditsEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _marketplaceService.RedeemOfferCredits(session);
}
