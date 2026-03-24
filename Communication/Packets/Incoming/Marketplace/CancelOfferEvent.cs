using Plus.Communication.Packets.Outgoing.Marketplace;
using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class CancelOfferEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public CancelOfferEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _marketplaceService.CancelOffer(session, packet.ReadUInt());
}
