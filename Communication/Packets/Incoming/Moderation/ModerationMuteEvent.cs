using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationMuteEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationMuteEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        packet.ReadString(); //message
        var durationMinutes = packet.ReadInt();
        packet.ReadString(); //unk1
        packet.ReadString(); //unk2
        return _moderationActionService.Mute(session, userId, durationMinutes);
    }
}
