using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorTicketChatlogsEvent : IPacketEvent
{
    private readonly IModerationQueryService _moderationQueryService;

    public GetModeratorTicketChatlogsEvent(IModerationQueryService moderationQueryService)
    {
        _moderationQueryService = moderationQueryService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _moderationQueryService.GetTicketChatlogs(session, packet.ReadInt());
}
