using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideHandleHelpRequestEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideHandleHelpRequestEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.HandleRequest(session, packet.ReadBool());
}
