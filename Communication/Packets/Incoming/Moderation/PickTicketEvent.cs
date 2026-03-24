using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class PickTicketEvent : IPacketEvent
{
    private readonly IModerationTicketService _moderationTicketService;

    public PickTicketEvent(IModerationTicketService moderationTicketService)
    {
        _moderationTicketService = moderationTicketService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        packet.ReadInt(); //Junk
        return _moderationTicketService.Pick(session, packet.ReadInt());
    }
}
