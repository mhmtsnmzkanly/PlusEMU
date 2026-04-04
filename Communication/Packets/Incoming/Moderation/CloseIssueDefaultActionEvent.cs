using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class CloseIssueDefaultActionEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public CloseIssueDefaultActionEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var result = packet.HasDataRemaining() ? packet.ReadInt() : 3;
        if (result is < 1 or > 3)
            result = 3;

        if (packet.HasDataRemaining())
            packet.ReadInt(); // optional junk / legacy action id

        if (!packet.HasDataRemaining())
            return Task.CompletedTask;

        var ticketId = packet.ReadInt();
        return _moderationTicketService.Close(session, result, ticketId);
    }
}
