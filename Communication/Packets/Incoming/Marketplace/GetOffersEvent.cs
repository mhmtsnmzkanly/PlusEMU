using Plus.HabboHotel.Catalog.Marketplace;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Marketplace;

internal class GetOffersEvent : IPacketEvent
{
    private readonly IMarketplaceService _marketplaceService;

    public GetOffersEvent(IMarketplaceService marketplaceService)
    {
        _marketplaceService = marketplaceService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var minCost = packet.ReadInt();
        var maxCost = packet.ReadInt();
        var searchQuery = packet.ReadString();
        var filterMode = packet.ReadInt();
        return _marketplaceService.GetOffers(session, minCost, maxCost, searchQuery, filterMode);
    }
}
