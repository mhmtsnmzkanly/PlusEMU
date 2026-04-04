using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideVisitUserEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideVisitUserEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.SendRequesterRoom(session);
}
