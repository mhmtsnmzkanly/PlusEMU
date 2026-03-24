using Plus.Communication.Packets.Outgoing.Moderation;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModeratorActionEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModeratorActionEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
        => _moderationActionService.BroadcastRoomAction(session, packet.ReadInt(), packet.ReadString());
}
