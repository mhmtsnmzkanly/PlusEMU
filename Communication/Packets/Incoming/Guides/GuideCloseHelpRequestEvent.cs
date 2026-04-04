using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideCloseHelpRequestEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideCloseHelpRequestEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.CloseRequest(session);
}
