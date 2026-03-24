using System.Data;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorRoomChatlogEvent : IPacketEvent
{
    private readonly IModerationQueryService _moderationQueryService;

    public GetModeratorRoomChatlogEvent(IModerationQueryService moderationQueryService)
    {
        _moderationQueryService = moderationQueryService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        packet.ReadInt();
        return _moderationQueryService.GetRoomChatlog(session, packet.ReadUInt());
    }
}
