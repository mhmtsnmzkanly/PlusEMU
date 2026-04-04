using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Guides;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class OpenHelpToolEvent : IPacketEvent
{
    private readonly IGuideService _guideService;
    private readonly IModerationTicketService _moderationTicketService;

    public OpenHelpToolEvent(IGuideService guideService, IModerationTicketService moderationTicketService)
    {
        _guideService = guideService;
        _moderationTicketService = moderationTicketService;
    }

    public async Task Parse(GameClient session, IIncomingPacket packet)
    {
        session.Send(new OpenHelpToolComposer());
        await _moderationTicketService.SendOpenState(session);
        await _guideService.SendToolState(session);
    }
}
