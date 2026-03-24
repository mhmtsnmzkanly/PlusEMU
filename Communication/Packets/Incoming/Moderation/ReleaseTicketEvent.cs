using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ReleaseTicketEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public ReleaseTicketEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var amount = packet.ReadInt();
        var ticketIds = new List<int>(amount);
        for (var i = 0; i < amount; i++)
            ticketIds.Add(packet.ReadInt());

        return _moderationTicketService.Release(session, ticketIds);
    }
}
