using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationMsgEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationMsgEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _moderationActionService.SendMessage(session, packet.ReadInt(), packet.ReadString());
}
