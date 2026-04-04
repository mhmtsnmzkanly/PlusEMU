using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class ShopTargetedOfferViewedEvent : IPacketEvent
{
    private readonly ITargetedOfferService _targetedOfferService;

    public ShopTargetedOfferViewedEvent(ITargetedOfferService targetedOfferService) => _targetedOfferService = targetedOfferService;

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        int? offerId = packet.HasDataRemaining() ? packet.ReadInt() : null;
        return _targetedOfferService.MarkViewed(session, offerId);
    }
}
