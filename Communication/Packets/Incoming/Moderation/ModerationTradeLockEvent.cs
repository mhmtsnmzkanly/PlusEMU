using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationTradeLockEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationTradeLockEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        var message = packet.ReadString();
        var durationMinutes = packet.ReadInt();
        packet.ReadString(); //unk1
        packet.ReadString(); //unk2
        return _moderationActionService.TradeLock(session, userId, message, durationMinutes);
    }
}
