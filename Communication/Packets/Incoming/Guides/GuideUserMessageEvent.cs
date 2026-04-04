using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideUserMessageEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideUserMessageEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.SendSessionMessage(session, packet.ReadString());
}
