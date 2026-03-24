using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;
using Plus.Utilities;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationBanEvent : IPacketEvent
{
    private readonly IModerationActionService _moderationActionService;

    public ModerationBanEvent(IModerationActionService moderationActionService)
    {
        _moderationActionService = moderationActionService;
    }

    public Task Parse(GameClient session, IIncomingPacket packet)
    {
        var userId = packet.ReadInt();
        var message = packet.ReadString();
        var durationHours = packet.ReadInt();
        packet.ReadString(); //unk1
        packet.ReadString(); //unk2
        var ipBan = packet.ReadBool();
        var machineBan = packet.ReadBool();
        return _moderationActionService.Ban(session, userId, message, durationHours, ipBan, machineBan);
    }
}
