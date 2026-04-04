using Plus.Communication.Packets.Outgoing.Catalog;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Subscriptions;

namespace Plus.Communication.Packets.Incoming.Catalog;

internal class GetClubGiftInfoEvent : IPacketEvent
{
    private readonly IClubCenterService _clubCenterService;

    public GetClubGiftInfoEvent(IClubCenterService clubCenterService) => _clubCenterService = clubCenterService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _clubCenterService.SendClubGifts(session);
}
