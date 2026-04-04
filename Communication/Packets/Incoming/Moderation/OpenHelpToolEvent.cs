using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class OpenHelpToolEvent : IPacketEvent
{
    private readonly IGuideService _guideService;

    public OpenHelpToolEvent(IGuideService guideService) => _guideService = guideService;

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.Send(new OpenHelpToolComposer());
        await _guideService.SendToolState(session);
    }
}
