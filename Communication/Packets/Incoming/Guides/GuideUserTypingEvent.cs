using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Guides;

internal sealed class GuideUserTypingEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public GuideUserTypingEvent(IGuideService guideService) => _guideService = guideService;

    public Task Parse(GameClient session, IIncomingPacket packet) => _guideService.SetTyping(session, packet.ReadBool());
}
