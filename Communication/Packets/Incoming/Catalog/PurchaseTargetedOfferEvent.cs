using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class PurchaseTargetedOfferEvent : IPacketEvent
{
    private readonly ITargetedOfferService _targetedOfferService;

    public PurchaseTargetedOfferEvent(ITargetedOfferService targetedOfferService) => _targetedOfferService = targetedOfferService;

    public Task Parse(GameClient session, IIncomingPacket packet) =>
        _targetedOfferService.Purchase(session, packet.ReadInt(), packet.ReadInt());
}
