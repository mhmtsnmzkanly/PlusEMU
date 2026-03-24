using System.Data;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Rooms.Chat.Logs;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorUserChatlogEvent : IPacketEvent
{
    private readonly IModerationQueryService _moderationQueryService;

    public GetModeratorUserChatlogEvent(IModerationQueryService moderationQueryService)
    {
        _moderationQueryService = moderationQueryService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _moderationQueryService.GetUserChatlog(session, packet.ReadInt());
}
