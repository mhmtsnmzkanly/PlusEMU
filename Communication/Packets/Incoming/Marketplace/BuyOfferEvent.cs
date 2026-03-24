using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class BuyOfferEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public BuyOfferEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _marketplaceService.BuyOffer(session, packet.ReadInt());
}
