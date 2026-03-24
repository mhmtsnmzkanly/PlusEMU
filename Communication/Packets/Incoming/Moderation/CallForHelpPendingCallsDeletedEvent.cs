using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class CallForHelpPendingCallsDeletedEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public CallForHelpPendingCallsDeletedEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _moderationTicketService.DeletePendingCalls(session);
}
