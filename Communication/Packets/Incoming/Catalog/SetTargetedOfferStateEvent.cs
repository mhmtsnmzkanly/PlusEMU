using Plus.HabboHotel.Catalog;
using Plus.HabboHotel.GameClients;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class SetTargetedOfferStateEvent : IPacketEvent
{
    private readonly ITargetedOfferService _targetedOfferService;

    public SetTargetedOfferStateEvent(ITargetedOfferService targetedOfferService) => _targetedOfferService = targetedOfferService;

    public Task Parse(GameClient session, IIncomingPacket packet) =>
        _targetedOfferService.SetState(session, packet.ReadInt(), packet.ReadInt());
}
