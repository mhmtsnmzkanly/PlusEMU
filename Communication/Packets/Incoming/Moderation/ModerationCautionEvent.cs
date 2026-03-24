using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationCautionEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationCautionEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _moderationActionService.SendCaution(session, packet.ReadInt(), packet.ReadString());
}
