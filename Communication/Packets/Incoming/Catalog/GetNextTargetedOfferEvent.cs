using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetNextTargetedOfferEvent : IPacketEvent
{
    private readonly ITargetedOfferService _targetedOfferService;

    public GetNextTargetedOfferEvent(ITargetedOfferService targetedOfferService) => _targetedOfferService = targetedOfferService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _targetedOfferService.SendCurrentOffer(session);
}
