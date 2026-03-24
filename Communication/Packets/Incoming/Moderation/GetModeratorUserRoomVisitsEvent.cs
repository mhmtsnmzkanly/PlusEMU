using System.Data;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.HabboHotel.Rooms;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorUserRoomVisitsEvent : IPacketEvent
{
    private readonly IModerationQueryService _moderationQueryService;

    public GetModeratorUserRoomVisitsEvent(IModerationQueryService moderationQueryService)
    {
        _moderationQueryService = moderationQueryService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _moderationQueryService.GetUserRoomVisits(session, packet.ReadInt());
}
