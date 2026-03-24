using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class CloseTicketEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public CloseTicketEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var result = packet.ReadInt(); // 1 = useless, 2 = abusive, 3 = resolved
        packet.ReadInt(); //junk
        var ticketId = packet.ReadInt();
        return _moderationTicketService.Close(session, result, ticketId);
    }
}
