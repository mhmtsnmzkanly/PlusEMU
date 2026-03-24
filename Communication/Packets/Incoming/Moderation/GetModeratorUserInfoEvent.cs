using System.Data;
using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.Core.Language;
using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class GetModeratorUserInfoEvent : IPacketEvent
{
    private readonly IModerationQueryService _moderationQueryService;

    public GetModeratorUserInfoEvent(IModerationQueryService moderationQueryService)
    {
        _moderationQueryService = moderationQueryService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet) => _moderationQueryService.GetUserInfo(session, packet.ReadInt());
}
