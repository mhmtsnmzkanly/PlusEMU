using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class MakeOfferEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public MakeOfferEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var sellingPrice = packet.ReadInt();
        packet.ReadInt(); //comission
        var itemId = packet.ReadUInt();
        return _marketplaceService.MakeOffer(session, sellingPrice, itemId);
    }
}
