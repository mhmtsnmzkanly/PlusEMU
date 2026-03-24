using Plus.Core.Language;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationKickEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationKickEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        packet.ReadString(); //message
        return _moderationActionService.Kick(session, userId);
    }
}
